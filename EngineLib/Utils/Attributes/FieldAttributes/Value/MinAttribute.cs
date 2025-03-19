using EngineLib;
using System.ComponentModel.DataAnnotations;

namespace AtomEngine
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    [Documentation(
    DocumentationSection = "Core",
    Name = "MinAttribute",
    SubSection = "Attribute/Inspector/Setter",
    Description = @"
    Sets the minimum allowed value for numeric fields.

    namespace AtomEngine
    MinAttribute(int minValue)
    MinAttribute(double minValue)

    This attribute validates the value of a numeric field or property
    and ensures that it is not less than the specified minimum value.

    Can be used with any numeric type (int, float, double, etc.)
    that can be converted to double.

    Parameters:
    - minValue: The minimum allowed value for the field or property

    During validation, if the value is less than the specified minimum, an error message is generated: ""The value of {field_name} must be at least {minimum}""

    Usage examples:
    public struct PowerComponent : IComponent
    {
        [Min(0)]
        public int Energy;
        
        [Min(0.01)]
        public float Speed;
    }
    ",
    Author = "AtomEngine Team")]
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
