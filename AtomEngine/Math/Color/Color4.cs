namespace AtomEngine.Math
{
    public struct Color4
    {
        public double R;
        public double G;
        public double B;
        public double A;

        public Color4(double r, double g, double b, double a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public Color4(Color4 color)
        {
            R = color.R;
            G = color.G;
            B = color.B;
            A = color.A;
        }

        public Color4()
        {
            R = 0;
            G = 0;
            B = 0;
            A = 0;
        }

        public static Color4 operator +(Color4 a, Color4 b)
        {
            return new Color4(a.R + b.R, a.G + b.G, a.B + b.B, a.A + b.A);
        }

        public static Color4 operator -(Color4 a, Color4 b)
        {
            return new Color4(a.R - b.R, a.G - b.G, a.B - b.B, a.A - b.A);
        }

        public static Color4 operator *(Color4 a, double b)
        {
            return new Color4(a.R * b, a.G * b, a.B * b, a.A * b);
        }

        public static Color4 operator *(double a, Color4 b)
        {
            return new Color4(a * b.R, a * b.G, a * b.B, a * b.A);
        }

        public static Color4 operator /(Color4 a, double b)
        {
            return new Color4(a.R / b, a.G / b, a.B / b, a.A / b);
        }

        public static Color4 operator /(double a, Color4 b)
        {
            return new Color4(a / b.R, a / b.G, a / b.B, a / b.A);
        }

        public static bool operator ==(Color4 a, Color4 b)
        {
            return a.R == b.R && a.G == b.G && a.B == b.B && a.A == b.A;
        }

        public static bool operator !=(Color4 a, Color4 b)
        {
            return a.R != b.R || a.G != b.G || a.B != b.B || a.A != b.A;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            return obj is Color4 color && this == color;
        }
    }
}
