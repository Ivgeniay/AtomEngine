using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System;

namespace Editor.Utils.Generator
{
    internal static class ShaderComponentGenerator
    {
        public static void GenerateComponentFromRepresentation(string representationFilePath, string outputDirectory)
        {
            if (!File.Exists(representationFilePath))
            {
                throw new FileNotFoundException($"Representation file not found: {representationFilePath}");
            }

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            string sourceCode = File.ReadAllText(representationFilePath);
            string className = ExtractClassName(sourceCode);
            if (string.IsNullOrEmpty(className))
            {
                throw new Exception($"Could not extract class name from {representationFilePath}");
            }

            var properties = ExtractShaderProperties(sourceCode);
            string componentName = className + "Component";
            string componentCode = GenerateComponentCode(componentName, className, properties);

            string outputPath = Path.Combine(outputDirectory, $"{componentName}.g.cs");
            File.WriteAllText(outputPath, componentCode, Encoding.UTF8);
        }

        public static void GenerateComponentsFromDirectory(string directoryPath, string outputDirectory, string searchPattern = "*Representation.g.cs")
        {
            if (!Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
            }

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var representationFiles = Directory.GetFiles(directoryPath, searchPattern, SearchOption.TopDirectoryOnly);
            foreach (var file in representationFiles)
            {
                try
                {
                    GenerateComponentFromRepresentation(file, outputDirectory);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing {file}: {ex.Message}");
                }
            }
        }

        private static string ExtractClassName(string sourceCode)
        {
            var match = Regex.Match(sourceCode, @"public\s+(?:partial\s+)?class\s+(\w+)\s*:\s*Mat");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return null;
        }

        private static List<(string type, string name, bool isTexture)> ExtractShaderProperties(string sourceCode)
        {
            var properties = new List<(string type, string name, bool isTexture)>();
            var matches = Regex.Matches(sourceCode, @"public\s+(?:unsafe\s+)?(\w+(?:<\w+>)?)\s+(\w+)(?!\s*Location)(?:\s*\{(?:[^{}]*|(?<brace>\{)|(?<-brace>\}))*\})");

            var textureSetters = Regex.Matches(sourceCode, @"public\s+void\s+(\w+)_SetTexture");
            var textureFields = new HashSet<string>();

            foreach (Match match in textureSetters)
            {
                textureFields.Add(match.Groups[1].Value);
            }

            foreach (Match match in matches)
            {
                string type = match.Groups[1].Value;
                string name = match.Groups[2].Value;

                if (!name.Contains("Location") && !sourceCode.Contains($"public {type} {name}("))
                {
                    if (!type.Contains("Array") && !type.Contains("List"))
                    {
                        bool isTexture = textureFields.Contains(name);
                        if (isTexture && type == "int")
                        {
                            properties.Add((type, name, true));
                        }
                        else if (!isTexture)
                        {
                            var mappedType = MapToSystemNumerics(type);
                            properties.Add((mappedType, name, false));
                        }
                    }
                }
            }

            return properties;
        }

        private static string MapToSystemNumerics(string type)
        {
            switch (type)
            {
                case "Vector2D<float>":
                    return "Vector2";
                case "Vector3D<float>":
                    return "Vector3";
                case "Vector4D<float>":
                    return "Vector4";
                case "Matrix4X4<float>":
                    return "Matrix4x4";
                default:
                    return type;
            }
        }

        private static string GenerateComponentCode(string componentName, string shaderClassName, List<(string type, string name, bool isTexture)> properties)
        {
            var sb = new StringBuilder();

            sb.AppendLine("using System;");
            sb.AppendLine("using System.Numerics;");
            sb.AppendLine("using AtomEngine;");
            sb.AppendLine("using OpenglLib;");
            sb.AppendLine("using EngineLib;");
            sb.AppendLine();

            sb.AppendLine("namespace AtomEngine");
            sb.AppendLine("{");

            sb.AppendLine("    [GLDependable]");
            sb.AppendLine($"    public partial struct {componentName} : IComponent");
            sb.AppendLine("    {");

            sb.AppendLine("        public Entity Owner { get; set; }");
            sb.AppendLine();

            sb.AppendLine($"        public {shaderClassName} Shader;");
            sb.AppendLine();

            foreach (var (type, name, isTexture) in properties)
            {
                if (isTexture)
                {
                    sb.AppendLine($"        public Texture {name}Texture;");
                }
                else
                {
                    sb.AppendLine($"        [ReadOnly]");
                    sb.AppendLine($"        public {type} {name};");
                }
                sb.AppendLine();
            }

            sb.AppendLine($"        public {componentName}(Entity owner, {shaderClassName} shader)");
            sb.AppendLine("        {");
            sb.AppendLine("            Owner = owner;");
            sb.AppendLine("            Shader = shader;");

            foreach (var (type, name, isTexture) in properties)
            {
                if (isTexture)
                {
                    sb.AppendLine($"            {name}Texture = null;");
                }
                else
                {
                    switch (type)
                    {
                        case "int":
                        case "uint":
                            sb.AppendLine($"            {name} = 0;");
                            break;
                        case "float":
                            sb.AppendLine($"            {name} = 0.0f;");
                            break;
                        case "bool":
                            sb.AppendLine($"            {name} = false;");
                            break;
                        case "Vector2":
                            sb.AppendLine($"            {name} = Vector2.Zero;");
                            break;
                        case "Vector3":
                            sb.AppendLine($"            {name} = Vector3.Zero;");
                            break;
                        case "Vector4":
                            sb.AppendLine($"            {name} = Vector4.Zero;");
                            break;
                        default:
                            sb.AppendLine($"            {name} = default;");
                            break;
                    }
                }
            }

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }
    }


}
