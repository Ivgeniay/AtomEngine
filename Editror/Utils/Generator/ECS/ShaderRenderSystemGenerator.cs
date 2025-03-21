using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System;

namespace Editor.Utils.Generator
{
    internal static class ShaderRenderSystemGenerator
    {
        public static void GenerateRenderSystemFromRepresentation(string representationFilePath, string outputDirectory)
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
            string systemName = className + "RenderSystem";
            string componentName = className + "Component";
            string systemCode = GenerateSystemCode(systemName, componentName, className, properties);

            string outputPath = Path.Combine(outputDirectory, $"{systemName}.g.cs");
            File.WriteAllText(outputPath, systemCode, Encoding.UTF8);
        }

        public static void GenerateRenderSystemsFromDirectory(string directoryPath, string outputDirectory,
            string searchPattern = "*Representation.g.cs")
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
                    GenerateRenderSystemFromRepresentation(file, outputDirectory);
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

        private static List<(string type, string name)> ExtractShaderProperties(string sourceCode)
        {
            var properties = new List<(string type, string name)>();
            var matches = Regex.Matches(sourceCode, @"public\s+(?:unsafe\s+)?(\w+(?:<\w+>)?)\s+(\w+)(?!\s*Location)(?:\s*\{(?:[^{}]*|(?<brace>\{)|(?<-brace>\}))*\})");

            foreach (Match match in matches)
            {
                string type = match.Groups[1].Value;
                string name = match.Groups[2].Value;

                if (!name.Contains("Location") && !sourceCode.Contains($"public {type} {name}("))
                {
                    if (!type.Contains("Array") && !type.Contains("List"))
                    {
                        var mappedType = MapToSystemNumerics(type);
                        properties.Add((mappedType, name));
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

        private static string GenerateSystemCode(string systemName, string componentName, string shaderClassName, List<(string type, string name)> properties)
        {
            var sb = new StringBuilder();

            // Добавляем необходимые using
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Numerics;");
            sb.AppendLine("using AtomEngine;");
            sb.AppendLine("using OpenglLib;");
            sb.AppendLine("using EngineLib;");
            sb.AppendLine("using Silk.NET.Maths;");
            sb.AppendLine();

            // Открываем пространство имен
            sb.AppendLine("namespace AtomEngine");
            sb.AppendLine("{");

            // Объявление класса системы
            sb.AppendLine($"    public class {systemName} : IRenderSystem");
            sb.AppendLine("    {");

            // Свойство World
            sb.AppendLine("        public IWorld World { get; set; }");
            sb.AppendLine();

            // Запросы
            sb.AppendLine("        private QueryEntity queryRenderersEntity;");
            sb.AppendLine();

            // Конструктор
            sb.AppendLine($"        public {systemName}(IWorld world)");
            sb.AppendLine("        {");
            sb.AppendLine("            World = world;");
            sb.AppendLine("            queryRenderersEntity = this.CreateEntityQuery()");
            sb.AppendLine("                .With<TransformComponent>()");
            sb.AppendLine("                .With<MeshComponent>()");
            sb.AppendLine($"                .With<{componentName}>();");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Метод Render
            sb.AppendLine("        public void Render(double deltaTime)");
            sb.AppendLine("        {");
            sb.AppendLine("            Entity[] rendererEntities = queryRenderersEntity.Build();");
            sb.AppendLine("            foreach (var entity in rendererEntities)");
            sb.AppendLine("            {");
            sb.AppendLine("                ref var transform = ref this.GetComponent<TransformComponent>(entity);");
            sb.AppendLine("                ref var meshComponent = ref this.GetComponent<MeshComponent>(entity);");
            sb.AppendLine($"                ref var shaderComponent = ref this.GetComponent<{componentName}>(entity);");
            sb.AppendLine();
            sb.AppendLine("                if (meshComponent.Mesh == null || shaderComponent.Shader == null)");
            sb.AppendLine("                {");
            sb.AppendLine("                    continue;");
            sb.AppendLine("                }");
            sb.AppendLine();
            sb.AppendLine($"                {shaderClassName} shader = ({shaderClassName})shaderComponent.Shader;");
            sb.AppendLine("                shader.Use();");
            sb.AppendLine();

            foreach (var (type, name) in properties)
            {
                if (!type.Contains("Matrix"))
                {
                    if (type.StartsWith("Vector"))
                    {
                        sb.AppendLine($"                shader.{name} = shaderComponent.{name}.ToSilk();");
                    }
                    else
                    {
                        sb.AppendLine($"                shader.{name} = shaderComponent.{name};");
                    }
                }
            }

            sb.AppendLine();
            sb.AppendLine("                meshComponent.Mesh.Draw(shaderComponent.Shader);");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine();

            sb.AppendLine("        public void Resize(Vector2 size) { }");
            sb.AppendLine();
            sb.AppendLine("        public void Initialize() { }");
            sb.AppendLine("    }");

            sb.AppendLine("}");

            return sb.ToString();
        }
    }
}
