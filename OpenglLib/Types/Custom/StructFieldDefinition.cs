

using Newtonsoft.Json;

namespace OpenglLib
{
    internal class StructFieldDefinition
    {
        public string FieldName;
        public string FieldType;
        public int ArraySize;

        public StructFieldDefinition(string fieldName, string fieldType, int arraySize)
        {
            FieldName = fieldName;
            FieldType = fieldType;
            ArraySize = arraySize;
        }

        public override string ToString() => JsonConvert.SerializeObject(this, Formatting.Indented);
    }
}
