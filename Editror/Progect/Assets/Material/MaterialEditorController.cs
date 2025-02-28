using Newtonsoft.Json;
using System.IO;
using System;
using AtomEngine;
using System.Collections.Generic;
using Silk.NET.Maths;
using OpenglLib;
using System.Numerics;
using System.Linq;

namespace Editor
{
    public class MaterialEditorController
    {
        private Dictionary<string, MaterialAsset> _cacheMaterials = new Dictionary<string, MaterialAsset>();
        private MaterialAsset _currentMaterial;

        private static MaterialEditorController instance;
        public static MaterialEditorController Instance
        {
            get
            {
                if (instance == null) instance = new MaterialEditorController();
                return instance;
            }
        }
        public MaterialEditorController() => instance = this;

        public MaterialAsset CreateMaterial(string shaderRepresentationGuid)
        {
            var material = new MaterialAsset
            {
                ShaderRepresentationGuid = shaderRepresentationGuid,
                Name = $"Material_{Guid.NewGuid().ToString().Substring(0, 8)}"
            };

            string filePath = MetadataManager.Instance.GetPathByGuid(shaderRepresentationGuid);

            if (File.Exists(filePath))
            {
                try
                {
                    string fileContent = File.ReadAllText(filePath);
                    string namespaceName = CSRepresentationParser.ExtractNamespace(fileContent);
                    string className = CSRepresentationParser.ExtractClassName(fileContent);

                    if (!string.IsNullOrEmpty(namespaceName) && !string.IsNullOrEmpty(className))
                    {
                        material.ShaderRepresentationTypeName = $"{namespaceName}.{className}";
                        DebLogger.Info($"Set shader representation type: {material.ShaderRepresentationTypeName}");
                    }
                    else
                    {
                        DebLogger.Warn($"Could not extract namespace or class name from file: {filePath}");
                        DefaultGettingRepTymeName(material, filePath);
                    }

                    InitializeUniformsFromShaderRepresentation(material, filePath);
                }
                catch (Exception ex)
                {
                    DebLogger.Error($"Error parsing shader representation file: {ex.Message}");
                    DefaultGettingRepTymeName(material, filePath);
                }
            }
            else
            {
                DebLogger.Warn($"Shader representation file not found: GUID={shaderRepresentationGuid}");
            }

            _cacheMaterials.Add(filePath, material);
            return material;
        }
        public void SaveMaterial(MaterialAsset material)
        {
            string path = _cacheMaterials.Where(e => e.Value == material).FirstOrDefault().Key;
            if (path != null)
            {
                SaveMaterial(material, path);
            }
            else
            {
                DebLogger.Error($"Saving material {material.Name}. Unkown path to safe");
            }
        }
        public void SaveMaterial(MaterialAsset material, string path)
        {
            string json = MaterialSerializer.SerializeMaterial(material);
            File.WriteAllText(path, json);
            //string json = JsonConvert.SerializeObject(material, Formatting.Indented);
            //File.WriteAllText(path, json);

            //// Создаем метаданные для материала
            //var metadata = new AssetMetadata
            //{
            //    AssetType = MetadataType.Material,
            //    LastModified = DateTime.UtcNow
            //};

            //// Добавляем зависимость от шейдерного представления
            //if (!string.IsNullOrEmpty(material.ShaderRepresentationGuid))
            //{
            //    metadata.Dependencies.Add(material.ShaderRepresentationGuid);
            //}

            //// Добавляем зависимости от текстур
            //foreach (var textureGuid in material.TextureReferences.Values)
            //{
            //    metadata.Dependencies.Add(textureGuid);
            //}


            //MetadataManager.Instance.SaveMetadata(path, metadata);
        }
        public MaterialAsset LoadMaterial(string path)
        {
            if (!File.Exists(path))
            {
                if (_cacheMaterials.TryGetValue(path, out MaterialAsset mat)) _cacheMaterials.Remove(path);

                DebLogger.Error($"File {path} is not exist");
                return null;
            }

            if (_cacheMaterials.TryGetValue(path, out MaterialAsset material))
            {
                return material;
            }

            string json = File.ReadAllText(path);
            MaterialAsset asset = MaterialSerializer.DeserializeMaterial(json);
            //string json = File.ReadAllText(path);
            //MaterialAsset asset = JsonConvert.DeserializeObject<MaterialAsset>(json);
            _cacheMaterials[path] = asset;

            return asset;
        }


        public void InitializeUniformsFromShaderRepresentation(MaterialAsset material, string shaderRepresentationPath)
        {
            if (!File.Exists(shaderRepresentationPath))
            {
                DebLogger.Error($"Shader representation file not found: {shaderRepresentationPath}");
                return;
            }

            try
            {
                string fileContent = File.ReadAllText(shaderRepresentationPath);

                if (material.UniformValues == null)
                    material.UniformValues = new Dictionary<string, object>();

                if (material.TextureReferences == null)
                    material.TextureReferences = new Dictionary<string, string>();

                Dictionary<string, object> properties = new Dictionary<string, object>();
                List<string> samplers = new List<string>();
                CSRepresentationParser.ExtractUniformProperties(fileContent, properties, samplers);
                foreach (var pair in properties)
                {
                    if (!samplers.Contains(pair.Key))
                        material.UniformValues[pair.Key] = pair.Value;
                }
                foreach (var sampler in samplers)
                {
                    material.TextureReferences.Add(sampler, string.Empty);
                }
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Error analyzing shader representation: {ex.Message}");
            }
        }

        public T GetUniformValue<T>(MaterialAsset material, string name)
        {
            if (material.UniformValues.TryGetValue(name, out var value))
            {
                string typeName = typeof(T).Name;
                object convertedValue = ConvertJObjectToTypedValue(value, typeName);
                if (convertedValue is T typedValue)
                {
                    return typedValue;
                }

                try
                {
                    return (T)Convert.ChangeType(convertedValue, typeof(T));
                }
                catch
                {
                    return default;
                }
            }

            return default;
        }
        private object ConvertJObjectToTypedValue(object value, string typeName)
        {
            if (value is Newtonsoft.Json.Linq.JObject jObject)
            {
                switch (typeName)
                {
                    case "vec2":
                    case "Vector2D<float>":
                        float x = jObject["X"]?.ToObject<float>() ?? 0f;
                        float y = jObject["Y"]?.ToObject<float>() ?? 0f;
                        return new System.Numerics.Vector2(x, y);

                    case "vec3":
                    case "Vector3D<float>":
                        float x3 = jObject["X"]?.ToObject<float>() ?? 0f;
                        float y3 = jObject["Y"]?.ToObject<float>() ?? 0f;
                        float z3 = jObject["Z"]?.ToObject<float>() ?? 0f;
                        return new System.Numerics.Vector3(x3, y3, z3);

                    case "vec4":
                    case "Vector4D<float>":
                        float x4 = jObject["X"]?.ToObject<float>() ?? 0f;
                        float y4 = jObject["Y"]?.ToObject<float>() ?? 0f;
                        float z4 = jObject["Z"]?.ToObject<float>() ?? 0f;
                        float w4 = jObject["W"]?.ToObject<float>() ?? 0f;
                        return new System.Numerics.Vector4(x4, y4, z4, w4);

                    // Добавьте другие типы по мере необходимости
                    default:
                        return jObject.ToObject<object>(); // Фолбэк
                }
            }

            return value; // Если это не JObject, вернуть как есть
        }
        private void DefaultGettingRepTymeName(MaterialAsset material, string filePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            if (fileName.IndexOf(".") > -1) fileName = fileName.Substring(0, fileName.IndexOf("."));
            material.ShaderRepresentationTypeName = $"OpenglLib.{fileName}";
        }
    }

    public static class CSRepresentationParser
    {
        public static object GetDefaultValueForType(string typeName)
        {
            switch (typeName)
            {
                case "bool":
                    return false;
                case "int":
                    return 0;
                case "uint":
                    return 0u;
                case "float":
                    return 0.0f;
                case "Vector2D<float>":
                    return Vector2D<float>.Zero;
                case "Vector3D<float>":
                    return Vector3D<float>.Zero;
                case "Vector4D<float>":
                    return Vector4D<float>.Zero;

                default:
                    return null;
            }
        }

        public static void ExtractUniformProperties(string code, in Dictionary<string, object> properties, in List<string> samplers)
        {
            // Регулярное выражение для поиска объявлений свойств для uniform-переменных
            // Ищем объявления вида: public [тип] [имя] { get; set; }
            var propertyRegex = new System.Text.RegularExpressions.Regex(
                @"public\s+(?:unsafe\s+)?(\w+(?:<\w+>)?)\s+(\w+)\s*\{",
                System.Text.RegularExpressions.RegexOptions.Multiline
            );

            // Регулярное выражение для поиска объявлений методов для семплерных типов
            // Ищем объявления вида: public void [имя]_SetTexture(OpenglLib.Texture texture)
            var samplerRegex = new System.Text.RegularExpressions.Regex(
                @"public\s+void\s+(\w+)_SetTexture\s*\(OpenglLib\.Texture\s+\w+\)",
                System.Text.RegularExpressions.RegexOptions.Multiline
            );

            // Находим все совпадения для свойств
            var propertyMatches = propertyRegex.Matches(code);
            foreach (System.Text.RegularExpressions.Match match in propertyMatches)
            {
                if (match.Groups.Count > 2)
                {
                    string typeName = match.Groups[1].Value;
                    string propertyName = match.Groups[2].Value;

                    // Пропускаем свойства для семплерных типов (они будут обрабатываться отдельно)
                    if (typeName.Contains("Array") || propertyName.EndsWith("Location") ||
                        typeName.Contains("Struct") || IsSamplerType(typeName))
                        continue;

                    properties.Add(propertyName, GetDefaultValueForType(typeName));
                }
            }

            // Находим все совпадения для семплеров
            var samplerMatches = samplerRegex.Matches(code);
            foreach (System.Text.RegularExpressions.Match match in samplerMatches)
            {
                if (match.Groups.Count > 1)
                {
                    string samplerName = match.Groups[1].Value;
                    samplers.Add(samplerName);
                }
            }

        }

        public static bool IsSamplerType(string typeName)
        {
            return typeName.Equals("int") && (typeName.Contains("sampler") || typeName.Contains("Sampler"));
        }

        public static string ExtractNamespace(string code)
        {
            var namespaceMatch = System.Text.RegularExpressions.Regex.Match(
                code,
                @"namespace\s+([^\s{]+)"
            );

            if (namespaceMatch.Success && namespaceMatch.Groups.Count > 1)
            {
                return namespaceMatch.Groups[1].Value;
            }

            return string.Empty;
        }
        public static string ExtractClassName(string code)
        {
            var classMatch = System.Text.RegularExpressions.Regex.Match(
                code,
                @"public(?:\s+partial)?\s+class\s+([^\s:]+)"
            );

            if (classMatch.Success && classMatch.Groups.Count > 1)
            {
                return classMatch.Groups[1].Value;
            }

            return string.Empty;
        }

    }

    public static class MaterialSerializer
    {
        private static readonly HashSet<Type> SpecialTypes = new HashSet<Type>
        {
            typeof(Vector2),
            typeof(Vector3),
            typeof(Vector4),
            typeof(Vector2D<float>),
            typeof(Vector3D<float>),
            typeof(Vector4D<float>),
            typeof(Matrix2X2<float>),
            typeof(Matrix3X3<float>),
            typeof(Matrix4X4<float>)
        };

        public static string SerializeMaterial(MaterialAsset material)
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.Auto,
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            var serializableData = new
            {
                material.ShaderRepresentationGuid,
                material.ShaderRepresentationTypeName,
                material.Name,
                UniformValues = ConvertUniformValuesToSerializable(material.UniformValues),
                material.TextureReferences
            };

            return JsonConvert.SerializeObject(serializableData, settings);
        }

        public static MaterialAsset DeserializeMaterial(string json)
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            };

            var deserializedData = JsonConvert.DeserializeObject<MaterialAsset>(json, settings);

            if (deserializedData.UniformValues != null)
            {
                deserializedData.UniformValues = ConvertUniformValuesToTyped(deserializedData.UniformValues);
            }

            return deserializedData;
        }

        private static Dictionary<string, object> ConvertUniformValuesToSerializable(Dictionary<string, object> values)
        {
            var result = new Dictionary<string, object>();

            if (values == null)
                return result;

            foreach (var pair in values)
            {
                result[pair.Key] = ConvertToSerializable(pair.Value);
            }

            return result;
        }

        private static Dictionary<string, object> ConvertUniformValuesToTyped(Dictionary<string, object> values)
        {
            var result = new Dictionary<string, object>();

            if (values == null)
                return result;

            foreach (var pair in values)
            {
                result[pair.Key] = ConvertToTyped(pair.Value);
            }

            return result;
        }

        private static object ConvertToSerializable(object value)
        {
            if (value == null)
                return null;

            Type type = value.GetType();

            if (!SpecialTypes.Contains(type))
                return value;

            if (type == typeof(Vector3) || type == typeof(Vector3D<float>))
            {
                if (value is Vector3 v3)
                    return v3;
                else
                    return ((Vector3D<float>)value).ToNumetrix();
            }
            else if (type == typeof(Vector2) || type == typeof(Vector2D<float>))
            {
                if (value is Vector2 v2)
                    return v2;
                else
                    return ((Vector2D<float>)value).ToNumetrix();
            }
            else if (type == typeof(Vector4) || type == typeof(Vector4D<float>))
            {
                if (value is Vector4 v4)
                    return v4;
                else
                    return ((Vector4D<float>)value).ToNumetrix();
            }
            else if (type == typeof(Matrix4X4<float>))
            {
                return ((Matrix4X4<float>)value).ToNumetrix();
            }

            return value;
        }

        private static object ConvertToTyped(object value)
        {
            if (value == null)
                return null;

            if (value is Newtonsoft.Json.Linq.JObject jObj)
            {
                if (jObj["X"] != null && jObj["Y"] != null)
                {
                    if (jObj["Z"] != null)
                    {
                        if (jObj["W"] != null)
                        {
                            float x4 = jObj["X"].ToObject<float>();
                            float y4 = jObj["Y"].ToObject<float>();
                            float z4 = jObj["Z"].ToObject<float>();
                            float w4 = jObj["W"].ToObject<float>();
                            return new Vector4(x4, y4, z4, w4);
                        }
                        else
                        {
                            float x3 = jObj["X"].ToObject<float>();
                            float y3 = jObj["Y"].ToObject<float>();
                            float z3 = jObj["Z"].ToObject<float>();
                            return new Vector3(x3, y3, z3);
                        }
                    }
                    else
                    {
                        float x2 = jObj["X"].ToObject<float>();
                        float y2 = jObj["Y"].ToObject<float>();
                        return new Vector2(x2, y2);
                    }
                }
                else if (jObj["Values"] != null)
                {
                    var values = jObj["Values"].ToObject<float[]>();
                    if (values.Length == 16)
                    {
                        return new Matrix4X4<float>(
                            values[0], values[1], values[2], values[3],
                            values[4], values[5], values[6], values[7],
                            values[8], values[9], values[10], values[11],
                            values[12], values[13], values[14], values[15]
                        );
                    }
                }

                return jObj;
            }

            return value;
        }

    }
}
