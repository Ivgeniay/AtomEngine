

using Newtonsoft.Json;

namespace OpenglLib
{
    internal class StructDefinition
    {
        public string Name { get; set; }
        public List<StructFieldDefinition> Fields { get; set; }

        public StructDefinition(string name)
        {
            Name = name;
            Fields = new List<StructFieldDefinition>();
        }

        public override string ToString() => JsonConvert.SerializeObject(this, Formatting.Indented);
    }
}
