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

    internal class LevelObject : IOriginator
    {
        internal class Memento
        {
            private Transform transform;

            public Memento(Transform transform)
            {
                this.transform = transform;
            }

            public Transform Transform => transform;
        }

        private Transform transform;
        private int modelID;

        public LevelObject()
        {
            this.transform = new Transform();
        }

        public Transform Transform
        {
            get
            {
                return this.transform;
            }
            set
            {
                this.transform = value;
            }
        }

        public int ModelID
        {
            get
            {
                return this.modelID;
            }
            set
            {
                this.modelID = value;
            }
        }

        public void RestoreFromMemento(object memento)
        {
            Memento value = (Memento)memento;
            this.transform = value.Transform;
        }

        public object SaveToMemento()
        {
            return new Memento(this.transform);
        }

        public void Serialize(Message stream)
        {
            stream.WriteDouble(transform.Pos[0]);
            stream.WriteDouble(transform.Pos[1]);
            stream.WriteDouble(transform.Pos[2]);
            stream.WriteDouble(Transform.Scale[0]);
            stream.WriteDouble(Transform.Scale[1]);
            stream.WriteDouble(Transform.Scale[2]);
            stream.WriteDouble(Transform.Rotation[0]);
            stream.WriteDouble(Transform.Rotation[1]);
            stream.WriteDouble(Transform.Rotation[2]);
            stream.WriteInt(ModelID);
        }
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

    internal class Level
    {
        private List<Client> connected = new List<Client>();
        private List<LevelObject> objects = new List<LevelObject>();
        private Dictionary<Type, ITransaction> transactions = new Dictionary<Type, ITransaction>();
        private Stack<TransactionContext> undo = new Stack<TransactionContext>();
        private Stack<TransactionContext> redo = new Stack<TransactionContext>();

        public Level()
        {
            transactions.Add(typeof(MoveObjectTransaction), new MoveObjectTransaction());
        }

        public void AddClient(Client client)
        {
            connected.Add(client);
        }

        public void RemoveClient(Client client)
        {
            connected.Remove(client);
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

        public void AddObject(LevelObject obj)
        {
            objects.Add(obj);

            Message msg = new Message("LEVEL_SVR", "LEVEL_STREAM_OBJECT");
            obj.Serialize(msg);

            foreach (Client client in connected)
            {
                client.Send(msg);
            }
        }
    }
}
