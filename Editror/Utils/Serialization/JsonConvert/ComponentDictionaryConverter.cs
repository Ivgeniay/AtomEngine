using System.Collections.Generic;
using Newtonsoft.Json;
using AtomEngine;
using System;
using System.Reflection;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Security.Cryptography;

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
                //SerializeComponentWithFilter(writer, kvp.Value, serializer);
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

        private void SerializeComponentWithFilter(JsonWriter writer, IComponent component, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            PropertyInfo[] properties = component.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo property in properties)
            {
                try
                {
                    if (property.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                        continue;

                    Type propertyType = property.PropertyType;

                    if (GLDependableTypes.IsDependableType(propertyType))
                        continue;

                    string propertyName = property.Name;
                    JsonPropertyAttribute? jsonPropertyAttr = property.GetCustomAttribute<JsonPropertyAttribute>();
                    if (jsonPropertyAttr != null && !string.IsNullOrEmpty(jsonPropertyAttr.PropertyName))
                        propertyName = jsonPropertyAttr.PropertyName;

                    object? value = null;
                    try
                    {
                        value = property.GetValue(component);
                    }
                    catch
                    {
                        continue;
                    }

                    // Записываем свойство
                    writer.WritePropertyName(propertyName);
                    serializer.Serialize(writer, value);
                }
                catch
                {
                    continue;
                }
            }

            List<FieldInfo> fields = component.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance).ToList();
            fields.AddRange(component.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Where(e => !e.Name.StartsWith("k__")));

            foreach (FieldInfo field in fields)
            {
                try
                {
                    if (field.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                        continue;

                    Type fieldType = field.FieldType;

                    if (GLDependableTypes.IsDependableType(fieldType))
                        continue;

                    string fieldName = field.Name;
                    JsonPropertyAttribute? jsonPropertyAttr = field.GetCustomAttribute<JsonPropertyAttribute>();
                    if (jsonPropertyAttr != null && !string.IsNullOrEmpty(jsonPropertyAttr.PropertyName))
                        fieldName = jsonPropertyAttr.PropertyName;

                    object? value = null;
                    try
                    {
                        value = field.GetValue(component);
                    }
                    catch
                    {
                        continue;
                    }

                    writer.WritePropertyName(fieldName);
                    serializer.Serialize(writer, value);
                }
                catch
                {
                    continue;
                }
            }
            writer.WriteEndObject();
        }

        private void SerializeComponentWithFilter2(JsonWriter writer, IComponent component, JsonSerializer serializer)
        {
            JObject jobject = JObject.FromObject(component, serializer);
            List<string> propsToRemove = new List<string>();

            foreach (JProperty prop in jobject.Properties())
            {
                PropertyInfo? propInfo = component.GetType().GetProperty(prop.Name);
                if (propInfo != null && GLDependableTypes.IsDependableType(propInfo.PropertyType))
                {
                    propsToRemove.Add(prop.Name);
                    continue;
                }

                FieldInfo? fieldInfo = component.GetType().GetField(prop.Name);
                if (fieldInfo != null && GLDependableTypes.IsDependableType(fieldInfo.FieldType))
                {
                    propsToRemove.Add(prop.Name);
                }
            }

            foreach (string propName in propsToRemove)
            {
                jobject.Remove(propName);
            }

            jobject.WriteTo(writer);
        }
    }
}
