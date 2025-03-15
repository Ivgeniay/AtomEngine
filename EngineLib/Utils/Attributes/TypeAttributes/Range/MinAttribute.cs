using System.ComponentModel.DataAnnotations;

namespace EngineLib
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class MinAttribute : ValidationAttribute
    {
        public readonly double MinValue;

        public MinAttribute(int minValue)
        {
            MinValue = minValue;
            ErrorMessage = "Значение {0} должно быть не меньше {1}";
        }

        public MinAttribute(double minValue)
        {
            MinValue = minValue;
            ErrorMessage = "Значение {0} должно быть не меньше {1}";
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

            return valueToCompare >= MinValue;
        }

        public override string FormatErrorMessage(string name)
        {
            return string.Format(ErrorMessage, name, MinValue);
        }
    }
}
