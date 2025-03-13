using System.Collections.Generic;
using Newtonsoft.Json;
using AtomEngine;
using System;

namespace Editor
{
    internal class ComponentDictionaryConverter : JsonConverter<Dictionary<string, IComponent>>
    {
        public override void WriteJson(JsonWriter writer, Dictionary<string, IComponent>? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();
            foreach (var kvp in value)
            {
                writer.WritePropertyName(kvp.Key);
                serializer.Serialize(writer, kvp.Value);
            }
            writer.WriteEndObject();
        }
        public override Dictionary<string, IComponent>? ReadJson(JsonReader reader, Type objectType, Dictionary<string, IComponent>? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var result = new Dictionary<string, IComponent>();
            reader.Read();

            var assemblyManager = ServiceHub.Get<EditorAssemblyManager>();
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
                    var componentType = assemblyManager.FindType(propertyName);
                    if (componentType != null && typeof(IComponent).IsAssignableFrom(componentType))
                    {
                        var component = (IComponent?)serializer.Deserialize(reader, componentType);
                        if (component != null)
                        {
                            result[propertyName] = component;
                        }
                    }
                    else
                    {
                        DebLogger.Warn($"Not finded component type: {propertyName}");
                        serializer.Deserialize(reader);
                    }
                }

                reader.Read();
            }

            return result;
        }
    }
}
