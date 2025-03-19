using EngineLib;
using System.ComponentModel.DataAnnotations;

namespace AtomEngine
{
    /// <summary>
    /// Атрибут для установки максимальной длины строки
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    [Documentation(
    DocumentationSection = "Core",
    Name = "MaxLengthAttribute",
    SubSection = "Attribute/Inspector/Setter",
    Description = @"
    Sets the maximum allowed length for string fields.

    namespace AtomEngine
    MaxLengthAttribute(int maxLength)

    This attribute validates the length of a string field or property
    and ensures that it does not exceed the specified maximum value.
    Applies only to string fields.

    Parameters:
    - maxLength: The maximum allowed string length in characters

    During validation, if the string length exceeds the specified maximum, an error message is generated: ""The length of the {field_name} string must not exceed {maximum} characters""

    Usage examples:
    public struct NameComponent : IComponent
    {
        [MaxLength(50)]
        public string PlayerName;
        
        [MaxLength(1000)]
        public string Description;
    }
    ",
    Author = "AtomEngine Team")]
    public class MaxLengthAttribute : ValidationAttribute
    {
        public readonly int MaxLength;

        public MaxLengthAttribute(int maxLength)
        {
            MaxLength = maxLength;
            ErrorMessage = "Длина строки {0} не должна превышать {1} символов";
        }

        public override bool IsValid(object value)
        {
            if (value == null)
                return true;

            string stringValue = value as string;
            if (stringValue == null)
                return false;

            return stringValue.Length <= MaxLength;
        }

        public override string FormatErrorMessage(string name)
        {
            return string.Format(ErrorMessage, name, MaxLength);
        }
    }
}
