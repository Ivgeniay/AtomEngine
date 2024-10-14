namespace AtomEngine.Math
{
    internal static class GenericMath<T>
    {
        internal static T AddT(T a, T b) => (T)Convert.ChangeType(Convert.ToDouble(a) + Convert.ToDouble(b), typeof(T));
        internal static T SubtractT(T a, T b) => (T)Convert.ChangeType(Convert.ToDouble(a) - Convert.ToDouble(b), typeof(T));
        internal static T MultiplyT(T a, T b) => (T)Convert.ChangeType(Convert.ToDouble(a) * Convert.ToDouble(b), typeof(T));
        internal static T DivideT(T a, T b) => (T)Convert.ChangeType(Convert.ToDouble(a) / Convert.ToDouble(b), typeof(T));
        internal static T ConvertTo<T>(double value) => (T)Convert.ChangeType(value, typeof(T));
    }
}
