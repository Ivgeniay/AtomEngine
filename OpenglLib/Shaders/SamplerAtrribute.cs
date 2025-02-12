namespace OpenglLib
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class SamplerAtrribute : Attribute
    {
        public string TextureUnit;
        public string Name;
        public string Type;
        public SamplerAtrribute(string name, string textureUnit, string type)
        {
            Name = name;
            TextureUnit = textureUnit;
            Type = type;
        }
    }
}
