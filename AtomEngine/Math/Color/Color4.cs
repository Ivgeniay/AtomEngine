namespace AtomEngine.Math
{
    public struct Color4
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;

        public Color4(byte r, byte g, byte b, byte a)
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
         
        public static explicit operator System.Drawing.Color(Color4 color) =>
            System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);

        public static explicit operator Color4(Vector3D color) =>
            new Color4((byte)color.X, (byte)color.Y, (byte)color.Z, 1);
        public static explicit operator Color4(Vector4D color) =>
            new Color4((byte)color.X, (byte)color.Y, (byte)color.Z, (byte)color.W);
        public static explicit operator double[](Color4 color) =>
            new double[] { color.R, color.G, color.B, color.A };
        public static explicit operator float[](Color4 color) =>
            new float[] { (float)color.R, (float)color.G, (float)color.B, (float)color.A };

        public static Color4 operator +(Color4 a, Color4 b) =>
            new Color4((byte)(a.R + b.R), (byte)(a.G + b.G), (byte)(a.B + b.B), (byte)(a.A + b.A)); 
        public static Color4 operator -(Color4 a, Color4 b) =>
            new Color4((byte)(a.R - b.R), (byte)(a.G - b.G), (byte)(a.B - b.B), (byte)(a.A - b.A)); 
        public static Color4 operator *(Color4 a, double b) =>
            new Color4((byte)(a.R * b), (byte)(a.G * b), (byte)(a.B * b), (byte)(a.A * b));
        public static Color4 operator *(double a, Color4 b) =>
            new Color4((byte)(a * b.R), (byte)(a * b.G), (byte)(a * b.B), (byte)(a * b.A)); 
        public static Color4 operator /(Color4 a, double b) =>
            new Color4((byte)(a.R / b), (byte)(a.G / b), (byte)(a.B / b), (byte)(a.A / b)); 
        public static Color4 operator /(double a, Color4 b) =>
            new Color4((byte)(a / b.R), (byte)(a / b.G), (byte)(a / b.B), (byte)(a / b.A));
        public static bool operator ==(Color4 a, Color4 b) =>
            a.R == b.R && a.G == b.G && a.B == b.B && a.A == b.A;
        public static bool operator !=(Color4 a, Color4 b) =>
            a.R != b.R || a.G != b.G || a.B != b.B || a.A != b.A;

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            return obj is Color4 color && this == color;
        }

        public static Color4 Transparent => new Color4(byte.MaxValue, byte.MaxValue, byte.MaxValue, 0); 
        public static Color4 AliceBlue => new Color4(240, 248, byte.MaxValue, byte.MaxValue); 
        public static Color4 AntiqueWhite => new Color4(250, 235, 215, byte.MaxValue); 
        public static Color4 Aqua => new Color4(0, byte.MaxValue, byte.MaxValue, byte.MaxValue); 
        public static Color4 Aquamarine => new Color4(127, byte.MaxValue, 212, byte.MaxValue); 
        public static Color4 Azure => new Color4(240, byte.MaxValue, byte.MaxValue, byte.MaxValue); 
        public static Color4 Beige => new Color4(245, 245, 220, byte.MaxValue); 
        public static Color4 Bisque => new Color4(byte.MaxValue, 228, 196, byte.MaxValue); 
        public static Color4 Black => new Color4(0, 0, 0, byte.MaxValue); 
        public static Color4 BlanchedAlmond => new Color4(byte.MaxValue, 235, 205, byte.MaxValue); 
        public static Color4 Blue => new Color4(0, 0, byte.MaxValue, byte.MaxValue); 
        public static Color4 BlueViolet => new Color4(138, 43, 226, byte.MaxValue); 
        public static Color4 Brown => new Color4(165, 42, 42, byte.MaxValue); 
        public static Color4 BurlyWood => new Color4(222, 184, 135, byte.MaxValue); 
        public static Color4 CadetBlue => new Color4(95, 158, 160, byte.MaxValue); 
        public static Color4 Chartreuse => new Color4(127, byte.MaxValue, 0, byte.MaxValue); 
        public static Color4 Chocolate => new Color4(210, 105, 30, byte.MaxValue); 
        public static Color4 Coral => new Color4(byte.MaxValue, 127, 80, byte.MaxValue); 
        public static Color4 CornflowerBlue => new Color4(100, 149, 237, byte.MaxValue); 
        public static Color4 Cornsilk => new Color4(byte.MaxValue, 248, 220, byte.MaxValue); 
        public static Color4 Crimson => new Color4(220, 20, 60, byte.MaxValue); 
        public static Color4 Cyan => new Color4(0, byte.MaxValue, byte.MaxValue, byte.MaxValue); 
        public static Color4 DarkBlue => new Color4(0, 0, 139, byte.MaxValue); 
        public static Color4 DarkCyan => new Color4(0, 139, 139, byte.MaxValue); 
        public static Color4 DarkGoldenrod => new Color4(184, 134, 11, byte.MaxValue); 
        public static Color4 DarkGray => new Color4(169, 169, 169, byte.MaxValue); 
        public static Color4 DarkGreen => new Color4(0, 100, 0, byte.MaxValue); 
        public static Color4 DarkKhaki => new Color4(189, 183, 107, byte.MaxValue); 
        public static Color4 DarkMagenta => new Color4(139, 0, 139, byte.MaxValue); 
        public static Color4 DarkOliveGreen => new Color4(85, 107, 47, byte.MaxValue); 
        public static Color4 DarkOrange => new Color4(byte.MaxValue, 140, 0, byte.MaxValue); 
        public static Color4 DarkOrchid => new Color4(153, 50, 204, byte.MaxValue); 
        public static Color4 DarkRed => new Color4(139, 0, 0, byte.MaxValue); 
        public static Color4 DarkSalmon => new Color4(233, 150, 122, byte.MaxValue); 
        public static Color4 DarkSeaGreen => new Color4(143, 188, 139, byte.MaxValue); 
        public static Color4 DarkSlateBlue => new Color4(72, 61, 139, byte.MaxValue); 
        public static Color4 DarkSlateGray => new Color4(47, 79, 79, byte.MaxValue); 
        public static Color4 DarkTurquoise => new Color4(0, 206, 209, byte.MaxValue); 
        public static Color4 DarkViolet => new Color4(148, 0, 211, byte.MaxValue); 
        public static Color4 DeepPink => new Color4(byte.MaxValue, 20, 147, byte.MaxValue); 
        public static Color4 DeepSkyBlue => new Color4(0, 191, byte.MaxValue, byte.MaxValue); 
        public static Color4 DimGray => new Color4(105, 105, 105, byte.MaxValue); 
        public static Color4 DodgerBlue => new Color4(30, 144, byte.MaxValue, byte.MaxValue); 
        public static Color4 Firebrick => new Color4(178, 34, 34, byte.MaxValue); 
        public static Color4 FloralWhite => new Color4(byte.MaxValue, 250, 240, byte.MaxValue); 
        public static Color4 ForestGreen => new Color4(34, 139, 34, byte.MaxValue); 
        public static Color4 Fuchsia => new Color4(byte.MaxValue, 0, byte.MaxValue, byte.MaxValue); 
        public static Color4 Gainsboro => new Color4(220, 220, 220, byte.MaxValue); 
        public static Color4 GhostWhite => new Color4(248, 248, byte.MaxValue, byte.MaxValue); 
        public static Color4 Gold => new Color4(byte.MaxValue, 215, 0, byte.MaxValue); 
        public static Color4 Goldenrod => new Color4(218, 165, 32, byte.MaxValue); 
        public static Color4 Gray => new Color4(128, 128, 128, byte.MaxValue); 
        public static Color4 Green => new Color4(0, 128, 0, byte.MaxValue); 
        public static Color4 GreenYellow => new Color4(173, byte.MaxValue, 47, byte.MaxValue); 
        public static Color4 Honeydew => new Color4(240, byte.MaxValue, 240, byte.MaxValue); 
        public static Color4 HotPink => new Color4(byte.MaxValue, 105, 180, byte.MaxValue); 
        public static Color4 IndianRed => new Color4(205, 92, 92, byte.MaxValue); 
        public static Color4 Indigo => new Color4(75, 0, 130, byte.MaxValue); 
        public static Color4 Ivory => new Color4(byte.MaxValue, byte.MaxValue, 240, byte.MaxValue); 
        public static Color4 Khaki => new Color4(240, 230, 140, byte.MaxValue); 
        public static Color4 Lavender => new Color4(230, 230, 250, byte.MaxValue); 
        public static Color4 LavenderBlush => new Color4(byte.MaxValue, 240, 245, byte.MaxValue); 
        public static Color4 LawnGreen => new Color4(124, 252, 0, byte.MaxValue); 
        public static Color4 LemonChiffon => new Color4(byte.MaxValue, 250, 205, byte.MaxValue); 
        public static Color4 LightBlue => new Color4(173, 216, 230, byte.MaxValue); 
        public static Color4 LightCoral => new Color4(240, 128, 128, byte.MaxValue); 
        public static Color4 LightCyan => new Color4(224, byte.MaxValue, byte.MaxValue, byte.MaxValue); 
        public static Color4 LightGoldenrodYellow => new Color4(250, 250, 210, byte.MaxValue); 
        public static Color4 LightGreen => new Color4(144, 238, 144, byte.MaxValue); 
        public static Color4 LightGray => new Color4(211, 211, 211, byte.MaxValue); 
        public static Color4 LightPink => new Color4(byte.MaxValue, 182, 193, byte.MaxValue); 
        public static Color4 LightSalmon => new Color4(byte.MaxValue, 160, 122, byte.MaxValue); 
        public static Color4 LightSeaGreen => new Color4(32, 178, 170, byte.MaxValue); 
        public static Color4 LightSkyBlue => new Color4(135, 206, 250, byte.MaxValue); 
        public static Color4 LightSlateGray => new Color4(119, 136, 153, byte.MaxValue); 
        public static Color4 LightSteelBlue => new Color4(176, 196, 222, byte.MaxValue); 
        public static Color4 LightYellow => new Color4(byte.MaxValue, byte.MaxValue, 224, byte.MaxValue); 
        public static Color4 Lime => new Color4(0, byte.MaxValue, 0, byte.MaxValue); 
        public static Color4 LimeGreen => new Color4(50, 205, 50, byte.MaxValue); 
        public static Color4 Linen => new Color4(250, 240, 230, byte.MaxValue); 
        public static Color4 Magenta => new Color4(byte.MaxValue, 0, byte.MaxValue, byte.MaxValue); 
        public static Color4 Maroon => new Color4(128, 0, 0, byte.MaxValue); 
        public static Color4 MediumAquamarine => new Color4(102, 205, 170, byte.MaxValue); 
        public static Color4 MediumBlue => new Color4(0, 0, 205, byte.MaxValue); 
        public static Color4 MediumOrchid => new Color4(186, 85, 211, byte.MaxValue); 
        public static Color4 MediumPurple => new Color4(147, 112, 219, byte.MaxValue); 
        public static Color4 MediumSeaGreen => new Color4(60, 179, 113, byte.MaxValue); 
        public static Color4 MediumSlateBlue => new Color4(123, 104, 238, byte.MaxValue); 
        public static Color4 MediumSpringGreen => new Color4(0, 250, 154, byte.MaxValue); 
        public static Color4 MediumTurquoise => new Color4(72, 209, 204, byte.MaxValue); 
        public static Color4 MediumVioletRed => new Color4(199, 21, 133, byte.MaxValue); 
        public static Color4 MidnightBlue => new Color4(25, 25, 112, byte.MaxValue); 
        public static Color4 MintCream => new Color4(245, byte.MaxValue, 250, byte.MaxValue); 
        public static Color4 MistyRose => new Color4(byte.MaxValue, 228, 225, byte.MaxValue); 
        public static Color4 Moccasin => new Color4(byte.MaxValue, 228, 181, byte.MaxValue); 
        public static Color4 NavajoWhite => new Color4(byte.MaxValue, 222, 173, byte.MaxValue); 
        public static Color4 Navy => new Color4(0, 0, 128, byte.MaxValue); 
        public static Color4 OldLace => new Color4(253, 245, 230, byte.MaxValue); 
        public static Color4 Olive => new Color4(128, 128, 0, byte.MaxValue); 
        public static Color4 OliveDrab => new Color4(107, 142, 35, byte.MaxValue); 
        public static Color4 Orange => new Color4(byte.MaxValue, 165, 0, byte.MaxValue); 
        public static Color4 OrangeRed => new Color4(byte.MaxValue, 69, 0, byte.MaxValue); 
        public static Color4 Orchid => new Color4(218, 112, 214, byte.MaxValue); 
        public static Color4 PaleGoldenrod => new Color4(238, 232, 170, byte.MaxValue); 
        public static Color4 PaleGreen => new Color4(152, 251, 152, byte.MaxValue); 
        public static Color4 PaleTurquoise => new Color4(175, 238, 238, byte.MaxValue); 
        public static Color4 PaleVioletRed => new Color4(219, 112, 147, byte.MaxValue); 
        public static Color4 PapayaWhip => new Color4(byte.MaxValue, 239, 213, byte.MaxValue); 
        public static Color4 PeachPuff => new Color4(byte.MaxValue, 218, 185, byte.MaxValue); 
        public static Color4 Peru => new Color4(205, 133, 63, byte.MaxValue); 
        public static Color4 Pink => new Color4(byte.MaxValue, 192, 203, byte.MaxValue); 
        public static Color4 Plum => new Color4(221, 160, 221, byte.MaxValue); 
        public static Color4 PowderBlue => new Color4(176, 224, 230, byte.MaxValue); 
        public static Color4 Purple => new Color4(128, 0, 128, byte.MaxValue); 
        public static Color4 Red => new Color4(byte.MaxValue, 0, 0, byte.MaxValue); 
        public static Color4 RosyBrown => new Color4(188, 143, 143, byte.MaxValue); 
        public static Color4 RoyalBlue => new Color4(65, 105, 225, byte.MaxValue); 
        public static Color4 SaddleBrown => new Color4(139, 69, 19, byte.MaxValue); 
        public static Color4 Salmon => new Color4(250, 128, 114, byte.MaxValue); 
        public static Color4 SandyBrown => new Color4(244, 164, 96, byte.MaxValue); 
        public static Color4 SeaGreen => new Color4(46, 139, 87, byte.MaxValue); 
        public static Color4 SeaShell => new Color4(byte.MaxValue, 245, 238, byte.MaxValue); 
        public static Color4 Sienna => new Color4(160, 82, 45, byte.MaxValue); 
        public static Color4 Silver => new Color4(192, 192, 192, byte.MaxValue); 
        public static Color4 SkyBlue => new Color4(135, 206, 235, byte.MaxValue); 
        public static Color4 SlateBlue => new Color4(106, 90, 205, byte.MaxValue); 
        public static Color4 SlateGray => new Color4(112, 128, 144, byte.MaxValue); 
        public static Color4 Snow => new Color4(byte.MaxValue, 250, 250, byte.MaxValue); 
        public static Color4 SpringGreen => new Color4(0, byte.MaxValue, 127, byte.MaxValue); 
        public static Color4 SteelBlue => new Color4(70, 130, 180, byte.MaxValue); 
        public static Color4 Tan => new Color4(210, 180, 140, byte.MaxValue); 
        public static Color4 Teal => new Color4(0, 128, 128, byte.MaxValue); 
        public static Color4 Thistle => new Color4(216, 191, 216, byte.MaxValue); 
        public static Color4 Tomato => new Color4(byte.MaxValue, 99, 71, byte.MaxValue); 
        public static Color4 Turquoise => new Color4(64, 224, 208, byte.MaxValue); 
        public static Color4 Violet => new Color4(238, 130, 238, byte.MaxValue); 
        public static Color4 Wheat => new Color4(245, 222, 179, byte.MaxValue); 
        public static Color4 White => new Color4(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue); 
        public static Color4 WhiteSmoke => new Color4(245, 245, 245, byte.MaxValue); 
        public static Color4 Yellow => new Color4(byte.MaxValue, byte.MaxValue, 0, byte.MaxValue); 
        public static Color4 YellowGreen => new Color4(154, 205, 50, byte.MaxValue);
    }
}
