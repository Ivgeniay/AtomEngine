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
    Sets the default value for string fields.

    namespace AtomEngine
    DefaultStringAttribute(string value)

    This attribute is used to set the initial value of string fields.
    When creating a component with this attribute, the corresponding field will be
    automatically initialized with the specified value.

    Parameters:
    - value: Default string value

    Usage examples:
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
    Sets the default value for Vector2 fields.

    namespace AtomEngine
    DefaultVector2Attribute(float x, float y)

    This attribute is used to set initial values ​​for two-dimensional vectors.
    When creating a component with this attribute, the corresponding field will be
    automatically initialized with the specified coordinate values.

    Parameters:
    - x: Default X coordinate value
    - y: Default Y coordinate value

    Usage examples:
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
    Sets the default value for Vector3 fields.

    namespace AtomEngine
    DefaultVector3Attribute(float x, float y, float z)

    This attribute is used to set initial values ​​for three-dimensional vectors.
    When creating a component with this attribute, the corresponding field will be
    automatically initialized with the specified coordinate values.

    Parameters:
    - x: Default X coordinate value
    - y: Default Y coordinate value
    - z: Default Z coordinate value

    Usage examples:
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
    Sets the default value for Vector4 fields.

    namespace AtomEngine
    DefaultVector4Attribute(float x, float y, float z, float a)

    This attribute is used to set initial values ​​for four-dimensional vectors.
    When creating a component with this attribute, the corresponding field will be
    automatically initialized with the specified coordinate values.

    Parameters:
    - x: Default X coordinate value
    - y: Default Y coordinate value
    - z: Default Z coordinate value
    - a: Default A coordinate value (often used as an alpha component)

    Usage examples:
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
