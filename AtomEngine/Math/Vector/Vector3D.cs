using System.Diagnostics.CodeAnalysis;

namespace AtomEngine.Math
{
    public struct Vector3D<T> where T : struct, IComparable, IConvertible, IFormattable
    {
        public T X;
        public T Y;
        public T Z;

        public Vector3D(Vector2D<T> source)
        {
            X = source.X;
            Y = source.Y;
            Z = default(T);
        }
        public Vector3D(Vector3D<T> source)
        {
            X = source.X;
            Y = source.Y;
            Z = source.Z;
        }
        public Vector3D(Vector4D<T> source)
        {
            X = source.X;
            Y = source.Y;
            Z = source.Z;
        }
        public Vector3D(T x, T y, T z)
        {
            X = x;
            Y = y;
            Z = z;
        }


        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj is Vector3D<T> vector)
            {
                return this == vector;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode();
        }

        public static Vector3D<T> operator +(Vector3D<T> a, Vector3D<T> b)
        {
            return new Vector3D<T>(GenericMath<T>.AddT(a.X, b.X), GenericMath<T>.AddT(a.Y, b.Y), GenericMath<T>.AddT(a.Z, b.Z));
        }

        public static Vector3D<T> operator -(Vector3D<T> a, Vector3D<T> b)
        {
            return new Vector3D<T>(GenericMath<T>.SubtractT(a.X, b.X), GenericMath<T>.SubtractT(a.Y, b.Y), GenericMath<T>.SubtractT(a.Z, b.Z));
        }

        public static Vector3D<T> operator *(Vector3D<T> a, T b)
        {
            return new Vector3D<T>(GenericMath<T>.MultiplyT(a.X, b), GenericMath<T>.MultiplyT(a.Y, b), GenericMath<T>.MultiplyT(a.Z, b));
        }

        public static Vector3D<T> operator /(Vector3D<T> a, T b)
        {
            return new Vector3D<T>(GenericMath<T>.DivideT(a.X, b), GenericMath<T>.DivideT(a.Y, b), GenericMath<T>.DivideT(a.Z, b));
        }

        public static bool operator ==(Vector3D<T> a, Vector3D<T> b)
        {
            return a.X.Equals(b.X) && a.Y.Equals(b.Y) && a.Z.Equals(b.Z);
        }

        public static bool operator !=(Vector3D<T> a, Vector3D<T> b)
        {
            return !(a == b);
        }

        public static Vector3D<T> Zero => new Vector3D<T>(default(T), default(T), default(T));
        public static Vector3D<T> One => new Vector3D<T>(GenericMath<T>.ConvertTo<T>(1), GenericMath<T>.ConvertTo<T>(1), GenericMath<T>.ConvertTo<T>(1));
        public static Vector3D<T> Up => new Vector3D<T>(default(T), GenericMath<T>.ConvertTo<T>(1), default(T));
        public static Vector3D<T> Down => new Vector3D<T>(default(T), GenericMath<T>.ConvertTo<T>(-1), default(T));
        public static Vector3D<T> Left => new Vector3D<T>(GenericMath<T>.ConvertTo<T>(-1), default(T), default(T));
        public static Vector3D<T> Right => new Vector3D<T>(GenericMath<T>.ConvertTo<T>(1), default(T), default(T));
        public static Vector3D<T> Forward => new Vector3D<T>(default(T), default(T), GenericMath<T>.ConvertTo<T>(1));
        public static Vector3D<T> Back => new Vector3D<T>(default(T), default(T), GenericMath<T>.ConvertTo<T>(-1));

        public double Magnitude => MathF.Sqrt(Convert.ToDouble(GenericMath<T>.MultiplyT(X, X)) + Convert.ToDouble(GenericMath<T>.MultiplyT(Y, Y)) + Convert.ToDouble(GenericMath<T>.MultiplyT(Z, Z)));
        public double SqrAbs() => Convert.ToDouble(X) * Convert.ToDouble(X) + Convert.ToDouble(Y) * Convert.ToDouble(Y) + Convert.ToDouble(Z) * Convert.ToDouble(Z);
        public Vector3D<T> Normalized => this / GenericMath<T>.ConvertTo<T>(Magnitude);

        public static T Dot(Vector3D<T> a, Vector3D<T> b)
        {
            var resX = GenericMath<T>.MultiplyT(a.X, b.X);
            var resY = GenericMath<T>.MultiplyT(a.Y, b.Y);
            var resZ = GenericMath<T>.MultiplyT(a.Z, b.Z);
            return GenericMath<T>.AddT(GenericMath<T>.AddT(resX, resY), resZ);
        }

        public static double Distance(Vector3D<T> a, Vector3D<T> b)
        {
            return (a - b).Magnitude;
        }

        public static Vector3D<T> Lerp(Vector3D<T> a, Vector3D<T> b, T t)
        {
            return a + (b - a) * t;
        }

        public override string ToString()
        {
            return "X: " + X + " Y: " + Y + " Z: " + Z;
        }

    }
    public struct Vector3D
    {
        public double X;
        public double Y;
        public double Z;
         
        public Vector3D(Vector2D source)
        {
            X = source.X;
            Y = source.Y;
            Z = 0;
        }
        public Vector3D(Vector3D source)
        {
            X = source.X;
            Y = source.Y;
            Z = source.Z;
        }
        public Vector3D(Vector4D source)
        {
            X = source.X;
            Y = source.Y;
            Z = source.Z;
        }
        public Vector3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }
        public Vector3D(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vector3D operator +(Vector3D a, Vector3D b)   => new Vector3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z); 
        public static Vector3D operator -(Vector3D a, Vector3D b)   => new Vector3D(a.X - b.X, a.Y - b.Y, a.Z - b.Z); 
        public static Vector3D operator *(Vector3D a, float b)      => new Vector3D(a.X * b, a.Y * b, a.Z * b); 
        public static Vector3D operator *(Vector3D a, double b)     => new Vector3D(a.X * b, a.Y * b, a.Z * b); 
        public static Vector3D operator /(Vector3D a, float b)      => new Vector3D(a.X / b, a.Y / b, a.Z / b); 
        public static Vector3D operator /(Vector3D a, double b)     => new Vector3D(a.X / b, a.Y / b, a.Z / b);
        public static bool operator ==(Vector3D a, Vector3D b)      => a.X == b.X && a.Y == b.Y && a.Z == b.Z;
        public static bool operator !=(Vector3D a, Vector3D b)      => !(a == b);

        public static Vector3D Zero         => new Vector3D(0, 0, 0);
        public static Vector3D One          => new Vector3D(1, 1, 1);
        public static Vector3D Up           => new Vector3D(0, 1, 0);
        public static Vector3D Down         => new Vector3D(0, -1, 0);
        public static Vector3D Left         => new Vector3D(-1, 0, 0);
        public static Vector3D Right        => new Vector3D(1, 0, 0);
        public static Vector3D Forward      => new Vector3D(0, 0, 1);
        public static Vector3D Back         => new Vector3D(0, 0, -1); 
        public double Magnitude             => System.Math.Sqrt(X * X + Y * Y + Z * Z);
        public double SqrAbs()              => X * X + Y * Y + Z * Z;
        public double Abs()                 => System.Math.Sqrt(SqrAbs());
        public Vector3D Normalized()
        {
            double length = Abs();
            if (length > Constants.EPS) return new Vector3D(X / length, Y / length, Z / length); 
            return new Vector3D(0, 0, 0);
        }
        public Vector3D Cross(Vector3D other)                           => new Vector3D(Y * other.Z - Z * other.Y, Z * other.X - X * other.Z, X * other.Y - Y * other.X);
        public static Vector3D Cross(Vector3D self, Vector3D other)     => new Vector3D(self.Y * other.Z - self.Z * other.Y, self.Z * other.X - self.X * other.Z, self.X * other.Y - self.Y * other.X);
        public static double Dot(Vector3D a, Vector3D b)                => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        public static double Distance(Vector3D a, Vector3D b)           => (a - b).Magnitude;
        public static Vector3D Lerp(Vector3D a, Vector3D b, float t)    => a + (b - a) * t;


        public override int GetHashCode()
        {
            unchecked {
                int hash = 17;
                hash = hash * 23 + X.GetHashCode();
                hash = hash * 23 + Y.GetHashCode();
                hash = hash * 23 + Z.GetHashCode();
                return hash;
            }
        }
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj == null) return false;
            if (obj is Vector3D vector)
            {
                return this == vector;
            }
            return false;
        }
        public override string ToString() => "X: " + X + " Y: " + Y + " Z: " + Z;

        internal static Vector3D Parse(string v)
        {
            v = v.Trim();
            string[] parts = v.Split(' ');

            double x = 0, y = 0, z = 0;

            foreach (string part in parts)
            {
                string[] keyValue = part.Split(':');
                if (keyValue.Length == 2)
                {
                    string key = keyValue[0].Trim();
                    string value = keyValue[1].Trim();

                    switch (key.ToUpper())
                    {
                        case "X":
                            if (!double.TryParse(value, out x))
                                throw new FormatException("Invalid X value");
                            break;
                        case "Y":
                            if (!double.TryParse(value, out y))
                                throw new FormatException("Invalid Y value");
                            break;
                        case "Z":
                            if (!double.TryParse(value, out z))
                                throw new FormatException("Invalid Z value");
                            break;
                    }
                }
            }

            return new Vector3D(x, y, z);
        }
    }
}
