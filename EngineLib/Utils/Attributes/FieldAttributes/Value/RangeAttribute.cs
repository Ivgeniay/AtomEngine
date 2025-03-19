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
    Устанавливает допустимый диапазон значений для числовых полей.
    
    namespace AtomEngine
    RangeAttribute(float minValue, float maxValue)
    RangeAttribute(int minValue, int maxValue)
    
    Этот атрибут осуществляет проверку значения числового поля или свойства
    и гарантирует, что оно находится в заданном диапазоне между минимальным и
    максимальным значением включительно. Может использоваться с любыми числовыми
    типами (int, float, double и т.д.), которые можно преобразовать в double.
    
    Параметры:
    - minValue: Минимально допустимое значение для поля или свойства
    - maxValue: Максимально допустимое значение для поля или свойства
    
    При валидации, если значение выходит за указанный диапазон, генерируется
    сообщение об ошибке: ""Значение {имя_поля} должно быть между {минимум} и {максимум}""
    
    Примеры использования:
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
