using System.Collections.Generic;
using System.Text;
using System.IO;
using System;

namespace Editor
{
    internal static class ShaderCodeRepresentationGenerator
    {
        /// <summary>
        /// Генерирует класс материала на основе шейдерного кода
        /// </summary>
        /// <param name="filePath">Путь к файлу шейдера</param>
        /// <param name="outputDirectory">Директория для сохранения файлов генерируемого кода</param>
        /// <param name="includedFiles">Словарь с включаемыми файлами для обработки директив #include</param>
        /// <returns>Имя сгенерированного класса материала</returns>
        public static void GenerateRepresentation(string filePath, string outputDirectory, Dictionary<string, string> includedFiles = null)
        {
            try
            {
                var sourceText = File.ReadAllText(filePath);
                string sourceGuid = ServiceHub.Get<MetadataManager>().GetMetadata(filePath)?.Guid;

                GenerateRepresentationFromSource(Path.GetFileNameWithoutExtension(filePath), sourceText, outputDirectory, includedFiles, sourceGuid, filePath);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error processing {filePath}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Генерирует класс материала на основе шейдерного кода
        /// </summary>
        /// <param name="representationName">Имя генерируемого материала</param>
        /// <param name="sourceText">Исходный код шейдера</param>
        /// <param name="outputDirectory">Директория для сохранения файлов генерируемого кода</param>
        /// <param name="includedFiles">Словарь с включаемыми файлами для обработки директив #include</param>
        /// <param name="sourceGuid">GUID исходного файла шейдера</param>
        /// <param name="sourcePath">Путь к исходному файлу шейдера</param>
        /// <returns>Имя сгенерированного класса представления</returns>
        public static string GenerateRepresentationFromSource(string representationName, string sourceText, string outputDirectory,
            Dictionary<string, string> includedFiles = null, string sourceGuid = null, string sourcePath = null)
        {
            if (!GlslParser.IsCompleteShaderFile(sourceText))
            {
                throw new Exception("The shader file does not contain both vertex and fragment shaders.");
            }

            if (string.IsNullOrEmpty(sourceGuid) && !string.IsNullOrEmpty(sourcePath))
                sourceGuid = ServiceHub.Get<MetadataManager>().GetMetadata(sourcePath)?.Guid;

            var (vertexSource, fragmentSource) = GlslParser.ExtractShaderSources(sourceText, includedFiles);
            GlslParser.ValidateMainFunctions(vertexSource, fragmentSource);

            var uniforms = GlslParser.ExtractUniforms(vertexSource + "\n" + fragmentSource);
            var uniformBlocks = GlslParser.ParseUniformBlocks(vertexSource + "\n" + fragmentSource);

            var representationCode = GenerateRepresentationClass(representationName, vertexSource, fragmentSource, uniforms, uniformBlocks, sourceGuid);
            var representationFilePath = Path.Combine(outputDirectory, $"{representationName}Representation.g.cs");
            File.WriteAllText(representationFilePath, representationCode, Encoding.UTF8);

            return $"{representationName}Representation";
        }

        /// <summary>
        /// Генерирует класс для uniform блока
        /// </summary>
        public static void GenerateUniformBlockClass(UniformBlockStructure block, string className, string outputDirectory, string representationName, string sourceGuid)
        {
            var builder = new StringBuilder();

            WriteGeneratedCodeHeader(builder, sourceGuid);

            builder.AppendLine("using System.Runtime.InteropServices;");
            builder.AppendLine("using Silk.NET.Maths;");
            builder.AppendLine();
            builder.AppendLine("namespace OpenglLib");
            builder.AppendLine("{");
            builder.AppendLine("    [StructLayout(LayoutKind.Sequential)]");
            builder.AppendLine($"    public struct {className}");
            builder.AppendLine("    {");

            foreach (var (type, name, arraySize) in block.Fields)
            {
                var csharpType = GlslParser.MapGlslTypeToCSharp(type);
                var isCastomType = GlslParser.IsCustomType(csharpType, type);
                if (!isCastomType)
                {
                    if (arraySize.HasValue)
                    {
                        builder.AppendLine($"        [MarshalAs(UnmanagedType.ByValArray, SizeConst = {arraySize.Value})]");
                        builder.AppendLine($"        public {csharpType}[] {name};");
                    }
                    else
                    {
                        builder.AppendLine($"        public {csharpType} {name};");
                    }
                }
            }

            builder.AppendLine("    }");
            builder.AppendLine("}");

            string blockClassName = $"{block.Name}_{representationName}";
            string blockFilePath = Path.Combine(outputDirectory, $"UBO.{blockClassName}.g.cs");
            string blockCode = builder.ToString();

            File.WriteAllText(blockFilePath, blockCode, Encoding.UTF8);
        }

        /// <summary>
        /// Генерирует класс представления
        /// </summary>
        private static string GenerateRepresentationClass(string materialName, string vertexSource,
            string fragmentSource, List<(string type, string name, int? arraySize)> uniforms,
            List<UniformBlockStructure> uniformBlocks, string sourceGuid)
        {
            var builder = new StringBuilder();
            var construcBuilder = new StringBuilder();
            List<string> constructor_lines = new List<string>();
            int samplers = 0;

            WriteGeneratedCodeHeader(builder, sourceGuid);

            builder.AppendLine("using OpenglLib.Buffers;");
            builder.AppendLine("using Silk.NET.OpenGL;");
            builder.AppendLine("using Silk.NET.Maths;");
            builder.AppendLine("using AtomEngine;");
            builder.AppendLine();
            builder.AppendLine("namespace OpenglLib");
            builder.AppendLine("{");
            builder.AppendLine($"    public partial class {materialName}Representation : Mat");
            builder.AppendLine("    {");
            builder.AppendLine($"        protected new string VertexSource = @\"{vertexSource.Replace("\"", "\"\"")}\";");
            builder.AppendLine($"        protected new string FragmentSource = @\"{fragmentSource.Replace("\"", "\"\"")}\";");

            builder.AppendLine("*construct*");
            construcBuilder.AppendLine($"        public {materialName}Representation(GL gl) : base(gl)");
            construcBuilder.AppendLine("        {");

            builder.AppendLine("");

            foreach (var (type, name, arraySize) in uniforms)
            {
                string csharpType = GlslParser.MapGlslTypeToCSharp(type);
                bool isCustomType = GlslParser.IsCustomType(csharpType, type);

                string cashFieldName = $"_{name}";
                string locationName = $"{name}Location";
                var _unsafe = type.StartsWith("mat") ? "unsafe " : "";

                if (isCustomType)
                {
                    if (arraySize.HasValue)
                    {
                        builder.AppendLine($"        private StructArray<{csharpType}> {cashFieldName};");
                        builder.AppendLine($"        public StructArray<{csharpType}> {name}");
                        builder.AppendLine("        {");
                        builder.Append(GetSimpleGetter(cashFieldName));
                        builder.AppendLine("        }");
                        constructor_lines.Add($"            {cashFieldName}  = new StructArray<{csharpType}>({arraySize.Value}, _gl);");
                    }
                    else
                    {
                        builder.AppendLine($"        private {csharpType} {cashFieldName};");
                        builder.AppendLine($"        public {csharpType} {name}");
                        builder.AppendLine("        {");
                        builder.Append(GetSimpleGetter(cashFieldName));
                        builder.AppendLine("        }");
                        constructor_lines.Add($"            {cashFieldName} = new {csharpType}(_gl);");
                    }
                }
                else
                {
                    if (arraySize.HasValue)
                    {
                        var localeProperty = GetPropertyForLocaleArray(csharpType, name, locationName);
                        builder.Append(localeProperty);
                        builder.AppendLine($"        private LocaleArray<{csharpType}> {cashFieldName};");
                        builder.AppendLine($"        public LocaleArray<{csharpType}> {name}");
                        builder.AppendLine("        {");
                        builder.Append(GetSimpleGetter(cashFieldName));
                        builder.AppendLine("        }");
                        constructor_lines.Add($"            {cashFieldName}  = new LocaleArray<{csharpType}>({arraySize.Value}, _gl);");
                    }
                    else
                    {
                        if (type.IndexOf("sampler") != -1)
                        {
                            builder.AppendLine($"        public void {name}_SetTexture(OpenglLib.Texture texture) => SetTexture(\"Texture{samplers}\", \"{GlslParser.GetTextureTarget(type)}\", {locationName}, {samplers++}, texture);");
                        }
                        builder.AppendLine($"        public int {locationName} " + "{" + " get ; protected set; } = -1;");
                        builder.AppendLine($"        private {csharpType} {cashFieldName};");
                        builder.AppendLine($"        public {_unsafe}{csharpType} {name}");
                        builder.AppendLine("        {");
                        builder.Append(GetSetter(type, locationName, cashFieldName));
                        builder.AppendLine("        }");
                    }
                }
                builder.AppendLine("");
                builder.AppendLine("");
            }

            foreach (var block in uniformBlocks)
            {
                if (block.InstanceName != null && block.Binding != null)
                {
                    string refStruct = $"_{block.InstanceName}";
                    builder.AppendLine($"        private UniformBufferObject<{block.Name}_{materialName}> {block.InstanceName}Ubo;");
                    construcBuilder.AppendLine($"            {block.InstanceName}Ubo = new UniformBufferObject<{block.Name}_{materialName}>(_gl, ref {refStruct}, {block.Binding.Value});");

                    builder.AppendLine($"        private {block.Name}_{materialName} {refStruct} = new {block.Name}_{materialName}();");
                    builder.AppendLine($"        public {block.Name}_{materialName} {block.InstanceName}");
                    builder.AppendLine("        {");
                    builder.AppendLine("            set");
                    builder.AppendLine("            {");
                    builder.AppendLine($"                {refStruct} = value;");
                    builder.AppendLine($"                {block.InstanceName}Ubo.UpdateData(ref {refStruct});");
                    builder.AppendLine("            }");
                    builder.AppendLine("        }");

                    builder.AppendLine("");
                    builder.AppendLine("");
                }
            }

            builder.AppendLine("    }");
            builder.AppendLine("}");

            if (constructor_lines.Count > 0)
            {
                foreach (var line in constructor_lines)
                {
                    construcBuilder.AppendLine(line);
                }
            }
            construcBuilder.AppendLine("            SetUpShader(VertexSource, FragmentSource);");
            construcBuilder.AppendLine("            SetupUniformLocations();");
            construcBuilder.AppendLine("        }");
            builder.Replace("*construct*", construcBuilder.ToString());

            return builder.ToString();
        }

        /// <summary>
        /// Добавляет комментарии с информацией о генерации кода
        /// </summary>
        private static void WriteGeneratedCodeHeader(StringBuilder builder, string sourceGuid)
        {
            builder.AppendLine("// <auto-generated>");
            builder.AppendLine("// This code was generated. Dont change this code.");
            builder.AppendLine($"// SourceGuid: {sourceGuid ?? "Unknown"}");
            builder.AppendLine($"// GeneratedAt: {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")} UTC");
            builder.AppendLine("// </auto-generated>");
            builder.AppendLine();
        }

        #region Helper Methods

        private static string GetSimpleGetter(string cashFieldName)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("            get");
            builder.AppendLine("            {");
            builder.AppendLine($"                return {cashFieldName};");
            builder.AppendLine("            }");
            return builder.ToString();
        }

        private static string GetPropertyForLocaleArray(string type, string fieldName, string locationFieldName)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine($"        public int {locationFieldName}");
            builder.AppendLine($"        {{");
            builder.AppendLine($"             get => {fieldName}.Location;");
            builder.AppendLine($"             set => {fieldName}.Location = value;");
            builder.AppendLine($"        }}");

            return builder.ToString();
        }

        private static string GetSetter(string type, string locationFieldName, string cashFieldName)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("            get");
            builder.AppendLine("            {");
            builder.AppendLine($"                return {cashFieldName};");
            builder.AppendLine("            }");
            builder.AppendLine("            set");
            builder.AppendLine("            {");
            builder.AppendLine($"                if ({locationFieldName} == -1)");
            builder.AppendLine($"                {{");
            builder.AppendLine($"                   return;");
            builder.AppendLine($"                }}");
            builder.AppendLine($"                {cashFieldName} = value;");

            switch (type)
            {
                case "bool":
                    builder.AppendLine($"                _gl.Uniform1({locationFieldName}, value ? 1 : 0);");
                    break;
                case "int":
                    builder.AppendLine($"                _gl.Uniform1({locationFieldName}, value);");
                    break;
                case "uint":
                    builder.AppendLine($"                _gl.Uniform1({locationFieldName}, value);");
                    break;
                case "float":
                    builder.AppendLine($"                _gl.Uniform1({locationFieldName}, value);");
                    break;
                case "double":
                    builder.AppendLine($"                _gl.Uniform1({locationFieldName}, value);");
                    break;

                case "bvec2":
                    builder.AppendLine($"                _gl.Uniform2({locationFieldName}, value.X ? 1 : 0, value.Y ? 1 : 0);");
                    break;
                case "bvec3":
                    builder.AppendLine($"                _gl.Uniform3({locationFieldName}, value.X ? 1 : 0, value.Y ? 1 : 0, value.Z ? 1 : 0);");
                    break;
                case "bvec4":
                    builder.AppendLine($"                _gl.Uniform4({locationFieldName}, value.X ? 1 : 0, value.Y ? 1 : 0, value.Z ? 1 : 0, value.W ? 1 : 0);");
                    break;

                case "ivec2":
                    builder.AppendLine($"                _gl.Uniform2({locationFieldName}, value.X, value.Y);");
                    break;
                case "ivec3":
                    builder.AppendLine($"                _gl.Uniform3({locationFieldName}, value.X, value.Y, value.Z);");
                    break;
                case "ivec4":
                    builder.AppendLine($"                _gl.Uniform4({locationFieldName}, value.X, value.Y, value.Z, value.W);");
                    break;

                case "uvec2":
                    builder.AppendLine($"                _gl.Uniform2({locationFieldName}, value.X, value.Y);");
                    break;
                case "uvec3":
                    builder.AppendLine($"                _gl.Uniform3({locationFieldName}, value.X, value.Y, value.Z);");
                    break;
                case "uvec4":
                    builder.AppendLine($"                _gl.Uniform4({locationFieldName}, value.X, value.Y, value.Z, value.W);");
                    break;

                case "vec2":
                    builder.AppendLine($"                _gl.Uniform2({locationFieldName}, value.X, value.Y);");
                    break;
                case "vec3":
                    builder.AppendLine($"                _gl.Uniform3({locationFieldName}, value.X, value.Y, value.Z);");
                    break;
                case "vec4":
                    builder.AppendLine($"                _gl.Uniform4({locationFieldName}, value.X, value.Y, value.Z, value.W);");
                    break;

                case "mat2":
                case "mat2x2":
                    builder.AppendLine($"                var mat2 = (Matrix2X2<float>)value;");
                    builder.AppendLine($"                _gl.UniformMatrix2({locationFieldName}, 1, false, (float*)&mat2);");
                    break;

                case "mat2x3":
                    builder.AppendLine($"                var mat2x3 = (Matrix2X3<float>)value;");
                    builder.AppendLine($"                _gl.UniformMatrix2x3({locationFieldName}, 1, false, (float*)&mat2x3);");
                    break;

                case "mat2x4":
                    builder.AppendLine($"                var mat2x4 = (Matrix2X4<float>)value;");
                    builder.AppendLine($"                _gl.UniformMatrix2x4({locationFieldName}, 1, false, (float*)&mat2x4);");
                    break;

                case "mat3":
                case "mat3x3":
                    builder.AppendLine($"                var mat3 = (Matrix3X3<float>)value;");
                    builder.AppendLine($"                _gl.UniformMatrix3({locationFieldName}, 1, false, (float*)&mat3);");
                    break;

                case "mat3x2":
                    builder.AppendLine($"                var mat3x2 = (Matrix3X2<float>)value;");
                    builder.AppendLine($"                _gl.UniformMatrix3x2({locationFieldName}, 1, false, (float*)&mat3x2);");
                    break;

                case "mat3x4":
                    builder.AppendLine($"                var mat3x4 = (Matrix3X4<float>)value;");
                    builder.AppendLine($"                _gl.UniformMatrix3x4({locationFieldName}, 1, false, (float*)&mat3x4);");
                    break;

                case "mat4":
                case "mat4x4":
                    builder.AppendLine($"                var mat4 = (Matrix4X4<float>)value;");
                    builder.AppendLine($"                _gl.UniformMatrix4({locationFieldName}, 1, false, (float*)&mat4);");
                    break;

                case "mat4x2":
                    builder.AppendLine($"                var mat4x2 = (Matrix4X2<float>)value;");
                    builder.AppendLine($"                _gl.UniformMatrix4x2({locationFieldName}, 1, false, (float*)&mat4x2);");
                    break;

                case "mat4x3":
                    builder.AppendLine($"                var mat4x3 = (Matrix4X3<float>)value;");
                    builder.AppendLine($"                _gl.UniformMatrix4x3({locationFieldName}, 1, false, (float*)&mat4x3);");
                    break;

                case string s when s.StartsWith("sampler"):
                    builder.AppendLine($"                _gl.Uniform1({locationFieldName}, value);");
                    break;
                case string s when s.StartsWith("isampler"):
                    builder.AppendLine($"                _gl.Uniform1i({locationFieldName}, value);");
                    break;
                case string s when s.StartsWith("usampler"):
                    builder.AppendLine($"                _gl.Uniform1ui({locationFieldName}, value);");
                    break;

                default:
                    builder.AppendLine($"                throw new NotSupportedException(\"Unsupported uniform type: {type}\");");
                    break;
            }

            builder.AppendLine("            }");
            return builder.ToString();
        }

        #endregion
    }
}