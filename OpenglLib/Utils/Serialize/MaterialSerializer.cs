using Newtonsoft.Json.Linq;
using System.Numerics;
using Newtonsoft.Json;
using Silk.NET.Maths;
using AtomEngine;
using EngineLib;

namespace OpenglLib
{
    public static class MaterialSerializer
    {
        private static readonly Dictionary<string, Type> TypeCache = new Dictionary<string, Type>(StringComparer.Ordinal);
        private static readonly HashSet<Type> SpecialTypes = new HashSet<Type>
        {
            typeof(Vector2),
            typeof(Vector3),
            typeof(Vector4),
            typeof(Matrix4x4),
            typeof(Vector2D<float>),
            typeof(Vector3D<float>),
            typeof(Vector4D<float>),
            typeof(Matrix2X2<float>),
            typeof(Matrix2X3<float>),
            typeof(Matrix2X4<float>),
            typeof(Matrix3X2<float>),
            typeof(Matrix3X3<float>),
            typeof(Matrix3X4<float>),
            typeof(Matrix4X2<float>),
            typeof(Matrix4X3<float>),
            typeof(Matrix4X4<float>),
            typeof(Vector2D<int>),
            typeof(Vector3D<int>),
            typeof(Vector4D<int>),
            typeof(Vector2D<uint>),
            typeof(Vector3D<uint>),
            typeof(Vector4D<uint>),
            typeof(Vector2D<double>),
            typeof(Vector3D<double>),
            typeof(Vector4D<double>),
            typeof(Matrix2X2<double>),
            typeof(Matrix2X3<double>),
            typeof(Matrix2X4<double>),
            typeof(Matrix3X2<double>),
            typeof(Matrix3X3<double>),
            typeof(Matrix3X4<double>),
            typeof(Matrix4X2<double>),
            typeof(Matrix4X3<double>),
            typeof(Matrix4X4<double>),
        };

        private static readonly Dictionary<string, Type> SpecialTypesByName;
        private static AssemblyManager assemblyManager;

        static MaterialSerializer()
        {
            SpecialTypesByName = new Dictionary<string, Type>(StringComparer.Ordinal);
            foreach (var type in SpecialTypes)
            {
                SpecialTypesByName[type.FullName] = type;
                SpecialTypesByName[type.Name] = type;

                if (type.IsGenericType)
                {
                    string genericName = type.Name;
                    int index = genericName.IndexOf('`');
                    if (index != -1)
                    {
                        genericName = genericName.Substring(0, index);
                        if (!SpecialTypesByName.ContainsKey(genericName))
                        {
                            SpecialTypesByName[genericName] = type;
                        }
                    }
                }
            }
        }

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
                material.ShaderGuid,
                material.ShaderRepresentationTypeName,
                Values = SerializeContainers(material.GetAllContainers()),
            };

            return JsonConvert.SerializeObject(serializableData, settings);
        }

        public static MaterialAsset DeserializeMaterial(string json)
        {
            var material = new MaterialAsset();

            try
            {
                var jsonObj = JsonConvert.DeserializeObject<JObject>(json);
                if (jsonObj == null)
                {
                    DebLogger.Error("Невозможно десериализовать материал: JSON не является объектом");
                    return material;
                }

                material.Guid = jsonObj["Guid"]?.ToString();
                material.ShaderGuid = jsonObj["ShaderGuid"]?.ToString();
                material.ShaderRepresentationTypeName = jsonObj["ShaderRepresentationTypeName"]?.ToString();

                if (jsonObj["Values"] != null && jsonObj["Values"] is JArray valuesArray)
                {
                    var containers = DeserializeContainers(valuesArray);
                    foreach (var container in containers)
                    {
                        material.AddContainer(container);
                    }
                }
                else
                {
                    DebLogger.Warn("Материал не содержит данных о значениях или формат неверен");
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
                try
                {
                    var serialized = SerializeContainer(container);
                    if (serialized != null)
                    {
                        result.Add(serialized);
                    }
                }
                catch (Exception ex)
                {
                    DebLogger.Warn($"Ошибка сериализации контейнера {container.Name}: {ex.Message}");
                }
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
                try
                {
                    var container = DeserializeContainer(item);
                    if (container != null)
                    {
                        result.Add(container);
                    }
                }
                catch (Exception ex)
                {
                    DebLogger.Warn($"Ошибка десериализации контейнера: {ex.Message}");
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
                {
                    DebLogger.Warn($"Пропущен контейнер с неверным типом или именем: {containerType}, {name}");
                    return null;
                }

                switch (containerType)
                {
                    case "Uniform":
                        Type uniformType = GetTypeFromName(typeName);
                        if (uniformType == null)
                        {
                            DebLogger.Warn($"Не удалось найти тип {typeName} для uniform-контейнера {name}");
                            uniformType = typeof(float);
                        }

                        var value = ConvertToTyped(item["Value"], uniformType);
                        return new MaterialUniformDataContainer
                        {
                            Name = name,
                            TypeName = typeName,
                            Type = uniformType,
                            Value = value ?? GetDefaultValueForType(uniformType)
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
                        {
                            DebLogger.Warn($"Не удалось найти тип {typeName} для struct-контейнера {name}");
                            return null;
                        }

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
                        {
                            DebLogger.Warn($"Не удалось найти тип элемента {elementTypeName} для struct-array-контейнера {name}");
                            return null;
                        }

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
                        {
                            DebLogger.Warn($"Не удалось найти тип элемента {arrayElementTypeName} для array-контейнера {name}");
                            return null;
                        }

                        int arraySize = item["Size"]?.Value<int>() ?? 0;
                        var valuesArray = item["Values"] as JArray ?? new JArray();
                        var values = new List<object>();

                        foreach (var valueToken in valuesArray)
                        {
                            var value_ = ConvertToTyped(valueToken, arrayElementType);
                            values.Add(value_ ?? GetDefaultValueForType(arrayElementType));
                        }

                        return new MaterialArrayDataContainer
                        {
                            Name = name,
                            TypeName = typeName,
                            ElementType = arrayElementType,
                            Size = arraySize,
                            Values = values
                        };

                    default:
                        DebLogger.Warn($"Неизвестный тип контейнера: {containerType}");
                        return null;
                }
            }
            catch (Exception ex)
            {
                DebLogger.Debug($"Ошибка десериализации контейнера: {ex.Message}");
                return null;
            }
        }

        private static Type GetTypeFromName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return null;

            if (TypeCache.TryGetValue(typeName, out Type cachedType))
                return cachedType;

            if (typeName.Contains("Silk.NET.Maths."))
            {
                if (typeName.Contains("Vector2D") && typeName.Contains("System.Single"))
                    return typeof(Vector2D<float>);
                if (typeName.Contains("Vector3D") && typeName.Contains("System.Single"))
                    return typeof(Vector3D<float>);
                if (typeName.Contains("Vector4D") && typeName.Contains("System.Single"))
                    return typeof(Vector4D<float>);
                if (typeName.Contains("Vector2D") && typeName.Contains("System.Int32"))
                    return typeof(Vector2D<int>);
                if (typeName.Contains("Vector3D") && typeName.Contains("System.Int32"))
                    return typeof(Vector3D<int>);
                if (typeName.Contains("Vector4D") && typeName.Contains("System.Int32"))
                    return typeof(Vector4D<int>);
                if (typeName.Contains("Vector2D") && typeName.Contains("System.UInt32"))
                    return typeof(Vector2D<uint>);
                if (typeName.Contains("Vector3D") && typeName.Contains("System.UInt32"))
                    return typeof(Vector3D<uint>);
                if (typeName.Contains("Vector4D") && typeName.Contains("System.UInt32"))
                    return typeof(Vector4D<uint>);
                if (typeName.Contains("Vector2D") && typeName.Contains("System.Double"))
                    return typeof(Vector2D<double>);
                if (typeName.Contains("Vector3D") && typeName.Contains("System.Double"))
                    return typeof(Vector3D<double>);
                if (typeName.Contains("Vector4D") && typeName.Contains("System.Double"))
                    return typeof(Vector4D<double>);

                if (typeName.Contains("Matrix2X2") && typeName.Contains("System.Single"))
                    return typeof(Matrix2X2<float>);
                if (typeName.Contains("Matrix3X3") && typeName.Contains("System.Single"))
                    return typeof(Matrix3X3<float>);
                if (typeName.Contains("Matrix4X4") && typeName.Contains("System.Single"))
                    return typeof(Matrix4X4<float>);
                if (typeName.Contains("Matrix2X3") && typeName.Contains("System.Single"))
                    return typeof(Matrix2X3<float>);
                if (typeName.Contains("Matrix2X4") && typeName.Contains("System.Single"))
                    return typeof(Matrix2X4<float>);
                if (typeName.Contains("Matrix3X2") && typeName.Contains("System.Single"))
                    return typeof(Matrix3X2<float>);
                if (typeName.Contains("Matrix3X4") && typeName.Contains("System.Single"))
                    return typeof(Matrix3X4<float>);
                if (typeName.Contains("Matrix4X2") && typeName.Contains("System.Single"))
                    return typeof(Matrix4X2<float>);
                if (typeName.Contains("Matrix4X3") && typeName.Contains("System.Single"))
                    return typeof(Matrix4X3<float>);

                if (typeName.Contains("Matrix2X2") && typeName.Contains("System.Double"))
                    return typeof(Matrix2X2<double>);
                if (typeName.Contains("Matrix3X3") && typeName.Contains("System.Double"))
                    return typeof(Matrix3X3<double>);
                if (typeName.Contains("Matrix4X4") && typeName.Contains("System.Double"))
                    return typeof(Matrix4X4<double>);
                if (typeName.Contains("Matrix2X3") && typeName.Contains("System.Double"))
                    return typeof(Matrix2X3<double>);
                if (typeName.Contains("Matrix2X4") && typeName.Contains("System.Double"))
                    return typeof(Matrix2X4<double>);
                if (typeName.Contains("Matrix3X2") && typeName.Contains("System.Double"))
                    return typeof(Matrix3X2<double>);
                if (typeName.Contains("Matrix3X4") && typeName.Contains("System.Double"))
                    return typeof(Matrix3X4<double>);
                if (typeName.Contains("Matrix4X2") && typeName.Contains("System.Double"))
                    return typeof(Matrix4X2<double>);
                if (typeName.Contains("Matrix4X3") && typeName.Contains("System.Double"))
                    return typeof(Matrix4X3<double>);
            }

            if (SpecialTypesByName.TryGetValue(typeName, out Type specialType))
            {
                TypeCache[typeName] = specialType;
                return specialType;
            }

            Type type = Type.GetType(typeName);
            if (type != null)
            {
                TypeCache[typeName] = type;
                return type;
            }

            if (assemblyManager == null)
                assemblyManager = ServiceHub.Get<AssemblyManager>();

            type = assemblyManager.FindType(typeName, true);
            if (type != null)
            {
                TypeCache[typeName] = type;
                return type;
            }

            DebLogger.Warn($"Тип не найден: {typeName}");
            return null;
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
            else if (type == typeof(Matrix2X2<float>))
            {
                var mat = (Matrix2X2<float>)value;
                return new { M11 = mat.M11, M12 = mat.M12, M21 = mat.M21, M22 = mat.M22 };
            }
            else if (type == typeof(Matrix3X3<float>))
            {
                var mat = (Matrix3X3<float>)value;
                return new
                {
                    M11 = mat.M11,
                    M12 = mat.M12,
                    M13 = mat.M13,
                    M21 = mat.M21,
                    M22 = mat.M22,
                    M23 = mat.M23,
                    M31 = mat.M31,
                    M32 = mat.M32,
                    M33 = mat.M33
                };
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
                else if (targetType == typeof(uint))
                    return token.Value<uint>();
                else if (targetType == typeof(double))
                    return token.Value<double>();
                else if (targetType == typeof(bool))
                    return token.Value<bool>();
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
                else if (targetType == typeof(Vector2))
                {
                    var obj = token as JObject;
                    if (obj != null && obj["X"] != null && obj["Y"] != null)
                    {
                        return new Vector2(
                            obj["X"].Value<float>(),
                            obj["Y"].Value<float>()
                        );
                    }
                }
                else if (targetType == typeof(Vector3))
                {
                    var obj = token as JObject;
                    if (obj != null && obj["X"] != null && obj["Y"] != null && obj["Z"] != null)
                    {
                        return new Vector3(
                            obj["X"].Value<float>(),
                            obj["Y"].Value<float>(),
                            obj["Z"].Value<float>()
                        );
                    }
                }
                else if (targetType == typeof(Vector4))
                {
                    var obj = token as JObject;
                    if (obj != null && obj["X"] != null && obj["Y"] != null && obj["Z"] != null && obj["W"] != null)
                    {
                        return new Vector4(
                            obj["X"].Value<float>(),
                            obj["Y"].Value<float>(),
                            obj["Z"].Value<float>(),
                            obj["W"].Value<float>()
                        );
                    }
                }
                else if (targetType == typeof(Matrix2X2<float>))
                {
                    var obj = token as JObject;
                    if (obj != null && obj["M11"] != null && obj["M12"] != null && obj["M21"] != null && obj["M22"] != null)
                    {
                        return new Matrix2X2<float>(
                            obj["M11"].Value<float>(), obj["M12"].Value<float>(),
                            obj["M21"].Value<float>(), obj["M22"].Value<float>()
                        );
                    }
                    return Matrix2X2<float>.Identity;
                }
                else if (targetType == typeof(Matrix3X3<float>))
                {
                    var obj = token as JObject;
                    if (obj != null && obj["M11"] != null)
                    {
                        return new Matrix3X3<float>(
                            obj["M11"].Value<float>(), obj["M12"].Value<float>(), obj["M13"].Value<float>(),
                            obj["M21"].Value<float>(), obj["M22"].Value<float>(), obj["M23"].Value<float>(),
                            obj["M31"].Value<float>(), obj["M32"].Value<float>(), obj["M33"].Value<float>()
                        );
                    }
                    return Matrix3X3<float>.Identity;
                }
                else if (targetType == typeof(Matrix4X4<float>))
                {
                    var obj = token as JObject;
                    if (obj != null && obj["M11"] != null)
                    {
                        return new Matrix4X4<float>(
                            obj["M11"].Value<float>(), obj["M12"].Value<float>(), obj["M13"].Value<float>(), obj["M14"].Value<float>(),
                            obj["M21"].Value<float>(), obj["M22"].Value<float>(), obj["M23"].Value<float>(), obj["M24"].Value<float>(),
                            obj["M31"].Value<float>(), obj["M32"].Value<float>(), obj["M33"].Value<float>(), obj["M34"].Value<float>(),
                            obj["M41"].Value<float>(), obj["M42"].Value<float>(), obj["M43"].Value<float>(), obj["M44"].Value<float>()
                        );
                    }
                    return Matrix4X4<float>.Identity;
                }

                try
                {
                    return token.ToObject(targetType);
                }
                catch
                {
                    DebLogger.Warn($"Не удалось преобразовать значение в тип {targetType.Name}");
                    return GetDefaultValueForType(targetType);
                }
            }
            catch (Exception ex)
            {
                DebLogger.Debug($"Ошибка преобразования типа {targetType.Name}: {ex.Message}");
                return GetDefaultValueForType(targetType);
            }
        }

        private static object GetDefaultValueForType(Type type)
        {
            if (type == typeof(float)) return 0.0f;
            if (type == typeof(int)) return 0;
            if (type == typeof(uint)) return 0u;
            if (type == typeof(double)) return 0.0;
            if (type == typeof(bool)) return false;
            if (type == typeof(string)) return string.Empty;

            if (type == typeof(Vector2D<float>)) return new Vector2D<float>(0, 0);
            if (type == typeof(Vector2D<int>)) return new Vector2D<int>(0, 0);
            if (type == typeof(Vector2D<uint>)) return new Vector2D<uint>(0, 0);
            if (type == typeof(Vector2D<double>)) return new Vector2D<double>(0, 0);

            if (type == typeof(Vector3D<float>)) return new Vector3D<float>(0, 0, 0);
            if (type == typeof(Vector3D<int>)) return new Vector3D<int>(0, 0, 0);
            if (type == typeof(Vector3D<uint>)) return new Vector3D<uint>(0, 0, 0);
            if (type == typeof(Vector3D<double>)) return new Vector3D<double>(0, 0, 0);

            if (type == typeof(Vector4D<float>)) return new Vector4D<float>(0, 0, 0, 0);
            if (type == typeof(Vector4D<int>)) return new Vector4D<int>(0, 0, 0, 0);
            if (type == typeof(Vector4D<uint>)) return new Vector4D<uint>(0, 0, 0, 0);
            if (type == typeof(Vector4D<double>)) return new Vector4D<double>(0, 0, 0, 0);

            if (type == typeof(Vector2)) return new Vector2(0, 0);
            if (type == typeof(Vector3)) return new Vector3(0, 0, 0);
            if (type == typeof(Vector4)) return new Vector4(0, 0, 0, 0);
            if (type == typeof(Matrix4x4)) return Matrix4x4.Identity;

            if (type == typeof(Matrix2X2<float>)) return Matrix2X2<float>.Identity;
            if (type == typeof(Matrix2X3<float>)) return Matrix2X3<float>.Identity;
            if (type == typeof(Matrix2X4<float>)) return Matrix2X4<float>.Identity;
            if (type == typeof(Matrix3X2<float>)) return Matrix3X2<float>.Identity;
            if (type == typeof(Matrix3X3<float>)) return Matrix3X3<float>.Identity;
            if (type == typeof(Matrix3X4<float>)) return Matrix3X4<float>.Identity;
            if (type == typeof(Matrix4X2<float>)) return Matrix4X2<float>.Identity;
            if (type == typeof(Matrix4X3<float>)) return Matrix4X3<float>.Identity;
            if (type == typeof(Matrix4X4<float>)) return Matrix4X4<float>.Identity;

            if (type == typeof(Matrix2X2<double>)) return Matrix2X2<double>.Identity;
            if (type == typeof(Matrix2X3<double>)) return Matrix2X3<double>.Identity;
            if (type == typeof(Matrix2X4<double>)) return Matrix2X4<double>.Identity;
            if (type == typeof(Matrix3X2<double>)) return Matrix3X2<double>.Identity;
            if (type == typeof(Matrix3X3<double>)) return Matrix3X3<double>.Identity;
            if (type == typeof(Matrix3X4<double>)) return Matrix3X4<double>.Identity;
            if (type == typeof(Matrix4X2<double>)) return Matrix4X2<double>.Identity;
            if (type == typeof(Matrix4X3<double>)) return Matrix4X3<double>.Identity;
            if (type == typeof(Matrix4X4<double>)) return Matrix4X4<double>.Identity;

            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }

            return null;
        }
    }

}
