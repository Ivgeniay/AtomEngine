using System.ComponentModel.DataAnnotations;
using EngineLib;

namespace AtomEngine
{
    /// <summary>
    /// Атрибут для установки диапазона значений для числовых типов (int, float, double и т.д.)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    [Documentation(
    DocumentationSection = "Core",
    Name = "RangeAttribute",
    SubSection = "Attribute/Inspector/Setter",
    Description = @"
    Sets the allowed range of values ​​for numeric fields.

    namespace AtomEngine
    RangeAttribute(float minValue, float maxValue)
    RangeAttribute(int minValue, int maxValue)

    This attribute validates the value of a numeric field or property and ensures that it is in the specified range between the minimum and
    maximum values, inclusive. It can be used with any numeric
    type (int, float, double, etc.) that can be converted to double.

    Parameters:
    - minValue: The minimum allowed value for the field or property
    - maxValue: The maximum allowed value for the field or property

    During validation, if the value is outside the specified range, an error message is generated: ""The value of {field_name} must be between {minimum} and {maximum}""

    Usage examples:
    public struct ColorComponent : IComponent
    {
        [Range(0, 255)]
        public int Red;
        
        [Range(0, 255)]
        public int Green;
        
        [Range(0, 255)]
        public int Blue;
        
        [Range(0.0f, 1.0f)]
        public float Alpha;
    }
    ",
    Author = "AtomEngine Team")]
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
