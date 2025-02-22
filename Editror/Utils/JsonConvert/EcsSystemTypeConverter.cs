using Newtonsoft.Json;
using AtomEngine;
using System;

namespace Editor
{
    [JsonConverter(typeof(EcsSystemTypeConverter))]
    internal class EcsSystemTypeConverter : JsonConverter<Type>
    {
        public override Type ReadJson(JsonReader reader, Type objectType, Type existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            string typeName = (string)reader.Value;
            if (string.IsNullOrEmpty(typeName))
                return null;

            Type systemType = Type.GetType(typeName);
            if (systemType == null || !typeof(ICommonSystem).IsAssignableFrom(systemType))
            {
                throw new JsonSerializationException($"Cannot deserialize system type: {typeName}");
            }

            return systemType;
        }

        public override void WriteJson(JsonWriter writer, Type value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteValue(value.AssemblyQualifiedName);
        }
    }
}
