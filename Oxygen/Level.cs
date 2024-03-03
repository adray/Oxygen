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

    internal class LevelObject
    {
        private Transform transform;

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
    }

    internal class Level
    {
        private List<Client> connected = new List<Client>();
        private List<LevelObject> objects = new List<LevelObject>();

        public void AddClient(Client client)
        {
            connected.Add(client);
        }

        public void RemoveClient(Client client)
        {
            connected.Remove(client);
        }

        public void AddObject(LevelObject obj)
        {
            objects.Add(obj);

            Message msg = new Message("LEVEL_SVR", "LEVEL_STREAM");
            msg.WriteDouble(obj.Transform.Pos[0]);
            msg.WriteDouble(obj.Transform.Pos[1]);
            msg.WriteDouble(obj.Transform.Pos[2]);
            msg.WriteDouble(obj.Transform.Scale[0]);
            msg.WriteDouble(obj.Transform.Scale[1]);
            msg.WriteDouble(obj.Transform.Scale[2]);
            msg.WriteDouble(obj.Transform.Rotation[0]);
            msg.WriteDouble(obj.Transform.Rotation[1]);
            msg.WriteDouble(obj.Transform.Rotation[2]);

            foreach (Client client in connected)
            {
                client.Send(msg);
            }
        }
    }
}
