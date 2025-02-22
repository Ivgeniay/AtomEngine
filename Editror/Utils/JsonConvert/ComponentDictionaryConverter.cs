using AtomEngine;

namespace Editor
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;

    internal class ComponentDictionaryConverter : JsonConverter<Dictionary<string, IComponent>>
    {
        private readonly Dictionary<string, Type> _componentTypes;

        public ComponentDictionaryConverter()
        {
            _componentTypes = new Dictionary<string, Type>
            {
                { nameof(TransformComponent), typeof(TransformComponent) },
            };
        }

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

            while (reader.TokenType != JsonToken.EndObject)
            {
                if (reader.TokenType != JsonToken.PropertyName)
                {
                    reader.Read();
                    continue;
                }

                var propertyName = reader.Value?.ToString();
                reader.Read();

                if (propertyName != null && _componentTypes.TryGetValue(propertyName, out Type? componentType))
                {
                    var component = (IComponent?)serializer.Deserialize(reader, componentType);
                    if (component != null)
                    {
                        result[propertyName] = component;
                    }
                }
                else
                {
                    serializer.Deserialize(reader);
                }

                reader.Read();
            }

            return result;
        }
    }
}
