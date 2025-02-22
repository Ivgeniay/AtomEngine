using System;

namespace Editor
{
    public class PropertyDescriptor
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public object Value { get; set; }
        public bool IsReadOnly { get; set; }
        public Action<object> OnValueChanged { get; set; }
    }
}
