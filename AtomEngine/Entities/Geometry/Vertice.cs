using AtomEngine.Math;

namespace AtomEngine.Geometry
{
    public struct Vertice : IVertice
    {
        private uint index = 0;
        public uint Index { get; set; }

        public Vector4D _position;
        public Vector4D Position {get => _position; private set => _position = value; }

        public Vector3D Normal;
        public Color4 Color;
        public Vector2D TexCoord;
        
        public bool IsPosition  = false;
        public bool IsNormal    = false;
        public bool IsColor     = false;
        public bool IsTexCoord  = false;

        public double[] ToVerticeInfoDouble()
        {
            double[] result = new double[0];
            if (IsPosition) result.Concat(new double[] { Position.X, Position.Y, Position.Z, Position.W });
            if (IsNormal) result.Concat(new double[] { Normal.X, Normal.Y, Normal.Z });
            if (IsColor) result.Concat(new double[] { Color.R, Color.G, Color.B, Color.A });
            if (IsTexCoord) result.Concat(new double[] { TexCoord.X, TexCoord.Y });
            return result;
        }
        public float[] ToVerticeInfoFloat()
        {
            float[] result = new float[0];
            if (IsPosition) result.Concat(new float[] { (float)Position.X, (float)Position.Y, (float)Position.Z, (float)Position.W });
            if (IsNormal) result.Concat(new float[] { (float)Normal.X, (float)Normal.Y, (float)Normal.Z });
            if (IsColor) result.Concat(new float[] { (float)Color.R, (float)Color.G, (float)Color.B, (float)Color.A });
            if (IsTexCoord) result.Concat(new float[] { (float)TexCoord.X, (float)TexCoord.Y });
            return result;
        }

        public Vertice(Vector4D positions, Vector3D normal, Color4 color, Vector2D texCoord, uint index = 0)
        {
            this.Position   = positions;
            this.index      = index;
            this.Normal     = normal;
            this.Color      = color;
            this.TexCoord   = texCoord;

            IsPosition      = true;
            IsNormal        = true;
            IsColor         = true;
            IsTexCoord      = true;
        }
        public Vertice(Vector4D positions, Vector3D normal, Color4 color, uint index = 0)
        {
            this.Position   = positions;
            this.index      = index;
            this.Normal     = normal;
            this.Color      = color;

            IsPosition      = true;
            IsNormal        = true;
            IsColor         = true;
        }
        public Vertice(Vector4D positions, Color4 color, Vector2D texCoord, uint index = 0)
        {
            this.Position   = positions;
            this.index      = index;
            this.Color      = color;
            this.TexCoord   = texCoord;

            IsPosition      = true;
            IsColor         = true;
            IsTexCoord      = true;
        }
        public Vertice(Vector4D positions, Color4 color, uint index = 0)
        {
            this.Position   = positions;
            this.index      = index; 
            this.Color      = color;

            IsPosition      = true;
            IsColor         = true;
        }
        public Vertice(Vector4D positions, Vector3D normal, uint index = 0)
        {
            this.Position   = positions;
            this.index      = index;
            this.Normal     = normal;

            IsPosition      = true;
            IsNormal        = true;
        }
        public Vertice(Vector4D positions, Vector2D texCoord, uint index = 0)
        {
            this.Position   = positions;
            this.index      = index;
            this.TexCoord   = texCoord;

            IsPosition      = true;
            IsTexCoord      = true;
        }
        public Vertice(Vector4D positions, uint index = 0)
        {
            this.Position   = positions;
            this.index      = index;

            IsPosition      = true;
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
            return new Vector3D(a.Position.X, a.Position.Y, a.Position.Z);
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
