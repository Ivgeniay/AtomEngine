using System;

namespace Editor
{
    public class PropertyDescriptor
    {
        public string Name { get; set; }
        public Type Type { get; set; }
        public object Value { get; set; }
        public object Context { get; set; }
        public bool IsReadOnly { get; set; }
        public Action<object> OnValueChanged { get; set; }
    }
}
