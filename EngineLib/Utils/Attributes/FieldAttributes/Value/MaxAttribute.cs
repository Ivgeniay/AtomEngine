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
    Устанавливает максимально допустимое значение для числовых полей.
    
    namespace AtomEngine
    MaxAttribute(int maxValue)
    MaxAttribute(double maxValue)
    
    Этот атрибут осуществляет проверку значения числового поля или свойства
    и гарантирует, что оно не превышает заданного максимального значения.
    Может использоваться с любыми числовыми типами (int, float, double и т.д.),
    которые можно преобразовать в double.
    
    Параметры:
    - maxValue: Максимально допустимое значение для поля или свойства
    
    При валидации, если значение превышает указанный максимум, генерируется
    сообщение об ошибке: ""Значение {имя_поля} должно быть не больше {максимум}""
    
    Примеры использования:
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
