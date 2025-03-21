using AtomEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace EngineLib
{
    public class ComponentDictionaryConverter : JsonConverter<Dictionary<string, IComponent>>
    {
        public override void WriteJson(JsonWriter writer, Dictionary<string, IComponent>? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var tempSerializer = new JsonSerializer
            {
                ContractResolver = new IgnorePropertiesForComponentsResolver(),
                TypeNameHandling = serializer.TypeNameHandling,
                PreserveReferencesHandling = PreserveReferencesHandling.None,
                ReferenceLoopHandling = serializer.ReferenceLoopHandling
            };

            writer.WriteStartObject();
            foreach (var kvp in value)
            {
                writer.WritePropertyName(kvp.Key);
                tempSerializer.Serialize(writer, kvp.Value);
            }
            writer.WriteEndObject();
        }


        public override Dictionary<string, IComponent>? ReadJson(JsonReader reader, Type objectType, Dictionary<string, IComponent>? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var result = new Dictionary<string, IComponent>();
            reader.Read();

            var assemblyManager = ServiceHub.Get<AssemblyManager>();
            while (reader.TokenType != JsonToken.EndObject)
            {
                if (reader.TokenType != JsonToken.PropertyName)
                {
                    reader.Read();
                    continue;
                }

                var propertyName = reader.Value?.ToString();
                reader.Read();

                if (propertyName != null)
                {
                    var componentType = assemblyManager.FindType(propertyName, true);
                    if (componentType != null && typeof(IComponent).IsAssignableFrom(componentType))
                    {
                        try
                        {
                            JObject componentJson = JObject.Load(reader);
                            var componentSerializer = new JsonSerializer
                            {
                                ContractResolver = new IgnorePropertiesForComponentsResolver(),
                                TypeNameHandling = serializer.TypeNameHandling,
                                PreserveReferencesHandling = PreserveReferencesHandling.None,
                                ObjectCreationHandling = ObjectCreationHandling.Replace
                            };

                            using (var jTokenReader = new JTokenReader(componentJson))
                            {
                                var component = (IComponent?)componentSerializer.Deserialize(jTokenReader, componentType);
                                if (component != null)
                                {
                                    result[propertyName] = component;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            DebLogger.Error($"Ошибка при десериализации компонента {propertyName}: {ex.Message}");
                            SkipCurrentObject(reader);
                        }
                    }
                    else
                    {
                        DebLogger.Warn($"Not found component type: {propertyName}");
                        SkipCurrentObject(reader);
                    }
                }
                else
                {
                    SkipCurrentObject(reader);
                }

                reader.Read();
            }

            return result;
        }

        private void SkipCurrentObject(JsonReader reader)
        {
            if (reader.TokenType == JsonToken.StartObject)
            {
                int depth = reader.Depth;
                while (reader.Read() && !(reader.TokenType == JsonToken.EndObject && reader.Depth == depth)) { }
            }
            else if (reader.TokenType == JsonToken.StartArray)
            {
                int depth = reader.Depth;
                while (reader.Read() && !(reader.TokenType == JsonToken.EndArray && reader.Depth == depth)) { }
            }
            else
            {
                reader.Skip();
            }
        }


        private class IgnorePropertiesForComponentsResolver : DefaultContractResolver
        {
            ExcludeSerializationTypeService excludeTypeService;

            public IgnorePropertiesForComponentsResolver()
            {
                excludeTypeService = ServiceHub.Get<ExcludeSerializationTypeService>();
            }

            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                JsonProperty property = base.CreateProperty(member, memberSerialization);

                Type propertyType = null;
                if (member is PropertyInfo propertyInfo)
                {
                    propertyType = propertyInfo.PropertyType;
                }
                else if (member is FieldInfo fieldInfo)
                {
                    propertyType = fieldInfo.FieldType;
                }

                if (propertyType != null && excludeTypeService.IsExcludedType(propertyType))
                {
                    property.ShouldSerialize = _ => false;
                }

                return property;
            }
        }
    }
}
