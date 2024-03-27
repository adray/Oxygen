using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static Oxygen.Level;

namespace Oxygen
{
    internal static class Vector3
    {
        public static void Copy(double[] source, double[] dest)
        {
            Array.Copy(source, dest, source.Length);
        }
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

    internal partial class Level
    {
        public enum Stream
        {
            Object,
            Event
        }

        private readonly string levelName;
        private readonly List<Client> connected = new List<Client>();
        private readonly List<LevelObject> objects = new List<LevelObject>();
        private readonly Dictionary<int, LevelObject> objectMap = new Dictionary<int, LevelObject>();
        private readonly ObjectStream objectStream = new ObjectStream();
        private readonly EventStream eventStream = new EventStream();
        private Dictionary<int, byte[]> state = new Dictionary<int, byte[]>();
        private readonly EventWaitHandle streamEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
        private int nextObjectID;
        private Thread streamThread;
        private bool running;
        private static string LevelPath = @"Levels\";
        private static List<string> levels = new List<string>();
        private const int END_STREAM = 255;

        private Level(string levelName)
        {
            this.levelName = levelName;

            running = true;
            streamThread = new Thread(StreamingThread);
            streamThread.Name = "StreamingThread";
            streamThread.Start();
        }

        public string Name => levelName;

        public bool Running => running;

        public static Level NewLevel(string file)
        {
            Level level = new Level(file);
            levels.Add(file);
            return level;
        }

        public static Level? LoadFromFile(string file)
        {
            if (File.Exists(LevelPath + file))
            {
                Level level = new Level(file);
                level.LoadLevel();
                return level;
            }

            return null;
        }

        public static void LoadLevels()
        {
            string[] files = Directory.GetFiles(LevelPath);
            foreach (string file in files)
            {
                levels.Add(Path.GetFileName(file));
            }
        }

        public static IList<string> Levels => levels;

        private void SaveLevel()
        {
            if (!Directory.Exists(LevelPath))
            {
                Directory.CreateDirectory(LevelPath);
            }

            Message msg = new Message("LEVEL", "1");
            foreach (LevelObject obj in objects)
            {
                obj.Serialize(msg);
            }
            byte[] levelData = msg.GetData();

            try
            {
                using (var stream = new BinaryWriter(File.OpenWrite(LevelPath + levelName)))
                {
                    stream.Write(objects.Count);
                    stream.Write(levelData.Length);
                    stream.Write(levelData);
                }
            }
            catch (IOException ex)
            {
                Logger.Instance.Log(ex.Message);
            }
        }

        public void LoadLevel()
        {
            byte[]? bytes = null;
            int count = 0;

            string file = LevelPath + levelName;
            if (File.Exists(file))
            {
                try
                {
                    using (var stream = new BinaryReader(File.OpenRead(file)))
                    {
                        count = stream.ReadInt32();
                        int numBytes = stream.ReadInt32();
                        bytes = stream.ReadBytes(numBytes);
                    }
                }
                catch (IOException ex)
                {
                    Logger.Instance.Log(ex.Message);
                }
            }

            if (bytes != null)
            {
                Message msg = new Message(bytes);
                for (int i = 0; i < count; i++)
                {
                    LevelObject obj = new LevelObject();
                    obj.ID = msg.ReadInt();
                    obj.Deserialize(msg);
                    //obj.ModelID = msg.ReadInt();
                    objects.Add(obj);
                    objectMap.Add(obj.ID, obj);

                    Message packed = new Message("LEVEL_SVR", "OBJECT_STREAM");
                    packed.WriteInt(0/*NEW_OBJECT*/);
                    obj.Serialize(packed);
                    state[obj.ID] = packed.GetData();

                    nextObjectID = Math.Max(obj.ID + 1, nextObjectID);
                }
            }
        }

        private void Shutdown()
        {
            running = false;
            streamEvent.Set(); // flush streaming thread
            SaveLevel();
        }

        private void StreamingThread()
        {
            Console.WriteLine("Streaming {0} Thread started", Name);
            while (running)
            {
                streamEvent.WaitOne();

                objectStream.StreamData();
                eventStream.StreamData();
            }
            Console.WriteLine("Streaming {0} Thread ended", Name);
        }

        private void StartObjectStream(Client client)
        {
            LevelObjectStream stream = new LevelObjectStream(client);

            foreach (var obj in this.objects)
            {
                stream.AddObject(obj);
            }

            // We don't need to lock until we share the resource
            // by adding it to the streams.
            if (objectStream.Add(client, stream))
            {
                streamEvent.Set();
            }
        }

        private void StopObjectStream(Client client)
        {
            // TODO: send close stream only if it is open
            objectStream.Remove(client);

            Message msg = new Message("LEVEL_SVR", "OBJECT_STREAM");
            msg.WriteInt(END_STREAM);
            client.Send(msg);
        }

        private void StartEventStream(Client client)
        {
            LevelEventStream stream = new LevelEventStream(client);

            foreach (var user in this.connected)
            {
                stream.UserConnected(user.ID, (string?)user.GetProperty("USER_NAME"));
            }

            if (eventStream.AddStream(client, stream))
            {
                streamEvent.Set();
            }
        }

        private void StopEventStream(Client client)
        {
            // TODO: send close stream only if it is open
            eventStream.RemoveStream(client);

            Message msg = new Message("LEVEL_SVR", "EVENT_STREAM");
            msg.WriteInt(END_STREAM);
            client.Send(msg);
        }

        public void AddClient(Client client)
        {
            if (!connected.Contains(client))
            {
                connected.Add(client);

                eventStream.UserConnected(client.ID, (string?)client.GetProperty("USER_NAME"));
                streamEvent.Set();
            }
        }

        public void RemoveClient(Client client)
        {
            eventStream.UserDisconnected(client.ID, (string?)client.GetProperty("USER_NAME"));
            streamEvent.Set();

            connected.Remove(client);

            StopEventStream(client);
            StopObjectStream(client);

            if (connected.Count == 0)
            {
                this.Shutdown();
            }
        }

        public void AddStream(Client client, Stream stream)
        {
            switch (stream)
            {
                case Stream.Object:
                    StartObjectStream(client);
                    break;
                case Stream.Event:
                    StartEventStream(client);
                    break;
            }
        }

        public void RemoveStream(Client client, Stream stream)
        {
            switch (stream)
            {
                case Stream.Object:
                    StopObjectStream(client);
                    break;
                case Stream.Event:
                    StopEventStream(client);
                    break;
            }
        }

        public void AddObject(Client client, Message msg)
        {
            LevelObject obj = new LevelObject();

            obj.Deserialize(msg);

            //obj.ModelID = msg.ReadInt();
            obj.ID = nextObjectID++;
            objects.Add(obj);
            objectMap.Add(obj.ID, obj);

            Message msg2 = new Message("LEVEL_SVR", "OBJECT_STREAM");
            msg2.WriteInt(1/*NEW_OBJECT*/);
            obj.Serialize(msg2);
            byte[] bytes = msg2.GetData();
            state.Add(obj.ID, bytes);

            objectStream.AddObject(obj);

            streamEvent.Set();

            Message response = new Message(msg.NodeName, msg.MessageName);
            response.WriteString("ACK");
            client.Send(response);
        }
        
        public void UpdateObject(Client client, Message msg)
        {
            int id = msg.ReadInt();
            int version = msg.ReadInt();

            var obj = objectMap[id];
            if (version == obj.Version)
            {
                obj.Version++;

                byte[] initialData = state[id];
                byte[] decomprssedData = DeltaCompress.Decompress(initialData, msg.ReadByteArray());
                Message msg2 = new Message(decomprssedData);
                msg2.ReadInt(); // type
                msg2.ReadInt(); // id
                obj.Deserialize(msg2);

                state[id] = decomprssedData;

                objectStream.UpdateObject(obj);

                streamEvent.Set();

                Message response = new Message(msg.NodeName, msg.MessageName);
                response.WriteString("ACK");
                client.Send(response);
            }
            else
            {
                Message response = new Message(msg.NodeName, msg.MessageName);
                response.WriteString("NACK");
                response.WriteInt(100);
                response.WriteString("Version number is out of date.");
                client.Send(response);
            }
        }

        public void RemoveObject(int id)
        {
            if (objectMap.TryGetValue(id, out LevelObject? obj))
            {
                objects.Remove(obj);
                objectMap.Remove(id);

                objectStream.RemoveObject(id);

                streamEvent.Set();
            }
        }

        public void MoveCursor(Client client, Message msg)
        {
            eventStream.MoveUserCursor(client.ID,
                msg.ReadInt(),
                msg.ReadInt());
            streamEvent.Set();
        }
    }
}
