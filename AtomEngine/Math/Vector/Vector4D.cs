namespace AtomEngine.Math
{
    public struct Vector4D<T> where T : struct, IComparable, IConvertible, IFormattable
    {
        public T X;
        public T Y;
        public T Z;
        public T W;

        public Vector4D(Vector2D<T> source)
        {
            X = source.X;
            Y = source.Y;
            Z = default(T);
            W = default(T);
        }
        public Vector4D(Vector3D<T> source)
        {
            X = source.X;
            Y = source.Y;
            Z = source.Z;
            W = default(T);
        }
        public Vector4D(Vector4D<T> source)
        {
            X = source.X;
            Y = source.Y;
            Z = source.Z;
            W = source.W;
        }
        public Vector4D(T x, T y, T z, T w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj is Vector4D<T> vector)
            {
                return this == vector;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode();
        }

        public static Vector4D<T> operator +(Vector4D<T> a, Vector4D<T> b)
        {
            return new Vector4D<T>(GenericMath<T>.AddT(a.X, b.X), GenericMath<T>.AddT(a.Y, b.Y), GenericMath<T>.AddT(a.Z, b.Z), GenericMath<T>.AddT(a.W, b.W));
        }

        public static Vector4D<T> operator -(Vector4D<T> a, Vector4D<T> b)
        {
            return new Vector4D<T>(GenericMath<T>.SubtractT(a.X, b.X), GenericMath<T>.SubtractT(a.Y, b.Y), GenericMath<T>.SubtractT(a.Z, b.Z), GenericMath<T>.SubtractT(a.W, b.W));
        }

        public static Vector4D<T> operator *(Vector4D<T> a, T b)
        {
            return new Vector4D<T>(GenericMath<T>.MultiplyT(a.X, b), GenericMath<T>.MultiplyT(a.Y, b), GenericMath<T>.MultiplyT(a.Z, b), GenericMath<T>.MultiplyT(a.W, b));
        }

        public static Vector4D<T> operator /(Vector4D<T> a, T b)
        {
            return new Vector4D<T>(GenericMath<T>.DivideT(a.X, b), GenericMath<T>.DivideT(a.Y, b), GenericMath<T>.DivideT(a.Z, b), GenericMath<T>.DivideT(a.W, b));
        }

        public static bool operator ==(Vector4D<T> a, Vector4D<T> b)
        {
            return a.X.Equals(b.X) && a.Y.Equals(b.Y) && a.Z.Equals(b.Z) && a.W.Equals(b.Z);
        }

        public static bool operator !=(Vector4D<T> a, Vector4D<T> b)
        {
            return !(a == b);
        }

        public static Vector4D<T> Zero => new Vector4D<T>(default(T), default(T), default(T), default(T));
        public static Vector4D<T> One => new Vector4D<T>(GenericMath<T>.ConvertTo<T>(1), GenericMath<T>.ConvertTo<T>(1), GenericMath<T>.ConvertTo<T>(1), GenericMath<T>.ConvertTo<T>(1));
        public static Vector4D<T> Up => new Vector4D<T>(default(T), GenericMath<T>.ConvertTo<T>(1), default(T), default(T));
        public static Vector4D<T> Down => new Vector4D<T>(default(T), GenericMath<T>.ConvertTo<T>(-1), default(T), default(T));
        public static Vector4D<T> Left => new Vector4D<T>(GenericMath<T>.ConvertTo<T>(-1), default(T), default(T), default(T));
        public static Vector4D<T> Right => new Vector4D<T>(GenericMath<T>.ConvertTo<T>(1), default(T), default(T), default(T));
        public static Vector4D<T> Forward => new Vector4D<T>(default(T), default(T), GenericMath<T>.ConvertTo<T>(1), default(T));
        public static Vector4D<T> Back => new Vector4D<T>(default(T), default(T), GenericMath<T>.ConvertTo<T>(-1), default(T));

        public double Magnitude => MathF.Sqrt(Convert.ToDouble(GenericMath<T>.MultiplyT(X, X)) + Convert.ToDouble(GenericMath<T>.MultiplyT(Y, Y)) + Convert.ToDouble(GenericMath<T>.MultiplyT(Z, Z)) + Convert.ToDouble(GenericMath<T>.MultiplyT(W, W)));
        public double SqrAbs() => Convert.ToDouble(X) * Convert.ToDouble(X) + Convert.ToDouble(Y) * Convert.ToDouble(Y) + Convert.ToDouble(Z) * Convert.ToDouble(Z) + Convert.ToDouble(W) * Convert.ToDouble(W);
        public Vector4D<T> Normalized => this / GenericMath<T>.ConvertTo<T>(Magnitude);

        public static T Dot(Vector4D<T> a, Vector4D<T> b)
        {
            var resX = GenericMath<T>.MultiplyT(a.X, b.X);
            var resY = GenericMath<T>.MultiplyT(a.Y, b.Y);
            var resZ = GenericMath<T>.MultiplyT(a.Z, b.Z);
            var resW = GenericMath<T>.MultiplyT(a.W, b.W);
            return GenericMath<T>.AddT(GenericMath<T>.AddT(GenericMath<T>.AddT(resX, resY), resZ), resW);
        }

        public static double Distance(Vector4D<T> a, Vector4D<T> b)
        {
            return (a - b).Magnitude;
        }

        public static Vector4D<T> Lerp(Vector4D<T> a, Vector4D<T> b, T t)
        {
            return a + (b - a) * t;
        }

        public override string ToString()
        {
            return "X: " + X + " Y: " + Y + "Z: " + Z + "W: " + W;
        }
    } 
    public struct Vector4D
    {
        public double X;
        public double Y;
        public double Z;
        public double W;

        public Vector4D(Vector2D source)
        {
            X = source.X;
            Y = source.Y;
            Z = 0;
            W = 0;
        }
        public Vector4D(Vector3D source)
        {
            X = source.X;
            Y = source.Y;
            Z = source.Z;
            W = 0;
        }
        public Vector4D(Vector4D source)
        {
            X = source.X;
            Y = source.Y;
            Z = source.Z;
            W = source.W;
        }
        public Vector4D(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }
        public Vector4D(double x, double y, double z, double w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public static Vector4D operator +(Vector4D a, Vector4D b)   => new Vector4D(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W); 
        public static Vector4D operator -(Vector4D a, Vector4D b)   => new Vector4D(a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.W - b.W); 
        public static Vector4D operator *(Vector4D a, float b)      => new Vector4D(a.X * b, a.Y * b, a.Z * b, a.W * b); 
        public static Vector4D operator *(Vector4D a, double b)     => new Vector4D(a.X * (float)b, a.Y * (float)b, a.Z * (float)b, a.W * (float)b); 
        public static Vector4D operator /(Vector4D a, float b)      => new Vector4D(a.X / b, a.Y / b, a.Z / b, a.W /b); 
        public static Vector4D operator /(Vector4D a, double b)     => new Vector4D(a.X / (float)b, a.Y / (float)b, a.Z / (float)b, a.W / (float)b); 
        public static bool operator ==(Vector4D a, Vector4D b)      => a.X == b.X && a.Y == b.Y && a.Z == b.Z && a.W == b.W; 
        public static bool operator !=(Vector4D a, Vector4D b)      => !(a == b); 

        public static Vector4D Zero     => new Vector4D(0, 0, 0, 0);
        public static Vector4D One      => new Vector4D(1, 1, 1, 1);
        public static Vector4D Up       => new Vector4D(0, 1, 0, 0);
        public static Vector4D Down     => new Vector4D(0, -1, 0, 0);
        public static Vector4D Left     => new Vector4D(-1, 0, 0, 0);
        public static Vector4D Right    => new Vector4D(1, 0, 0, 0);
        public static Vector4D Forward  => new Vector4D(0, 0, 1, 0);
        public static Vector4D Back     => new Vector4D(0, 0, -1, 0);
        public double Magnitude         => System.Math.Sqrt(X * X + Y * Y + Z * Z + W * W);
        public double SqrAbs()          => X * X + Y * Y + Z * Z + W * W;
        public double Abs()             => System.Math.Sqrt(SqrAbs());  
        public Vector4D Normalized()
        {
            double length = Abs();
            if (length > Constants.EPS) return new Vector4D(X / length, Y / length, Z / length, W/length); 
            return new Vector4D(0, 0, 0, 0);
        }
        public Vector4D Cross(Vector4D other) => new Vector4D(
            x: Y * other.Z - Z * other.Y, 
            y: Z * other.X - X * other.Z, 
            z: X * other.Y - Y * other.X, 
            w: 0);
        public static double Dot(Vector4D a, Vector4D b)                => a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W;
        public static double Distance(Vector4D a, Vector4D b)           => (float)(a - b).Magnitude;
        public static Vector4D Lerp(Vector4D a, Vector4D b, float t)    => a + (b - a) * t;

        public override int GetHashCode()
        {
            unchecked
            {
                return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode() ^ W.GetHashCode();
            }
        }

        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj is Vector4D vector)
            {
                return this == vector;
            }
            return false;
        }

        public override string ToString() => "X: " + X + " Y: " + Y + "Z: " + Z + "W: " + W; 
    }
}
