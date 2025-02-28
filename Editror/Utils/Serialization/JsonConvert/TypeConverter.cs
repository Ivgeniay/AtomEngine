using Newtonsoft.Json;
using System;
using System.Linq;
using System.Reflection;

namespace Editor
{
    public class TypeConverter : JsonConverter<Type>
    {
        public override Type ReadJson(JsonReader reader, Type objectType, Type existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            string typeName = (string)reader.Value;
            if (string.IsNullOrEmpty(typeName))
                return null;

            Type type = Type.GetType(typeName);

            if (type == null)
            {
                type = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.FullName == typeName);
            }

            if (type == null)
            {
                string assemblyName = typeName.Split(',')[1].Trim();
                try
                {
                    Assembly assembly = Assembly.Load(assemblyName);
                    type = assembly.GetType(typeName.Split(',')[0].Trim());
                }
                catch (Exception)
                {
                }
            }

            return type;
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
