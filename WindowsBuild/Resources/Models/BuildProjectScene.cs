using AtomEngine;
using Newtonsoft.Json;

namespace WindowsBuild
{
    internal class BuildProjectScene
    {
        public List<BuildWorldData> Worlds = new List<BuildWorldData>();
        public List<SystemData> Systems = new List<SystemData>();
        public string ScenePath { get; set; } = string.Empty;
    }

    internal class BuildWorldData
    {
        public uint WorldId { get; set; } = 0;
        public string WorldName { get; set; } = "World_0";
        public List<BuildEntityData> Entities { get; set; } = new List<BuildEntityData>();
    }

    internal class BuildEntityData
    {
        public uint Id { get; set; }
        public uint Version { get; set; }
        public string Name { get; set; } = string.Empty;
        [JsonConverter(typeof(ComponentDictionaryConverter))]
        public Dictionary<string, IComponent> Components { get; set; } = new();

        public string Guid { get; set; } = System.Guid.NewGuid().ToString();
        public bool IsPrefabInstance { get; set; } = false;
        public string PrefabSourceGuid { get; set; } = string.Empty;
    }

    public class ComponentDictionaryConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Dictionary<string, IComponent>);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var result = new Dictionary<string, IComponent>();

            if (reader.TokenType != JsonToken.StartObject)
                return result;

            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
                string componentName = reader.Value.ToString();
                Type componentType = null;

                componentType = AssemblyManager.Instance.FindType(componentName, true);

                if (componentType == null)
                    componentType = Type.GetType($"UserScripts.{componentName}");

                if (componentType == null)
                {
                    reader.Skip();
                    continue;
                }

                reader.Read();
                var component = (IComponent)serializer.Deserialize(reader, componentType);

                result[componentName] = component;
            }

            return result;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var dictionary = (Dictionary<string, IComponent>)value;

            writer.WriteStartObject();

            foreach (var kvp in dictionary)
            {
                writer.WritePropertyName(kvp.Key);
                serializer.Serialize(writer, kvp.Value);
            }

            writer.WriteEndObject();
        }
    }
}
