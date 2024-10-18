using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtomEngine.Math
{
    public static class MathF
    {
        public static double Abs(double value) => value < 0 ? -value : value;
        public static double Sin(double value) => System.Math.Sin(value);
        public static double Cos(double value) => System.Math.Cos(value);
        public static double Tan(double value) => System.Math.Tan(value);
        public static double Asin(double value) => System.Math.Asin(value);
        public static double Acos(double value) => System.Math.Acos(value);
        public static double Atan(double value) => System.Math.Atan(value);
        public static double Atan2(double y, double x) => System.Math.Atan2(y, x);
        public static double Add(double a, double b) => a + b;
        public static double Subtract(double a, double b) => a - b;
        public static double Multiply(double a, double b) => a * b;
        public static double Divide(double a, double b) => a / b;
        public static double ConvertToDouble<T>(T value) => Convert.ToDouble(value);
        public static double Sqrt(double x)
        {
            if (x < 0)
                throw new ArgumentException("Cannot calculate square root of a negative number");

            if (x == 0)
                return 0;

            double guess = x / 2;
            double result = guess;

            while (true)
            {
                result = (result + x / result) / 2;

                if (MathF.Abs(result - guess) < Constants.EPS)
                    break;

                guess = result;
            }

            return result;
        }
        public static double Lerp(double a, double b, double t) => a + (b - a) * t;
        public static int LerpInt(int a, int b, double t) => (int)(a + (b - a) * t);

    }
}
