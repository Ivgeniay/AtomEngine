using System.ComponentModel.DataAnnotations;
using EngineLib;

namespace AtomEngine
{
    /// <summary>
    /// Атрибут для установки максимального значения для числовых типов (int, float, double и т.д.)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    [Documentation(
    DocumentationSection = "Core",
    Name = "MaxAttribute",
    SubSection = "Attribute/Inspector/Setter",
    Description = @"
    Sets the maximum allowed value for numeric fields.

    namespace AtomEngine
    MaxAttribute(int maxValue)
    MaxAttribute(double maxValue)

    This attribute validates the value of a numeric field or property
    and ensures that it does not exceed the specified maximum value.

    Can be used with any numeric type (int, float, double, etc.)
    that can be converted to double.

    Parameters:
    - maxValue: The maximum allowed value for the field or property

    During validation, if the value exceeds the specified maximum, an error message is generated: ""The value of {field_name} must be less than or equal to {maximum}""

    Usage examples:
    public struct StatsComponent : IComponent
    {
        [Max(100)]
        public int HealthPoints;
        
        [Max(1.0)]
        public float NormalizedValue;
    }
    ",
    Author = "AtomEngine Team")]
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
