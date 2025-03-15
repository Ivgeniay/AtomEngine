using System.ComponentModel.DataAnnotations;

namespace EngineLib
{
    /// <summary>
    /// Атрибут для установки максимального значения для числовых типов (int, float, double и т.д.)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class MaxAttribute : ValidationAttribute
    {
        public readonly double MaxValue;

        public MaxAttribute(int maxValue)
        {
            this.MaxValue = maxValue;
            ErrorMessage = "Значение {0} должно быть не больше {1}";
        }
        public MaxAttribute(double maxValue)
        {
            MaxValue = maxValue;
            ErrorMessage = "Значение {0} должно быть не больше {1}";
        }

        public override bool IsValid(object value)
        {
            if (value == null)
                return true;

            double valueToCompare;

            try
            {
                valueToCompare = Convert.ToDouble(value);
            }
            catch (InvalidCastException)
            {
                return false;
            }

            return valueToCompare <= MaxValue;
        }

        public override string FormatErrorMessage(string name)
        {
            return string.Format(ErrorMessage, name, MaxValue);
        }
    }
}
