using System.Collections.Generic;
using Newtonsoft.Json.Linq;
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
                var readerState = CaptureReaderState(reader);
                reader.Read();

                if (propertyName != null)
                {
                    var componentType = assemblyManager.FindType(propertyName, true);
                    //if (propertyName == "UserScripts.TestShaderComponent")
                    //{
                    //    var fields = componentType.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    //}
                    if (componentType != null && typeof(IComponent).IsAssignableFrom(componentType))
                    {
                        try
                        {
                            var component = (IComponent?)serializer.Deserialize(reader, componentType);
                            if (component != null)
                            {
                                result[propertyName] = component;
                            }
                        }
                        catch (Exception ex)
                        {
                            DebLogger.Warn($"Ошибка при десериализации компонента {propertyName}. Будет создан новый экземпляр. Ошибка: {ex.Message}");

                            ResetAndSkipObject(reader, readerState);

                            try
                            {
                                var newComponent = (IComponent)Activator.CreateInstance(componentType);
                                result[propertyName] = newComponent;

                                DebLogger.Info($"Создан новый экземпляр компонента {propertyName} с дефолтными значениями");
                            }
                            catch (Exception createEx)
                            {
                                DebLogger.Error($"Не удалось создать экземпляр компонента {propertyName}: {createEx.Message}");
                            }
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

        private class ReaderState
        {
            public int Depth { get; set; }
            public JsonToken TokenType { get; set; }
            public string Path { get; set; }
            public object Value { get; set; }
        }

        private ReaderState CaptureReaderState(JsonReader reader)
        {
            return new ReaderState
            {
                Depth = reader.Depth,
                TokenType = reader.TokenType,
                Path = reader.Path,
                Value = reader.Value
            };
        }

        private void ResetAndSkipObject(JsonReader reader, ReaderState state)
        {
            var jsonObject = JObject.Parse($"{{\"{state.Value}\": {reader.ReadAsString()}}}");
            foreach (var prop in jsonObject.Properties())
            {
            }
        }

        private void SkipObject(JsonReader reader)
        {
            int depth = reader.Depth;
            while (reader.Read() && reader.Depth > depth)
            {
            }
        }
    }
}
