using AtomEngine;

namespace Editor
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;

    internal class ComponentConverter : JsonConverter<IComponent>
    {
        public override void WriteJson(JsonWriter writer, IComponent? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            JObject obj = JObject.FromObject(value, serializer);
            obj.Add("Type", value.GetType().AssemblyQualifiedName!);
            obj.WriteTo(writer);
        }

        public override IComponent? ReadJson(JsonReader reader, Type objectType, IComponent? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            JObject obj = JObject.Load(reader);
            string? typeName = obj["Type"]?.ToString();
            if (string.IsNullOrEmpty(typeName))
                throw new JsonSerializationException("Missing Type information");

            Type? type = Type.GetType(typeName);
            if (type == null)
                throw new JsonSerializationException($"Unknown type: {typeName}");

            return (IComponent?)obj.ToObject(type, serializer);
        }
    }
}
