using System.Collections.Generic;

namespace Editor
{
    public class ComponentGeneratorInfo
    {
        public string ComponentName { get; set; } = string.Empty;
        public List<ComponentGeneratorFieldInfo> Fields { get; set; } = new List<ComponentGeneratorFieldInfo>();
        public void AddField(ComponentGeneratorFieldInfo fieldInfo) =>
            Fields.Add(fieldInfo);
    }
}
