using AtomEngine.Math;

namespace AtomEngine.Geometry
{
    public struct Vertice : IVertice
    {
        private Vector4D position;
        public Vector4D Position { get => position;}

        public Vertice(Vector4D positions)
        {
            this.position = positions;
        }

        public static Vertice operator +(Vertice a, Vertice b)
        {
            return new Vertice(a.Position + b.Position);
        }

        public static Vertice operator -(Vertice a, Vertice b)
        {
            return new Vertice(a.Position - b.Position);
        }

        public static implicit operator Vector4D(Vertice a)
        {
            return a.Position;
        } 

        public static implicit operator Vector3D(Vertice a)
        {
            return new Vector3D(a.position.X, a.position.Y, a.position.Z);
        }

        public static Vertice operator *(Vertice a, double b)
        {
            return new Vertice(a.Position * b);
        }

        public static Vertice operator /(Vertice a, double b)
        {
            return new Vertice(a.Position / b);
        }
    }
}
