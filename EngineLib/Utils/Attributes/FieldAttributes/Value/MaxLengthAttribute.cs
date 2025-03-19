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
    Устанавливает максимально допустимую длину для строковых полей.
    
    namespace AtomEngine
    MaxLengthAttribute(int maxLength)
    
    Этот атрибут осуществляет проверку длины строкового поля или свойства
    и гарантирует, что она не превышает заданного максимального значения.
    Применяется только к полям типа string.
    
    Параметры:
    - maxLength: Максимально допустимая длина строки в символах
    
    При валидации, если длина строки превышает указанный максимум, генерируется
    сообщение об ошибке: ""Длина строки {имя_поля} не должна превышать {максимум} символов""
    
    Примеры использования:
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
