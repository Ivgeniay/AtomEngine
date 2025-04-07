using AtomEngine;
using EngineLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Silk.NET.Maths;
using System.Numerics;

namespace OpenglLib
{
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
                TypeNameHandling = TypeNameHandling.None,
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            var serializableData = new
            {
                material.Guid,
                material.ShaderRepresentationGuid,
                material.ShaderRepresentationTypeName,
                Values = SerializeContainers(material.GetAllContainers()),
                material.TextureReferences
            };

            return JsonConvert.SerializeObject(serializableData, settings);
        }

        public static MaterialAsset DeserializeMaterial(string json)
        {
            var material = new MaterialAsset();

            try
            {
                var jsonObj = JsonConvert.DeserializeObject<JObject>(json);

                material.Guid = jsonObj["Guid"]?.ToString();
                material.ShaderRepresentationGuid = jsonObj["ShaderRepresentationGuid"]?.ToString();
                material.ShaderRepresentationTypeName = jsonObj["ShaderRepresentationTypeName"]?.ToString();

                if (jsonObj["TextureReferences"] != null)
                {
                    material.TextureReferences = jsonObj["TextureReferences"].ToObject<Dictionary<string, string>>();
                }

                if (jsonObj["Values"] != null && jsonObj["Values"] is JArray valuesArray)
                {
                    var containers = DeserializeContainers(valuesArray);
                    foreach (var container in containers)
                    {
                        material.AddContainer(container);
                    }
                }
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка десериализации материала: {ex.Message}");
            }

            return material;
        }

        private static object[] SerializeContainers(List<MaterialDataContainer> containers)
        {
            var result = new List<object>();

            foreach (var container in containers)
            {
                result.Add(SerializeContainer(container));
            }

            return result.ToArray();
        }

        private static object SerializeContainer(MaterialDataContainer container)
        {
            if (container is MaterialUniformDataContainer uniformContainer)
            {
                return new
                {
                    Type = "Uniform",
                    container.Name,
                    container.TypeName,
                    Value = ConvertToSerializable(uniformContainer.Value)
                };
            }
            else if (container is MaterialSamplerDataContainer samplerContainer)
            {
                return new
                {
                    Type = "Sampler",
                    container.Name,
                    container.TypeName,
                    TextureGuid = samplerContainer.TextureGuid
                };
            }
            else if (container is MaterialSamplerArrayDataContainer samplerArrayContainer)
            {
                return new
                {
                    Type = "SamplerArray",
                    container.Name,
                    container.TypeName,
                    samplerArrayContainer.Size,
                    TextureGuids = samplerArrayContainer.TextureGuids.ToArray()
                };
            }
            else if (container is MaterialStructDataContainer structContainer)
            {
                return new
                {
                    Type = "Struct",
                    container.Name,
                    container.TypeName,
                    Fields = SerializeContainers(structContainer.Fields)
                };
            }
            else if (container is MaterialStructArrayDataContainer structArrayContainer)
            {
                var elements = new List<object>();
                foreach (var element in structArrayContainer.Elements)
                {
                    elements.Add(SerializeContainer(element));
                }

                return new
                {
                    Type = "StructArray",
                    container.Name,
                    container.TypeName,
                    ElementTypeName = structArrayContainer.ElementType?.FullName,
                    structArrayContainer.Size,
                    Elements = elements.ToArray()
                };
            }
            else if (container is MaterialArrayDataContainer arrayContainer)
            {
                var serializedValues = new List<object>();
                foreach (var value in arrayContainer.Values)
                {
                    serializedValues.Add(ConvertToSerializable(value));
                }

                return new
                {
                    Type = "Array",
                    container.Name,
                    container.TypeName,
                    ElementTypeName = arrayContainer.ElementType?.FullName,
                    arrayContainer.Size,
                    Values = serializedValues.ToArray()
                };
            }

            return null;
        }

        private static List<MaterialDataContainer> DeserializeContainers(JArray array)
        {
            var result = new List<MaterialDataContainer>();

            foreach (JObject item in array)
            {
                var container = DeserializeContainer(item);
                if (container != null)
                {
                    result.Add(container);
                }
            }

            return result;
        }

        private static MaterialDataContainer DeserializeContainer(JObject item)
        {
            try
            {
                string containerType = item["Type"]?.ToString();
                string name = item["Name"]?.ToString();
                string typeName = item["TypeName"]?.ToString();

                if (string.IsNullOrEmpty(containerType) || string.IsNullOrEmpty(name))
                    return null;

                switch (containerType)
                {
                    case "Uniform":
                        Type uniformType = GetTypeFromName(typeName);
                        if (uniformType == null)
                            return null;

                        var value = ConvertToTyped(item["Value"], uniformType);
                        return new MaterialUniformDataContainer
                        {
                            Name = name,
                            TypeName = typeName,
                            Type = uniformType,
                            Value = value
                        };

                    case "Sampler":
                        string textureGuid = item["TextureGuid"]?.ToString() ?? string.Empty;
                        return new MaterialSamplerDataContainer
                        {
                            Name = name,
                            TypeName = typeName,
                            TextureGuid = textureGuid
                        };

                    case "SamplerArray":
                        int samplerArraySize = item["Size"]?.Value<int>() ?? 0;
                        var textureGuids = item["TextureGuids"]?.ToObject<string[]>() ?? new string[0];

                        return new MaterialSamplerArrayDataContainer
                        {
                            Name = name,
                            TypeName = typeName,
                            Size = samplerArraySize,
                            TextureGuids = new List<string>(textureGuids)
                        };

                    case "Struct":
                        Type structType = GetTypeFromName(typeName);
                        if (structType == null)
                            return null;

                        var fields = DeserializeContainers(item["Fields"] as JArray ?? new JArray());

                        return new MaterialStructDataContainer
                        {
                            Name = name,
                            TypeName = typeName,
                            StructType = structType,
                            Fields = fields
                        };

                    case "StructArray":
                        string elementTypeName = item["ElementTypeName"]?.ToString();
                        Type elementType = GetTypeFromName(elementTypeName);
                        if (elementType == null)
                            return null;

                        int structArraySize = item["Size"]?.Value<int>() ?? 0;
                        var elementsArray = item["Elements"] as JArray ?? new JArray();
                        var elements = new List<MaterialStructDataContainer>();

                        foreach (JObject elementObj in elementsArray)
                        {
                            var element = DeserializeContainer(elementObj) as MaterialStructDataContainer;
                            if (element != null)
                            {
                                elements.Add(element);
                            }
                        }

                        return new MaterialStructArrayDataContainer
                        {
                            Name = name,
                            TypeName = typeName,
                            ElementType = elementType,
                            Size = structArraySize,
                            Elements = elements
                        };

                    case "Array":
                        string arrayElementTypeName = item["ElementTypeName"]?.ToString();
                        Type arrayElementType = GetTypeFromName(arrayElementTypeName);
                        if (arrayElementType == null)
                            return null;

                        int arraySize = item["Size"]?.Value<int>() ?? 0;
                        var valuesArray = item["Values"] as JArray ?? new JArray();
                        var values = new List<object>();

                        foreach (var valueToken in valuesArray)
                        {
                            var value_ = ConvertToTyped(valueToken, arrayElementType);
                            values.Add(value_);
                        }

                        return new MaterialArrayDataContainer
                        {
                            Name = name,
                            TypeName = typeName,
                            ElementType = arrayElementType,
                            Size = arraySize,
                            Values = values
                        };
                }
            }
            catch (Exception ex)
            {
                DebLogger.Debug($"Ошибка десериализации контейнера: {ex.Message}");
            }

            return null;
        }

        private static Type GetTypeFromName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return null;

            Type type = ServiceHub.Get<AssemblyManager>().FindType(typeName, true);

            if (type == null)
            {
                type = Type.GetType(typeName);
            }

            return type;
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

        private static object ConvertToTyped(JToken token, Type targetType)
        {
            if (token == null)
                return null;

            try
            {
                if (targetType == typeof(float))
                    return token.Value<float>();
                else if (targetType == typeof(int))
                    return token.Value<int>();
                else if (targetType == typeof(bool))
                    return token.Value<bool>();
                else if (targetType == typeof(double))
                    return token.Value<double>();
                else if (targetType == typeof(string))
                    return token.Value<string>();
                else if (targetType == typeof(Vector2D<float>))
                {
                    var obj = token as JObject;
                    if (obj != null && obj["X"] != null && obj["Y"] != null)
                    {
                        return new Vector2D<float>(
                            obj["X"].Value<float>(),
                            obj["Y"].Value<float>()
                        );
                    }
                }
                else if (targetType == typeof(Vector3D<float>))
                {
                    var obj = token as JObject;
                    if (obj != null && obj["X"] != null && obj["Y"] != null && obj["Z"] != null)
                    {
                        return new Vector3D<float>(
                            obj["X"].Value<float>(),
                            obj["Y"].Value<float>(),
                            obj["Z"].Value<float>()
                        );
                    }
                }
                else if (targetType == typeof(Vector4D<float>))
                {
                    var obj = token as JObject;
                    if (obj != null && obj["X"] != null && obj["Y"] != null && obj["Z"] != null && obj["W"] != null)
                    {
                        return new Vector4D<float>(
                            obj["X"].Value<float>(),
                            obj["Y"].Value<float>(),
                            obj["Z"].Value<float>(),
                            obj["W"].Value<float>()
                        );
                    }
                }

                return token.ToObject(targetType);
            }
            catch (Exception ex)
            {
                DebLogger.Debug($"Ошибка преобразования типа {targetType.Name}: {ex.Message}");
                return null;
            }
        }
    }
}
