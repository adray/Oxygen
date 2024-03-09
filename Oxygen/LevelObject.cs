namespace Oxygen
{
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
        private int id;

        public LevelObject()
        {
            this.transform = new Transform();
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
            stream.WriteInt(ModelID);
        }
    }
}
