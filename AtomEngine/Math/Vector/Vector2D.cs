namespace AtomEngine.Math
{
    public struct Vector2D<T> where T : struct, IComparable, IConvertible, IFormattable
    {
        public T X;
        public T Y;

        public Vector2D(Vector2D<T> source)
        {
            X = source.X;
            Y = source.Y;
        }
        public Vector2D(Vector3D<T> source)
        {
            X = source.X;
            Y = source.Y;
        }
        public Vector2D(Vector4D<T> source)
        {
            X = source.X;
            Y = source.Y;
        }
        public Vector2D(T x, T y)
        {
            X = x;
            Y = y;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj is Vector2D<T> vector)
            {
                return this == vector;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode();
        }

        public static Vector2D<T> operator +(Vector2D<T> a, Vector2D<T> b)
        {
            return new Vector2D<T>(GenericMath<T>.AddT(a.X, b.X), GenericMath<T>.AddT(a.Y, b.Y));
        }

        public static Vector2D<T> operator -(Vector2D<T> a, Vector2D<T> b)
        {
            return new Vector2D<T>(GenericMath<T>.SubtractT(a.X, b.X), GenericMath<T>.SubtractT(a.Y, b.Y));
        }

        public static Vector2D<T> operator *(Vector2D<T> a, T b)
        {
            return new Vector2D<T>(GenericMath<T>.MultiplyT(a.X, b), GenericMath<T>.MultiplyT(a.Y, b));
        }

        public static Vector2D<T> operator /(Vector2D<T> a, T b)
        {
            return new Vector2D<T>(GenericMath<T>.DivideT(a.X, b), GenericMath<T>.DivideT(a.Y, b));
        }

        public static bool operator ==(Vector2D<T> a, Vector2D<T> b)
        {
            return a.X.Equals(b.X) && a.Y.Equals(b.Y);
        }

        public static bool operator !=(Vector2D<T> a, Vector2D<T> b)
        {
            return !(a == b);
        }

        public static Vector2D<T> Zero => new Vector2D<T>(default(T), default(T));
        public static Vector2D<T> One => new Vector2D<T>(GenericMath<T>.ConvertTo<T>(1), GenericMath<T>.ConvertTo<T>(1));
        public static Vector2D<T> Up => new Vector2D<T>(default(T), GenericMath<T>.ConvertTo<T>(1));
        public static Vector2D<T> Down => new Vector2D<T>(default(T), GenericMath<T>.ConvertTo<T>(-1));
        public static Vector2D<T> Left => new Vector2D<T>(GenericMath<T>.ConvertTo<T>(-1), default(T));
        public static Vector2D<T> Right => new Vector2D<T>(GenericMath<T>.ConvertTo<T>(1), default(T));

        public double Magnitude => MathF.Sqrt(Convert.ToDouble(GenericMath<T>.MultiplyT(X, X)) + Convert.ToDouble(GenericMath<T>.MultiplyT(Y, Y)));
        public double SqrAbs() => Convert.ToDouble(X) * Convert.ToDouble(X) + Convert.ToDouble(Y) * Convert.ToDouble(Y);
        public Vector2D<T> Normalized => this / GenericMath<T>.ConvertTo<T>(Magnitude);

        public static T Dot(Vector2D<T> a, Vector2D<T> b)
        {
            return GenericMath<T>.AddT(GenericMath<T>.MultiplyT(a.X, b.X), GenericMath<T>.MultiplyT(a.Y, b.Y));
        }

        public static double Distance(Vector2D<T> a, Vector2D<T> b)
        {
            return (a - b).Magnitude;
        }

        public static Vector2D<T> Lerp(Vector2D<T> a, Vector2D<T> b, T t)
        {
            return a + (b - a) * t;
        } 

        public override string ToString()
        {
            return "X: " + X + " Y: " + Y;
        }

    }
    public struct Vector2D
    { 
        public double X;
        public double Y;

        public Vector2D(Vector2D source)
        {
            X = source.X;
            Y = source.Y;
        }
        public Vector2D(Vector3D source)
        {
            X = source.X;
            Y = source.Y;
        }
        public Vector2D(Vector4D source)
        {
            X = source.X;
            Y = source.Y;
        }
        public Vector2D(float x, float y)
        {
            X = x;
            Y = y;
        }
        public Vector2D(double x, double y)
        {
            X = x;
            Y = y;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj is Vector2D vector)
            {
                return this == vector;
            }
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return X.GetHashCode() ^ Y.GetHashCode();
            }
        }

        public static Vector2D operator +(Vector2D a, Vector2D b)   =>  new Vector2D(a.X + b.X, a.Y + b.Y); 
        public static Vector2D operator -(Vector2D a, Vector2D b)   =>  new Vector2D(a.X - b.X, a.Y - b.Y);
        public static Vector2D operator *(Vector2D a, float b)      =>  new Vector2D(a.X * b, a.Y * b); 
        public static Vector2D operator *(Vector2D a, double b)     =>  new Vector2D(a.X * b, a.Y * b); 
        public static Vector2D operator /(Vector2D a, float b)      =>  new Vector2D(a.X / b, a.Y / b); 
        public static Vector2D operator /(Vector2D a, double b)     =>  new Vector2D(a.X / b, a.Y / b); 

        public static bool operator ==(Vector2D a, Vector2D b)      =>  a.X == b.X && a.Y == b.Y;
        public static bool operator !=(Vector2D a, Vector2D b)      =>  !(a == b);
        public static Vector2D Zero     => new Vector2D(0, 0);
        public static Vector2D One      => new Vector2D(1, 1);
        public static Vector2D Up       => new Vector2D(0, 1);
        public static Vector2D Down     => new Vector2D(0, -1);
        public static Vector2D Left     => new Vector2D(-1, 0);
        public static Vector2D Right    => new Vector2D(1, 0);
        public static double Dot(Vector2D a, Vector2D b)                => a.X * b.X + a.Y * b.Y;  
        public static double Distance(Vector2D a, Vector2D b)           => (a - b).Magnitude;  
        public static Vector2D Lerp(Vector2D a, Vector2D b, float t)    => a + (b - a) * t; 

        public double Magnitude         => (double)System.Math.Sqrt(X * X + Y * Y);
        public double SqrAbs()          => X * X + Y * Y;
        public double Abs()             => System.Math.Sqrt(SqrAbs());
        public Vector2D Normalized()
        {
            double length = (double)Abs();
            if (length > Constants.EPS) return new Vector2D(X / length, Y / length);
            return new Vector2D(0, 0);
        } 


        public override string ToString() => "X: " + X + " Y: " + Y; 
    }
}
