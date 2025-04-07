using EngineLib;
using Silk.NET.Maths;

namespace OpenglLib
{
    public class MaterialAsset : Asset
    {
        public string ShaderRepresentationGuid { get; set; } = string.Empty;
        public string ShaderRepresentationTypeName { get; set; } = string.Empty;
        public bool HasValidShader => !string.IsNullOrEmpty(ShaderRepresentationGuid);

        public Dictionary<string, string> TextureReferences { get; set; } = new Dictionary<string, string>();


        private List<MaterialDataContainer> _values = new List<MaterialDataContainer>();

        public MaterialDataContainer GetContainerByName(string name)
        {
            return _values.FirstOrDefault(v => v.Name == name);
        }

        public MaterialDataContainer GetContainerByPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            string[] parts = path.Split('.');
            string rootName = parts[0];

            var arrayMatch = System.Text.RegularExpressions.Regex.Match(rootName, @"^([\w\d_]+)\[(\d+)\]$");
            if (arrayMatch.Success)
            {
                string arrayName = arrayMatch.Groups[1].Value;
                int index = int.Parse(arrayMatch.Groups[2].Value);

                var container = GetContainerByName(arrayName);
                if (container == null)
                    return null;

                if (container is MaterialStructArrayDataContainer structArrayContainer &&
                    index >= 0 && index < structArrayContainer.Elements.Count)
                {
                    var element = structArrayContainer.Elements[index];
                    if (parts.Length == 1)
                        return element;

                    return GetNestedFieldFromStruct(element, parts.Skip(1).ToArray());
                }
                else if (container is MaterialArrayDataContainer arrayContainer &&
                         index >= 0 && index < arrayContainer.Values.Count)
                {
                    return null;
                }
                else if (container is MaterialSamplerArrayDataContainer samplerArrayContainer &&
                         index >= 0 && index < samplerArrayContainer.TextureGuids.Count)
                {
                    return null;
                }

                return null;
            }

            var container_ = GetContainerByName(rootName);
            if (container_ == null || parts.Length == 1)
                return container_;

            if (container_ is MaterialStructDataContainer structContainer)
                return GetNestedFieldFromStruct(structContainer, parts.Skip(1).ToArray());

            return null;
        }

        private MaterialDataContainer GetNestedFieldFromStruct(MaterialStructDataContainer structContainer, string[] pathParts)
        {
            if (pathParts.Length == 0)
                return structContainer;

            var field = structContainer.Fields.FirstOrDefault(f => f.Name == pathParts[0]);
            if (field == null)
                return null;

            if (pathParts.Length == 1)
                return field;

            if (field is MaterialStructDataContainer nestedStruct)
                return GetNestedFieldFromStruct(nestedStruct, pathParts.Skip(1).ToArray());

            return null;
        }

        public object GetValue(string path)
        {
            var container = GetContainerByPath(path);
            if (container == null)
                return null;

            if (container is MaterialUniformDataContainer uniformContainer)
                return uniformContainer.Value;
            else if (container is MaterialSamplerDataContainer samplerContainer)
                return samplerContainer.TextureGuid;
            else if (container is MaterialSamplerArrayDataContainer samplerArrayContainer)
                return samplerArrayContainer.TextureGuids;
            else if (container is MaterialArrayDataContainer arrayContainer)
                return arrayContainer.Values;

            return null;
        }

        public void SetValue(string path, object value)
        {
            var container = GetContainerByPath(path);
            if (container == null)
                return;

            if (container is MaterialUniformDataContainer uniformContainer)
            {
                uniformContainer.Value = value;
            }
            else if (container is MaterialSamplerDataContainer samplerContainer && value is string textureGuid)
            {
                samplerContainer.TextureGuid = textureGuid;
            }
            else if (container is MaterialSamplerArrayDataContainer samplerArrayContainer && value is IEnumerable<string> textureGuids)
            {
                samplerArrayContainer.TextureGuids = new List<string>(textureGuids);
            }
            else if (container is MaterialArrayDataContainer arrayContainer && value is IEnumerable<object> values)
            {
                arrayContainer.Values = new List<object>(values);
            }
        }

        public string GetTextureGuid(string samplerName)
        {
            var container = GetContainerByName(samplerName);
            if (container is MaterialSamplerDataContainer samplerContainer)
                return samplerContainer.TextureGuid;

            return string.Empty;
        }

        public void SetTextureGuid(string samplerName, string textureGuid)
        {
            var container = GetContainerByName(samplerName);
            if (container is MaterialSamplerDataContainer samplerContainer)
                samplerContainer.TextureGuid = textureGuid;
        }

        public void AddContainer(MaterialDataContainer container)
        {
            if (container == null || string.IsNullOrEmpty(container.Name))
                return;

            var existing = _values.FirstOrDefault(v => v.Name == container.Name);
            if (existing != null)
                _values.Remove(existing);

            _values.Add(container);
        }

        public List<MaterialDataContainer> GetAllContainers()
        {
            return new List<MaterialDataContainer>(_values);
        }

        public void ClearContainers()
        {
            _values.Clear();
        }
    }

    public abstract class MaterialDataContainer
    {
        public string Name { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
    }
    public class MaterialUniformDataContainer : MaterialDataContainer
    {
        public Type Type { get; set; }
        public object Value { get; set; }
    }
    public class MaterialSamplerDataContainer : MaterialDataContainer
    {
        public string TextureGuid { get; set; } = string.Empty;
    }
    public class MaterialSamplerArrayDataContainer : MaterialDataContainer
    {
        public int Size { get; set; }
        public List<string> TextureGuids { get; set; } = new List<string>();
    }
    public class MaterialStructDataContainer : MaterialDataContainer
    {
        public Type StructType { get; set; }
        public List<MaterialDataContainer> Fields { get; set; } = new List<MaterialDataContainer>();
    }
    public class MaterialStructArrayDataContainer : MaterialDataContainer
    {
        public Type ElementType { get; set; }
        public int Size { get; set; }
        public List<MaterialStructDataContainer> Elements { get; set; } = new List<MaterialStructDataContainer>();
    }
    public class MaterialArrayDataContainer : MaterialDataContainer
    {
        public Type ElementType { get; set; }
        public int Size { get; set; }
        public List<object> Values { get; set; } = new List<object>();
    }

    
}
