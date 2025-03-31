namespace OpenglLib
{
    public class ComponentGeneratorFieldInfo
    {
        public string FieldName { get; set; } = string.Empty;
        public string FieldType { get; set; } = string.Empty;
        public bool IsArray { get; set; } = false;
        public int ArraySize { get; set; } = 0;
        public bool IsUniform { get; set; } = false;
        public bool IsUniformBlock { get; set; } = false;
        public bool IsCustomStruct { get; set; } = false;
        public bool IsDirtySupport { get; set; } = false;
        public List<string> Attributes { get; set; } = new List<string>();
    }
}
