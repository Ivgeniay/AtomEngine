using System.ComponentModel.DataAnnotations;

namespace EngineLib
{
    /// <summary>
    /// Атрибут для установки максимальной длины строки
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    [Documentation(DocumentationSection = "Attribute", Name = "MaxLengthAttribute", Description = @"
    Атрибут для установки максимальной длины строки
", SubSection = "Inspector/Setter")]
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
