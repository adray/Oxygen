namespace Oxygen
{
    internal class LevelObject
    {
        private Transform transform;
        private byte[]? customData;
        private int id;
        private int version;

        public LevelObject()
        {
            this.transform = new Transform();
        }

        public int Version
        {
            get
            {
                return this.version;
            }
            set
            {
                this.version = value;
            }
        }

        public int ID
        {
            get
            {
                return this.id;
            }
            set
            {
                this.id = value;
            }
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

        public void Serialize(Message stream)
        {
            stream.WriteInt(ID);
            stream.WriteDouble(transform.Pos[0]);
            stream.WriteDouble(transform.Pos[1]);
            stream.WriteDouble(transform.Pos[2]);
            stream.WriteDouble(Transform.Scale[0]);
            stream.WriteDouble(Transform.Scale[1]);
            stream.WriteDouble(Transform.Scale[2]);
            stream.WriteDouble(Transform.Rotation[0]);
            stream.WriteDouble(Transform.Rotation[1]);
            stream.WriteDouble(Transform.Rotation[2]);

            if (customData != null)
            {
                stream.WriteInt(1);
                stream.WriteBytes(customData);
            }
            else
            {
                stream.WriteInt(0);
            }
        }

        public void Deserialize(Message stream)
        {
            double posX = stream.ReadDouble();
            double posY = stream.ReadDouble();
            double posZ = stream.ReadDouble();
            double scaleX = stream.ReadDouble();
            double scaleY = stream.ReadDouble();
            double scaleZ = stream.ReadDouble();
            double rotationX = stream.ReadDouble();
            double rotationY = stream.ReadDouble();
            double rotationZ = stream.ReadDouble();

            Transform.Pos[0] = posX;
            Transform.Pos[1] = posY;
            Transform.Pos[2] = posZ;
            Transform.Scale[0] = scaleX;
            Transform.Scale[1] = scaleY;
            Transform.Scale[2] = scaleZ;
            Transform.Rotation[0] = rotationX;
            Transform.Rotation[1] = rotationY;
            Transform.Rotation[2] = rotationZ;

            if (stream.ReadInt() == 1)
            {
                byte[] msgData = stream.GetData();
                long length = stream.Length - stream.Position;

                customData = new byte[length];
                Array.Copy(msgData, stream.Position, customData, 0, length);
            }
            else
            {
                customData = null;
            }
        }
    }
}
