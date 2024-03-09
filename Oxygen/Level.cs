using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxygen
{
    internal static class Vector3
    {
        public static void Copy(double[] source, double[] dest)
        {
            Array.Copy(source, dest, source.Length);
        }
    }

    internal interface IOriginator
    {
        object SaveToMemento();
        void RestoreFromMemento(object memento);
    }

    internal struct Transform
    {
        private double[] pos = new double[3];
        private double[] scale = new double[3];
        private double[] rotation = new double[3];

        public Transform()
        {
        }

        public void SetPos(double x, double y, double z)
        {
            pos[0] = x;
            pos[1] = y;
            pos[2] = z;
        }

        public void SetScale(double x, double y, double z)
        {
            scale[0] = x;
            scale[1] = y;
            scale[2] = z;
        }

        public void SetRotation(double x, double y, double z)
        {
            rotation[0] = x;
            rotation[1] = y;
            rotation[2] = z;
        }

        public readonly double[] Pos => pos;
        public readonly double[] Scale => scale;
        public readonly double[] Rotation => rotation;
    }

    internal interface ITransaction
    {
        void Apply(TransactionContext context);
    }

    internal class TransactionContext
    {
        public LevelObject Object { get; private set; }
        public object Data { get; private set; }
        private object? memento;

        public TransactionContext(LevelObject @object, object data)
        {
            Object = @object;
            Data = data;
        }

        public void Save()
        {
            memento = Object.SaveToMemento();
        }

        public void Restore()
        {
            if (memento != null)
            {
                Object.RestoreFromMemento(memento);
            }
        }
    }

    internal class MoveObjectTransaction : ITransaction
    {
        public void Apply(TransactionContext context)
        {
            double[]? pos = context.Data as double[];
            if (pos != null)
            {
                context.Object.Transform.SetPos(pos[0], pos[1], pos[2]);
            }
        }
    }

    /// <summary>
    /// Per client level object stream.
    /// </summary>
    internal class LevelObjectStream
    {
        private readonly Client client;
        private Queue<Message> newObjects = new Queue<Message>();
        private Queue<Message> removeObjects = new Queue<Message>();
        private Queue<Message> updates = new Queue<Message>();
        private Dictionary<int, byte[]> state = new Dictionary<int, byte[]>();

        private const int NEW_OBJECT = 0;
        private const int UPDATE_OBJECT = 1;
        private const int DELETE_OBJECT = 2;

        public LevelObjectStream(Client client)
        {
            this.client = client;
        }

        public void AddObject(LevelObject obj)
        {
            Message msg = new Message("LEVEL_SVR", "OBJECT_STREAM");
            msg.WriteInt(NEW_OBJECT);
            obj.Serialize(msg);

            newObjects.Enqueue(msg);

            byte[] bytes = msg.GetData();
            state.Add(obj.ID, bytes);
        }

        public void RemoveObject(int id)
        {
            Message msg = new Message("LEVEL_SVR", "OBJECT_STREAM");
            msg.WriteInt(DELETE_OBJECT);
            msg.WriteInt(id);
            removeObjects.Enqueue(msg);
        }

        public void UpdateObject(LevelObject obj)
        {
            //
            // Generates a delta compressed message (within another message)
            // NOTE: if multiple updates occur quickly, this will not compact the updates.
            // And will send an update for each change to the object.

            Message msg = new Message("LEVEL_SVR", "OBJECT_STREAM");
            msg.WriteInt(NEW_OBJECT);
            obj.Serialize(msg);

            byte[] bytes = state[obj.ID];
            byte[] newBytes = msg.GetData();
            byte[] compressBytes = DeltaCompress.Compress(bytes, newBytes);

            Message msg2 = new Message("LEVEL_SVR", "OBJECT_STREAM");
            msg2.WriteInt(UPDATE_OBJECT);
            msg2.WriteInt(obj.ID);
            msg2.WriteInt(compressBytes.Length);
            msg2.WriteBytes(compressBytes);

            updates.Enqueue(msg2);

            state[obj.ID] = newBytes;
        }

        public void StreamData()
        {
            while (newObjects.Count > 0)
            {
                client.Send(newObjects.Dequeue());
            }

            while (removeObjects.Count > 0)
            {
                client.Send(removeObjects.Dequeue());
            }
            
            while (updates.Count > 0)
            {
                client.Send(updates.Dequeue());
            }
        }
    }

    internal class Level
    {
        private readonly List<Client> connected = new List<Client>();
        private readonly List<LevelObject> objects = new List<LevelObject>();
        private readonly Dictionary<int, LevelObject> objectMap = new Dictionary<int, LevelObject>();
        private Dictionary<Type, ITransaction> transactions = new Dictionary<Type, ITransaction>();
        private Stack<TransactionContext> undo = new Stack<TransactionContext>();
        private Stack<TransactionContext> redo = new Stack<TransactionContext>();
        private readonly Dictionary<Client, LevelObjectStream> streams = new Dictionary<Client, LevelObjectStream>();
        private readonly object streamLock = new object();
        private readonly EventWaitHandle streamEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
        private int nextObjectID;
        private Thread streamThread;
        private bool running;

        public Level()
        {
            transactions.Add(typeof(MoveObjectTransaction), new MoveObjectTransaction());

            running = true;
            streamThread = new Thread(StreamingThread);
            streamThread.Name = "StreamingThread";
            streamThread.Start();
        }

        private void StreamingThread()
        {
            while (running)
            {
                streamEvent.WaitOne();

                lock (this.streamLock)
                {
                    foreach (var stream in streams)
                    {
                        stream.Value.StreamData();
                    }
                }
            }
        }

        private void StartStream(Client client)
        {
            LevelObjectStream stream = new LevelObjectStream(client);

            foreach (var obj in this.objects)
            {
                stream.AddObject(obj);
            }

            // We don't need to lock until we share the resource
            // by adding it to the streams.
            lock (this.streamLock)
            {
                streams.Add(client, stream);
            }

            streamEvent.Set();
        }

        public void AddClient(Client client)
        {
            connected.Add(client);
            StartStream(client);
        }

        public void RemoveClient(Client client)
        {
            connected.Remove(client);

            lock (this.streamLock)
            {
                streams.Remove(client);
            }
        }

        public void RunTransaction<T>(TransactionContext context)
        {
            context.Save();
            ITransaction transaction = transactions[typeof(T)];
            transaction.Apply(context);
        }

        public void Undo()
        {
            TransactionContext cxt = undo.Peek();
            cxt.Restore();
            redo.Push(cxt);
            undo.Pop();
        }

        public void Redo()
        {
            TransactionContext cxt = redo.Peek();
            // TODO: call RunTransaction
        }

        public void AddObject(Message msg)
        {
            LevelObject obj = new LevelObject();

            string objectType = msg.ReadString();

            double posX = msg.ReadDouble();
            double posY = msg.ReadDouble();
            double posZ = msg.ReadDouble();
            double scaleX = msg.ReadDouble();
            double scaleY = msg.ReadDouble();
            double scaleZ = msg.ReadDouble();
            double rotationX = msg.ReadDouble();
            double rotationY = msg.ReadDouble();
            double rotationZ = msg.ReadDouble();

            Transform transform = new Transform();
            transform.SetPos(posX, posY, posZ);
            transform.SetScale(scaleX, scaleY, scaleZ);
            transform.SetRotation(rotationX, rotationY, rotationZ);

            obj.Transform = transform;
            obj.ModelID = msg.ReadInt();
            obj.ID = nextObjectID++;
            objects.Add(obj);
            objectMap.Add(obj.ID, obj);

            lock (this.streamLock)
            {
                foreach (var stream in this.streams)
                {
                    stream.Value.AddObject(obj);
                }
            }

            streamEvent.Set();
        }
        
        public void UpdateObject(Message msg)
        {
            int id = msg.ReadInt();
            double posX = msg.ReadDouble();
            double posY = msg.ReadDouble();
            double posZ = msg.ReadDouble();
            double scaleX = msg.ReadDouble();
            double scaleY = msg.ReadDouble();
            double scaleZ = msg.ReadDouble();
            double rotationX = msg.ReadDouble();
            double rotationY = msg.ReadDouble();
            double rotationZ = msg.ReadDouble();

            var obj = objectMap[id];
            obj.Transform.Pos[0] = posX;
            obj.Transform.Pos[1] = posY;
            obj.Transform.Pos[2] = posZ;
            obj.Transform.Scale[0] = scaleX;
            obj.Transform.Scale[1] = scaleY;
            obj.Transform.Scale[2] = scaleZ;
            obj.Transform.Rotation[0] = rotationX;
            obj.Transform.Rotation[1] = rotationY;
            obj.Transform.Rotation[2] = rotationZ;

            lock (this.streamLock)
            {
                foreach (var stream in this.streams)
                {
                    stream.Value.UpdateObject(obj);
                }
            }

            streamEvent.Set();
        }

        public void RemoveObject(int id)
        {
            if (objectMap.TryGetValue(id, out LevelObject? obj))
            {
                objects.Remove(obj);
                objectMap.Remove(id);

                lock (this.streamLock)
                {
                    foreach (var stream in this.streams)
                    {
                        stream.Value.RemoveObject(id);
                    }
                }

                streamEvent.Set();
            }
        }
    }
}
