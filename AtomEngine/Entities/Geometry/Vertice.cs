using AtomEngine.Math;

namespace AtomEngine.Geometry
{
    public struct Vertice : IVertice
    { 
        public uint Index           { get; set; }
        public Vector4D Position    { get; set; }
        public Vector3D Normal      { get; set; }
        public Color4 Color         { get; set; }
        public Vector2D TexCoord    { get; set; }

        public bool IsPosition      = false;
        public bool IsNormal        = false;
        public bool IsColor         = false;
        public bool IsTexCoord      = false;

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
            Index           = index;

            Position        = positions;
            Normal          = normal;
            Color           = color;
            TexCoord        = texCoord;

            IsPosition      = true;
            IsNormal        = true;
            IsColor         = true;
            IsTexCoord      = true;
        }
        public Vertice(Vector4D positions, Vector3D normal, Color4 color, uint index = 0)
        {
            Index           = index;

            Position        = positions;
            Normal          = normal;
            Color           = color;
            TexCoord        = default;

            IsPosition      = true;
            IsNormal        = true;
            IsColor         = true;
            IsTexCoord      = false;
        }
        public Vertice(Vector4D positions, Color4 color, Vector2D texCoord, uint index = 0)
        {
            Index           = index;

            Position        = positions;
            Color           = color;
            TexCoord        = texCoord;
            Normal          = default;

            IsPosition      = true;
            IsColor         = true;
            IsTexCoord      = true;
            IsNormal        = false;
        }
        public Vertice(Vector4D positions, Color4 color, uint index = 0)
        {
            Index           = index; 

            Position        = positions;
            TexCoord        = default;
            Normal          = default;
            Color           = color;

            IsPosition      = true;
            IsTexCoord      = false;
            IsNormal        = false;
            IsColor         = true;
        }
        public Vertice(Vector4D positions, Vector3D normal, uint index = 0)
        {
            Index           = index;

            Position        = positions;
            TexCoord        = default;
            Normal          = normal;
            Color           = default;

            IsPosition      = true;
            IsTexCoord      = false;
            IsNormal        = true;
            IsColor         = false;
        }
        public Vertice(Vector4D positions, Vector2D texCoord, uint index = 0)
        {
            Index           = index;

            Position        = positions;
            TexCoord        = texCoord;
            Normal          = default;
            Color           = default;

            IsPosition      = true;
            IsTexCoord      = true;
            IsNormal        = false;
            IsColor         = false;
        }
        public Vertice(Vector4D positions, uint index = 0)
        {
            Index           = index;

            Position        = positions;
            Normal          = default;
            Color           = default;
            TexCoord        = default;

            IsPosition      = true;
            IsNormal        = false;
            IsColor         = false;
            IsTexCoord      = false;
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
