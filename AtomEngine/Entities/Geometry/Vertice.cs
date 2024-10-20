using AtomEngine.Math;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace AtomEngine.Geometry
{
    public struct Vertice : IVertice
    { 
        public uint Index           { get; set; }
        public Vector3D Position    { get; set; }
        public Vector3D Normal      { get; set; }
        public Color4 Color         { get; set; }
        public Vector2D TexCoord    { get; set; }

        public bool IsPosition      = false;
        public bool IsNormal        = false;
        public bool IsColor         = false;
        public bool IsTexCoord      = false;

        //public IEnumerable<double> ToVerticeInfoDouble()
        //{ 
        //    List<double> result = new List<double>();
        //    if (IsPosition) result.AddRange(new double[] { Position.X, Position.Y, Position.Z});
        //    if (IsNormal)   result.AddRange(new double[] { Normal.X, Normal.Y, Normal.Z });
        //    if (IsColor)    result.AddRange(new double[] { Color.R, Color.G, Color.B, Color.A });
        //    if (IsTexCoord) result.AddRange(new double[] { TexCoord.X, TexCoord.Y });
        //    return result;
        //}
        //public IEnumerable<float> ToVerticeInfoFloat()
        //{
        //    List<float> result = new List<float>();
        //    if (IsPosition) result.AddRange(new float[] {(float)Position.X, (float)Position.Y, (float)Position.Z});
        //    if (IsNormal)   result.AddRange(new float[] {(float)Normal.X, (float)Normal.Y, (float)Normal.Z });
        //    if (IsColor)    result.AddRange(new float[] {(float)Color.R, (float)Color.G, (float)Color.B, (float)Color.A });
        //    if (IsTexCoord) result.AddRange(new float[] {(float)TexCoord.X, (float)TexCoord.Y });
        //    return result.ToArray();
        //}

        public IEnumerable<T> FromoVerticeTo<T>() where T : IConvertible 
            =>GetVertceAttributes(d => (T)Convert.ChangeType(d, typeof(T)));

        private IEnumerable<ToType> GetVertceAttributes<ToType>(Func<double, ToType> converter)
        {
            return Enumerable.Empty<ToType>()
                .Concat(IsPosition ? new[] { Position.X, Position.Y, Position.Z }.Select(converter) : Enumerable.Empty<ToType>())
                .Concat(IsNormal ? new[] { Normal.X, Normal.Y, Normal.Z }.Select(converter) : Enumerable.Empty<ToType>())
                .Concat(IsColor ? new[] { (double)Color.R, (double)Color.G, (double)Color.B, (double)Color.A }.Select(converter) : Enumerable.Empty<ToType>())
                .Concat(IsTexCoord ? new[] { TexCoord.X, TexCoord.Y }.Select(converter) : Enumerable.Empty<ToType>());
        }

        public Vertice(Vector3D positions, Vector3D normal, Color4 color, Vector2D texCoord, uint index = 0)
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
        public Vertice(Vector3D positions, Vector3D normal, Color4 color, uint index = 0)
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
        public Vertice(Vector3D positions, Color4 color, Vector2D texCoord, uint index = 0)
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
        public Vertice(Vector3D positions, Color4 color, uint index = 0)
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
        public Vertice(Vector3D positions, Vector3D normal, uint index = 0)
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
        public Vertice(Vector3D positions, Vector2D texCoord, uint index = 0)
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
        public Vertice(Vector3D positions, uint index = 0)
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

        public static Vertice operator +(Vertice a, Vertice b) => new Vertice(a.Position + b.Position);
        public static Vertice operator -(Vertice a, Vertice b) => new Vertice(a.Position - b.Position);
        public static Vertice operator *(Vertice a, double b) => new Vertice(a.Position * b);
        public static Vertice operator /(Vertice a, double b) => new Vertice(a.Position / b);
        public static implicit operator Vector3D(Vertice a) => a.Position;

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is null) return false;
            if (obj is Vertice otherVertice)
                return Index == otherVertice.Index &
                    Position == otherVertice.Position &
                    Normal == otherVertice.Normal &
                    Color == otherVertice.Color &
                    TexCoord == otherVertice.TexCoord;
            
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Index, Position, Normal, Color, TexCoord);
        }
    }

    public class VerticeEqualityComparer : IEqualityComparer<Vertice>
    {
        public bool Equals(Vertice x, Vertice y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(Vertice obj)
        {
            return HashCode.Combine(obj.Index, obj.Position, obj.Normal, obj.Color, obj.TexCoord);
        }
    }
}
