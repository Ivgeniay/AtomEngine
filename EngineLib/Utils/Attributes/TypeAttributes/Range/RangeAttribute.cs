using System.ComponentModel.DataAnnotations;

namespace EngineLib
{
    /// <summary>
    /// Атрибут для установки диапазона значений для числовых типов (int, float, double и т.д.)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class RangeAttribute : ValidationAttribute
    {
        public readonly double MinValue;
        public readonly double MaxValue;

        public RangeAttribute(float minValue, float maxValue)
        {
            MinValue = minValue; 
            MaxValue = maxValue;
            ErrorMessage = "Значение {0} должно быть между {1} и {2}";
        }
        public RangeAttribute(int minValue, int maxValue)
        {
            MinValue = minValue; 
            MaxValue = maxValue;
            ErrorMessage = "Значение {0} должно быть между {1} и {2}";
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

            return valueToCompare >= MinValue && valueToCompare <= MaxValue;
        }

        public override string FormatErrorMessage(string name)
        {
            return string.Format(ErrorMessage, name, MinValue, MaxValue);
        }
    }
}
