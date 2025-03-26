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

            // Получаем информацию о матрицах из шейдера
            var matrixInfo = ShaderMatrixInfo.FromSourceCode(sourceCode);

            string systemCode = GenerateSystemCode(systemName, componentName, className, properties, matrixInfo);

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
            // Ищем объявление класса типа: public class SomeNameRepresentation : Mat
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

            foreach (Match match in matches)
            {
                string type = match.Groups[1].Value;
                string name = match.Groups[2].Value;

                if (!name.Contains("Location") && !sourceCode.Contains($"public {type} {name}("))
                {
                    if (!type.Contains("Array") && !type.Contains("List"))
                    {
                        bool isTexture = ShaderCodeAnalyzer.IsTextureType(type);
                        var mappedType = MapToSystemNumerics(type);
                        properties.Add((mappedType, name, isTexture));
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

        private static string GenerateSystemCode(
            string systemName,
            string componentName,
            string shaderClassName,
            List<(string type, string name, bool isTexture)> properties,
            ShaderMatrixInfo matrixInfo = null)
        {
            var sb = new StringBuilder();

            if (matrixInfo == null)
            {
                matrixInfo = new ShaderMatrixInfo();
            }

            sb.AppendLine("using System;");
            sb.AppendLine("using System.Numerics;");
            sb.AppendLine("using AtomEngine;");
            sb.AppendLine("using OpenglLib;");
            sb.AppendLine("using EngineLib;");
            sb.AppendLine("using Silk.NET.Maths;");
            sb.AppendLine();

            sb.AppendLine("namespace AtomEngine");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {systemName} : IRenderSystem");
            sb.AppendLine("    {");
            sb.AppendLine("        public IWorld World { get; set; }");
            sb.AppendLine();
            sb.AppendLine("        private QueryEntity queryRenderersEntity;");
            if (matrixInfo.NeedsCamera)
            {
                sb.AppendLine("        private QueryEntity queryCameraEntity;");
            }
            sb.AppendLine();

            sb.AppendLine($"        public {systemName}(IWorld world)");
            sb.AppendLine("        {");
            sb.AppendLine("            World = world;");

            if (matrixInfo.NeedsCamera)
            {
                sb.AppendLine("            queryCameraEntity = this.CreateEntityQuery()");
                sb.AppendLine("                .With<TransformComponent>()");
                sb.AppendLine("                .With<CameraComponent>();");
            }

            sb.AppendLine("            queryRenderersEntity = this.CreateEntityQuery()");
            sb.AppendLine("                .With<TransformComponent>()");
            sb.AppendLine("                .With<MeshComponent>()");
            sb.AppendLine($"                .With<{componentName}>();");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        public void Render(double deltaTime)");
            sb.AppendLine("        {");
            if (matrixInfo.NeedsCamera)
            {
                sb.AppendLine("            Entity[] cameras = queryCameraEntity.Build();");
                sb.AppendLine("            if (cameras.Length == 0)");
                sb.AppendLine("            {");
                sb.AppendLine("                return;");
                sb.AppendLine("            }");
                sb.AppendLine();
                sb.AppendLine("            Entity activeCamera = Entity.Null;");
                sb.AppendLine("            foreach (var camera in cameras)");
                sb.AppendLine("            {");
                sb.AppendLine("                ref var cameraComponent = ref this.GetComponent<CameraComponent>(camera);");
                sb.AppendLine("                if (cameraComponent.IsActive)");
                sb.AppendLine("                {");
                sb.AppendLine("                    activeCamera = camera;");
                sb.AppendLine("                    break;");
                sb.AppendLine("                }");
                sb.AppendLine("            }");
                sb.AppendLine();
                sb.AppendLine("            if (activeCamera == Entity.Null)");
                sb.AppendLine("            {");
                sb.AppendLine("                activeCamera = cameras[0];");
                sb.AppendLine("            }");
                sb.AppendLine();
                sb.AppendLine("            ref var cameraTransform = ref this.GetComponent<TransformComponent>(activeCamera);");
                sb.AppendLine("            ref var activeCameraComponent = ref this.GetComponent<CameraComponent>(activeCamera);");
                sb.AppendLine();

                if (matrixInfo.UsesViewMatrix || matrixInfo.UsesViewProjectionMatrix || matrixInfo.UsesModelViewProjectionMatrix)
                {
                    sb.AppendLine("            var viewMatrix = activeCameraComponent.ViewMatrix;");
                }

                if (matrixInfo.UsesProjectionMatrix || matrixInfo.UsesViewProjectionMatrix || matrixInfo.UsesModelViewProjectionMatrix)
                {
                    sb.AppendLine("            var projectionMatrix = activeCameraComponent.CreateProjectionMatrix();");
                }

                if (matrixInfo.UsesViewProjectionMatrix)
                {
                    sb.AppendLine("            var viewProjectionMatrix = viewMatrix * projectionMatrix;");
                }

                sb.AppendLine();
            }

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
            sb.AppendLine("                shaderComponent.Shader.Use();");
            sb.AppendLine();

            if (matrixInfo.UsesModelMatrix && !string.IsNullOrEmpty(matrixInfo.ModelMatrixName))
            {
                sb.AppendLine($"                shaderComponent.Shader.{matrixInfo.ModelMatrixName} = transform.GetModelMatrix().ToSilk();");
            }

            if (matrixInfo.NeedsCamera)
            {
                if (matrixInfo.UsesViewMatrix && !string.IsNullOrEmpty(matrixInfo.ViewMatrixName))
                {
                    sb.AppendLine($"                shaderComponent.Shader.{matrixInfo.ViewMatrixName} = viewMatrix.ToSilk();");
                }

                if (matrixInfo.UsesProjectionMatrix && !string.IsNullOrEmpty(matrixInfo.ProjectionMatrixName))
                {
                    sb.AppendLine($"                shaderComponent.Shader.{matrixInfo.ProjectionMatrixName} = projectionMatrix.ToSilk();");
                }

                if (matrixInfo.UsesViewProjectionMatrix && !string.IsNullOrEmpty(matrixInfo.ViewProjectionMatrixName))
                {
                    sb.AppendLine($"                shaderComponent.Shader.{matrixInfo.ViewProjectionMatrixName} = viewProjectionMatrix.ToSilk();");
                }

                if (matrixInfo.UsesModelViewProjectionMatrix && !string.IsNullOrEmpty(matrixInfo.ModelViewProjectionMatrixName))
                {
                    sb.AppendLine($"                var modelMatrix = transform.GetModelMatrix();");
                    sb.AppendLine($"                shaderComponent.Shader.{matrixInfo.ModelViewProjectionMatrixName} = (modelMatrix * viewMatrix * projectionMatrix).ToSilk();");
                }
            }
            foreach (var (type, name, isTexture) in properties)
            {
                if (type.Contains("Matrix") ||
                    (matrixInfo.UsesModelMatrix && name == matrixInfo.ModelMatrixName) ||
                    (matrixInfo.UsesViewMatrix && name == matrixInfo.ViewMatrixName) ||
                    (matrixInfo.UsesProjectionMatrix && name == matrixInfo.ProjectionMatrixName) ||
                    (matrixInfo.UsesViewProjectionMatrix && name == matrixInfo.ViewProjectionMatrixName) ||
                    (matrixInfo.UsesModelViewProjectionMatrix && name == matrixInfo.ModelViewProjectionMatrixName))
                {
                    continue;
                }

                if (isTexture)
                {
                    sb.AppendLine($"                if (shaderComponent.{name}Texture != null)");
                    sb.AppendLine($"                {{");
                    sb.AppendLine($"                    shaderComponent.Shader.{name}_SetTexture(shaderComponent.{name}Texture);");
                    sb.AppendLine($"                }}");
                }
                else
                {
                    if (type.StartsWith("Vector"))
                    {
                        sb.AppendLine($"                shaderComponent.Shader.{name} = shaderComponent.{name}.ToSilk();");
                    }
                    else
                    {
                        sb.AppendLine($"                shaderComponent.Shader.{name} = shaderComponent.{name};");
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
