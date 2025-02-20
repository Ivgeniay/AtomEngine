using AtomEngine;

namespace Editor
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;

    internal class ComponentDictionaryConverter : JsonConverter<Dictionary<string, IComponent>>
    {
        public override void WriteJson(JsonWriter writer, Dictionary<string, IComponent>? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            JObject obj = new JObject();
            foreach (var kvp in value)
            {
                obj[kvp.Key] = JToken.FromObject(kvp.Value, serializer);
            }
            obj.WriteTo(writer);
        }

        public override Dictionary<string, IComponent>? ReadJson(JsonReader reader, Type objectType, Dictionary<string, IComponent>? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            JObject obj = JObject.Load(reader);
            var components = new Dictionary<string, IComponent>();

            foreach (var kvp in obj)
            {
                var component = kvp.Value.ToObject<IComponent>(serializer);
                if (component != null)
                {
                    components[kvp.Key] = component;
                }
            }

            return components;
        }
    }
}
