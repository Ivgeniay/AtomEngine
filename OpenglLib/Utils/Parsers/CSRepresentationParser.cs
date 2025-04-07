using AtomEngine;
using EngineLib;
using Silk.NET.Maths;
using System.Reflection;

namespace OpenglLib
{
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
            var propertyRegex = new System.Text.RegularExpressions.Regex(
                @"public\s+(?:unsafe\s+)?(\w+(?:<\w+>)?)\s+(\w+)\s*\{",
                System.Text.RegularExpressions.RegexOptions.Multiline
            );

            var samplerRegex = new System.Text.RegularExpressions.Regex(
                @"public\s+void\s+(\w+)_SetTexture\s*\(OpenglLib\.Texture\s+\w+\)",
                System.Text.RegularExpressions.RegexOptions.Multiline
            );

            var propertyMatches = propertyRegex.Matches(code);
            foreach (System.Text.RegularExpressions.Match match in propertyMatches)
            {
                if (match.Groups.Count > 2)
                {
                    string typeName = match.Groups[1].Value;
                    string propertyName = match.Groups[2].Value;

                    if (typeName.Contains("Array") || propertyName.EndsWith("Location") ||
                        typeName.Contains("Struct") || IsSamplerType(typeName))
                        continue;

                    properties.Add(propertyName, GetDefaultValueForType(typeName));
                }
            }

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


        public static Dictionary<string, int> ExtractArraySizesFromCode(string shaderCode)
        {
            Dictionary<string, int> result = new Dictionary<string, int>();

            try
            {
                var samplerArrayMatches = System.Text.RegularExpressions.Regex.Matches(
                    shaderCode,
                    @"_(\w+)\s*=\s*new\s+SamplerArray\s*\(\s*[^,]*\s*,\s*(\d+)",
                    System.Text.RegularExpressions.RegexOptions.Multiline
                );

                foreach (System.Text.RegularExpressions.Match match in samplerArrayMatches)
                {
                    if (match.Groups.Count > 2)
                    {
                        string fieldName = match.Groups[1].Value;
                        if (int.TryParse(match.Groups[2].Value, out int size) && size > 0)
                        {
                            result[fieldName] = size;
                        }
                    }
                }

                var localeArrayMatches = System.Text.RegularExpressions.Regex.Matches(
                    shaderCode,
                    @"_(\w+)\s*=\s*new\s+LocaleArray<[^>]+>\s*\(\s*(\d+)",
                    System.Text.RegularExpressions.RegexOptions.Multiline
                );

                foreach (System.Text.RegularExpressions.Match match in localeArrayMatches)
                {
                    if (match.Groups.Count > 2)
                    {
                        string fieldName = match.Groups[1].Value;
                        if (int.TryParse(match.Groups[2].Value, out int size) && size > 0)
                        {
                            result[fieldName] = size;
                        }
                    }
                }

                var structArrayMatches = System.Text.RegularExpressions.Regex.Matches(
                    shaderCode,
                    @"_(\w+)\s*=\s*new\s+StructArray<[^>]+>\s*\(\s*(\d+)",
                    System.Text.RegularExpressions.RegexOptions.Multiline
                );

                foreach (System.Text.RegularExpressions.Match match in structArrayMatches)
                {
                    if (match.Groups.Count > 2)
                    {
                        string fieldName = match.Groups[1].Value;
                        if (int.TryParse(match.Groups[2].Value, out int size) && size > 0)
                        {
                            result[fieldName] = size;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при извлечении размеров массивов: {ex.Message}");
            }

            return result;
        }

    }


    public static class ShaderRepresentationAnalyzer
    {
        public static List<MaterialDataContainer> AnalyzeShaderRepresentation(string typeName, string shaderCode)
        {
            Type shaderType = ServiceHub.Get<AssemblyManager>().FindType(typeName, true);
            if (shaderType == null)
            {
                DebLogger.Error($"Тип шейдерного представления не найден: {typeName}");
                return new List<MaterialDataContainer>();
            }

            Dictionary<string, int> arraySizes = CSRepresentationParser.ExtractArraySizesFromCode(shaderCode);
            return AnalyzeType(shaderType, arraySizes);
        }

        private static List<MaterialDataContainer> AnalyzeType(Type shaderType, Dictionary<string, int> arraySizes)
        {
            var result = new List<MaterialDataContainer>();
            var properties = shaderType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                if (property.Name.EndsWith("Location") || 
                    property.Name == "Handle" ||
                    property.Name.StartsWith("_")
                    )
                    continue;

                var container = CreateContainerForProperty(property, arraySizes);
                if (container != null)
                    result.Add(container);
            }

            return result;
        }

        private static MaterialDataContainer CreateContainerForProperty(PropertyInfo property, Dictionary<string, int> arraySizes)
        {
            var propertyType = property.PropertyType;
            var propertyName = property.Name;

            string privateFieldName = "_" + propertyName;

            int GetArraySize()
            {
                if (arraySizes.TryGetValue(privateFieldName, out int size))
                    return size;

                if (arraySizes.TryGetValue(propertyName, out size))
                    return size;

                return 1;
            }

            if (MaterialSupportedTypes.IsTextureType(propertyType))
            {
                return new MaterialSamplerDataContainer { 
                    Name = propertyName,
                    TypeName = propertyType.FullName
                };
            }
            else if (MaterialSupportedTypes.IsSamplerArrayType(propertyType))
            {
                int size = GetArraySize();
                return new MaterialSamplerArrayDataContainer
                {
                    Name = propertyName,
                    TypeName = propertyType.FullName,
                    Size = size,
                    TextureGuids = new List<string>(new string[size])
                };
            }
            else if (MaterialSupportedTypes.IsCustomStructType(propertyType))
            {
                var structContainer = new MaterialStructDataContainer
                {
                    Name = propertyName,
                    TypeName = propertyType.FullName,
                    StructType = propertyType,
                    Fields = AnalyzeStructType(propertyType, arraySizes)
                };
                return structContainer;
            }
            else if (MaterialSupportedTypes.IsStructArrayType(propertyType))
            {
                Type elementType = propertyType.GetGenericArguments()[0];
                int size = GetArraySize();

                var elements = new List<MaterialStructDataContainer>();
                for (int i = 0; i < size; i++)
                {
                    elements.Add(new MaterialStructDataContainer
                    {
                        Name = $"{propertyName}[{i}]",
                        TypeName = elementType.FullName,
                        StructType = elementType,
                        Fields = AnalyzeStructType(elementType, arraySizes)
                    });
                }

                return new MaterialStructArrayDataContainer
                {
                    Name = propertyName,
                    TypeName = propertyType.FullName,
                    ElementType = elementType,
                    Size = size,
                    Elements = elements
                };
            }
            else if (MaterialSupportedTypes.IsLocaleArrayType(propertyType))
            {
                Type elementType = propertyType.GetGenericArguments()[0];
                int size = GetArraySize();

                var values = new List<object>();
                for (int i = 0; i < size; i++)
                {
                    values.Add(CreateDefaultValue(elementType));
                }

                return new MaterialArrayDataContainer
                {
                    Name = propertyName,
                    TypeName = propertyType.FullName,
                    ElementType = elementType,
                    Size = size,
                    Values = values
                };
            }
            else if (MaterialSupportedTypes.IsSupportedPrimitiveType(propertyType))
            {
                return new MaterialUniformDataContainer
                {
                    Name = propertyName,
                    TypeName = propertyType.FullName,
                    Type = propertyType,
                    Value = CreateDefaultValue(propertyType)
                };
            }

            return null;
        }

        private static List<MaterialDataContainer> AnalyzeStructType(Type structType, Dictionary<string, int> arraySizes)
        {
            var result = new List<MaterialDataContainer>();
            var properties = structType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                if (property.Name == "IsDirty" || 
                    property.Name == "Owner" ||
                    property.Name.StartsWith("_") ||
                    property.Name.EndsWith("Location")
                    )
                    continue;

                var container = CreateContainerForProperty(property, arraySizes);
                if (container != null)
                    result.Add(container);
            }

            return result;
        }

        private static object CreateDefaultValue(Type type)
        {
            if (type == typeof(float)) return 0.0f;
            if (type == typeof(int)) return 0;
            if (type == typeof(bool)) return false;
            if (type == typeof(double)) return 0.0;
            if (type == typeof(Vector2D<float>)) return new Vector2D<float>(0, 0);
            if (type == typeof(Vector3D<float>)) return new Vector3D<float>(0, 0, 0);
            if (type == typeof(Vector4D<float>)) return new Vector4D<float>(0, 0, 0, 0);

            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }
    }

    public static class MaterialSupportedTypes
    {
        private static readonly HashSet<Type> SupportedPrimitiveTypes = new HashSet<Type>
        {
            typeof(float),
            typeof(int),
            typeof(bool),
            typeof(double),
            typeof(Vector2D<float>),
            typeof(Vector3D<float>),
            typeof(Vector4D<float>),
        };


        public static bool IsSupportedPrimitiveType(Type type)
        {
            return SupportedPrimitiveTypes.Contains(type) ||
                   type.IsPrimitive ||
                   type == typeof(string);
        }

        public static bool IsCustomStructType(Type type)
        {
            return type != null && typeof(CustomStruct).IsAssignableFrom(type);
        }

        public static bool IsTextureType(Type type)
        {
            return type == typeof(OpenglLib.Texture);
        }

        public static bool IsLocaleArrayType(Type type)
        {
            return type != null &&
                   type.IsGenericType &&
                   type.GetGenericTypeDefinition() == typeof(LocaleArray<>);
        }

        public static bool IsStructArrayType(Type type)
        {
            return type != null &&
                   type.IsGenericType &&
                   type.GetGenericTypeDefinition() == typeof(StructArray<>);
        }

        public static bool IsSamplerArrayType(Type type)
        {
            return type == typeof(SamplerArray);
        }
    }
}
