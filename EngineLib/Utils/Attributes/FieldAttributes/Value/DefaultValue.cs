using System.ComponentModel.DataAnnotations;

namespace EngineLib
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    [Documentation(DocumentationSection = "Attribute", Name = "DefaultIntAttribute", Description = @"
    Setter default value for int type.
", SubSection = "Inspector/Setter")]
    public class DefaultIntAttribute : Attribute
    {
        public readonly int Value;

        public DefaultIntAttribute(int value)
        {
            Value = value;
        }

    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    [Documentation(DocumentationSection = "Attribute", Name = "DefaultFloatAttribute", Description = @"
    Setter default value for float type.
", SubSection = "Inspector/Setter")]
    public class DefaultFloatAttribute : Attribute
    {
        public readonly float Value;

        public DefaultFloatAttribute(float value)
        {
            Value = value;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    [Documentation(DocumentationSection = "Attribute", Name = "DefaultStringAttribute", Description = @"
    Setter default value for string type.
", SubSection = "Inspector/Setter")]
    public class DefaultStringAttribute : Attribute
    {
        public readonly string Value;

        public DefaultStringAttribute(string value)
        {
            Value = value;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    [Documentation(DocumentationSection = "Attribute", Name = "DefaultVector2Attribute", Description = @"
    Setter default value for System.Numetrics.Vector2 type.
", SubSection = "Inspector/Setter")]
    public class DefaultVector2Attribute : Attribute
    {
        public readonly float XValue;
        public readonly float YValue;

        public DefaultVector2Attribute(float x, float y)
        {
            XValue = x;
            YValue = x;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    [Documentation(DocumentationSection = "Attribute", Name = "DefaultVector3Attribute", Description = @"
    Setter default value for System.Numetrics.Vector3 type.
", SubSection = "Inspector/Setter")]
    public class DefaultVector3Attribute : Attribute
    {
        public readonly float XValue;
        public readonly float YValue;
        public readonly float ZValue;

        public DefaultVector3Attribute(float x, float y, float z)
        {
            XValue = x;
            YValue = x;
            ZValue = z;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    [Documentation(DocumentationSection = "Attribute", Name = "DefaultVector4Attribute", Description = @"
    Setter default value for System.Numetrics.Vector4 type.
", SubSection = "Inspector/Setter")]
    public class DefaultVector4Attribute : Attribute
    {
        public readonly float XValue;
        public readonly float YValue;
        public readonly float ZValue;
        public readonly float AValue;

        public DefaultVector4Attribute(float x, float y, float z, float a)
        {
            XValue = x;
            YValue = x;
            ZValue = z;
            AValue = a;
        }
    }
}
