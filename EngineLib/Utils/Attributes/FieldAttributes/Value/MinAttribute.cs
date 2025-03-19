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
    Устанавливает минимально допустимое значение для числовых полей.
    
    namespace AtomEngine
    MinAttribute(int minValue)
    MinAttribute(double minValue)
    
    Этот атрибут осуществляет проверку значения числового поля или свойства
    и гарантирует, что оно не меньше заданного минимального значения.
    Может использоваться с любыми числовыми типами (int, float, double и т.д.),
    которые можно преобразовать в double.
    
    Параметры:
    - minValue: Минимально допустимое значение для поля или свойства
    
    При валидации, если значение меньше указанного минимума, генерируется
    сообщение об ошибке: ""Значение {имя_поля} должно быть не меньше {минимум}""
    
    Примеры использования:
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
