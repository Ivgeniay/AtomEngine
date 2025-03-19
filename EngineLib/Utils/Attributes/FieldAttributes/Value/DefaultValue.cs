using EngineLib;
using System.Numerics;

namespace AtomEngine
{
    [Documentation(
    DocumentationSection = "Core",
    Name = "DefaultValueAttribute",
    SubSection = "Attribute/Inspector/Setter",
    Description = @"
    Базовый абстрактный атрибут для всех атрибутов установки значений по умолчанию.
    
    namespace AtomEngine
    DefaultValueAttribute()
    
    Этот атрибут служит базовым классом для конкретных атрибутов установки значений по умолчанию
    для различных типов данных. Не используется напрямую, вместо него следует использовать
    специализированные атрибуты, такие как DefaultIntAttribute, DefaultFloatAttribute и т.д.
    ",
    Author = "AtomEngine Team")]
    public abstract class DefaultValueAttribute : Attribute { }


    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    [Documentation(
    DocumentationSection = "Core",
    Name = "DefaultIntAttribute",
    SubSection = "Attribute/Inspector/Setter",
    Description = @"
    Устанавливает значение по умолчанию для полей типа int.
    
    namespace AtomEngine
    DefaultIntAttribute(int value)
    
    Этот атрибут используется для задания начального значения целочисленных полей.
    При создании компонента с таким атрибутом, соответствующее поле будет 
    автоматически инициализировано указанным значением.
    
    Параметры:
    - value: Целочисленное значение по умолчанию
    
    Примеры использования:
    public struct PlayerComponent : IComponent
    {
        [DefaultInt(100)]
        public int Health;
    }
    ",
    Author = "AtomEngine Team")]
    public class DefaultIntAttribute : DefaultValueAttribute
    {
        public readonly int Value;

        public DefaultIntAttribute(int value)
        {
            Value = value;
        }

    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    [Documentation(
    DocumentationSection = "Core",
    Name = "DefaultFloatAttribute",
    SubSection = "Attribute/Inspector/Setter",
    Description = @"
    Устанавливает значение по умолчанию для полей типа float.
    
    namespace AtomEngine
    DefaultFloatAttribute(float value)
    
    Этот атрибут используется для задания начального значения полей с плавающей точкой.
    При создании компонента с таким атрибутом, соответствующее поле будет 
    автоматически инициализировано указанным значением.
    
    Параметры:
    - value: Значение с плавающей точкой по умолчанию
    
    Примеры использования:
    public struct MovementComponent : IComponent
    {
        [DefaultFloat(5.0f)]
        public float Speed;
    }
    ",
    Author = "AtomEngine Team")]
    public class DefaultFloatAttribute : DefaultValueAttribute
    {
        public readonly float Value;

        public DefaultFloatAttribute(float value)
        {
            Value = value;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
        [Documentation(
    DocumentationSection = "Core",
    Name = "DefaultStringAttribute",
    SubSection = "Attribute/Inspector/Setter",
    Description = @"
    Устанавливает значение по умолчанию для полей типа string.
    
    namespace AtomEngine
    DefaultStringAttribute(string value)
    
    Этот атрибут используется для задания начального значения строковых полей.
    При создании компонента с таким атрибутом, соответствующее поле будет 
    автоматически инициализировано указанным значением.
    
    Параметры:
    - value: Строковое значение по умолчанию
    
    Примеры использования:
    public struct NameTagComponent : IComponent
    {
        [DefaultString(""Player"")]
        public string Name;
    }
    ",
    Author = "AtomEngine Team")]
    public class DefaultStringAttribute : DefaultValueAttribute
    {
        public readonly string Value;

        public DefaultStringAttribute(string value)
        {
            Value = value;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    [Documentation(
    DocumentationSection = "Core",
    Name = "DefaultVector2Attribute",
    SubSection = "Attribute/Inspector/Setter",
    Description = @"
    Устанавливает значение по умолчанию для полей типа Vector2.
    
    namespace AtomEngine
    DefaultVector2Attribute(float x, float y)
    
    Этот атрибут используется для задания начальных значений для двумерных векторов.
    При создании компонента с таким атрибутом, соответствующее поле будет 
    автоматически инициализировано указанными значениями координат.
    
    Параметры:
    - x: Значение координаты X по умолчанию
    - y: Значение координаты Y по умолчанию
    
    Примеры использования:
    public struct SizeComponent : IComponent
    {
        [DefaultVector2(100.0f, 50.0f)]
        public Vector2 Size;
    }
    ",
    Author = "AtomEngine Team")]
    public class DefaultVector2Attribute : DefaultValueAttribute
    {
        public readonly float XValue;
        public readonly float YValue;

        public DefaultVector2Attribute(float x, float y)
        {
            XValue = x;
            YValue = y;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    [Documentation(
    DocumentationSection = "Core",
    Name = "DefaultVector3Attribute",
    SubSection = "Attribute/Inspector/Setter",
    Description = @"
    Устанавливает значение по умолчанию для полей типа Vector3.
    
    namespace AtomEngine
    DefaultVector3Attribute(float x, float y, float z)
    
    Этот атрибут используется для задания начальных значений для трехмерных векторов.
    При создании компонента с таким атрибутом, соответствующее поле будет 
    автоматически инициализировано указанными значениями координат.
    
    Параметры:
    - x: Значение координаты X по умолчанию
    - y: Значение координаты Y по умолчанию
    - z: Значение координаты Z по умолчанию
    
    Примеры использования:
    public struct PositionComponent : IComponent
    {
        [DefaultVector3(0.0f, 1.0f, 0.0f)]
        public Vector3 Position;
    }
    ",
    Author = "AtomEngine Team")]
    public class DefaultVector3Attribute : DefaultValueAttribute
    {
        public readonly float XValue;
        public readonly float YValue;
        public readonly float ZValue;

        public DefaultVector3Attribute(float x, float y, float z)
        {
            XValue = x;
            YValue = y;
            ZValue = z;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    [Documentation(
    DocumentationSection = "Core",
    Name = "DefaultVector4Attribute",
    SubSection = "Attribute/Inspector/Setter",
    Description = @"
    Устанавливает значение по умолчанию для полей типа Vector4.
    
    namespace AtomEngine
    DefaultVector4Attribute(float x, float y, float z, float a)
    
    Этот атрибут используется для задания начальных значений для четырехмерных векторов.
    При создании компонента с таким атрибутом, соответствующее поле будет 
    автоматически инициализировано указанными значениями координат.
    
    Параметры:
    - x: Значение координаты X по умолчанию
    - y: Значение координаты Y по умолчанию
    - z: Значение координаты Z по умолчанию
    - a: Значение координаты A по умолчанию (часто используется как альфа-компонент)
    
    Примеры использования:
    public struct ColorComponent : IComponent
    {
        [DefaultVector4(1.0f, 1.0f, 1.0f, 1.0f)]
        public Vector4 Color;
    }
    ",
    Author = "AtomEngine Team")]
    public class DefaultVector4Attribute : DefaultValueAttribute
    {
        public readonly float XValue;
        public readonly float YValue;
        public readonly float ZValue;
        public readonly float AValue;

        public DefaultVector4Attribute(float x, float y, float z, float a)
        {
            XValue = x;
            YValue = y;
            ZValue = z;
            AValue = a;
        }
    }
}
