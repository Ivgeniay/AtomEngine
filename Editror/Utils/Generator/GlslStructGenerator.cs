﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System;

namespace Editor
{
    internal static class GlslStructGenerator
    {
        private static HashSet<string> _generatedTypes = new HashSet<string>();

        /// <summary>
        /// Генерирует код структур C# из GLSL структур, найденных в шейдерном коде
        /// </summary>
        /// <param name="shaderSourceCode">Исходный код шейдера</param>
        /// <param name="outputDirectory">Директория для сохранения файлов генерируемого кода</param>
        /// <param name="sourceGuid">GUID исходного файла шейдера</param>
        /// <returns>Список названий сгенерированных типов</returns>
        public static List<string> GenerateStructs(string shaderSourceCode, string outputDirectory, string sourceGuid = null)
        {
            _generatedTypes = new HashSet<string>();
            var pendingStructures = new List<GlslStructure>();
            var result = new List<string>();

            var structures = GlslParser.ParseGlslStructures(shaderSourceCode);
            pendingStructures.AddRange(structures);

            while (pendingStructures.Count > 0)
            {
                bool processedAny = false;
                var remainingStructures = new List<GlslStructure>();

                foreach (var structure in pendingStructures)
                {
                    if (CanProcessStructure(structure, _generatedTypes))
                    {
                        if (!_generatedTypes.Add(structure.Name))
                        {
                            continue;
                        }

                        var modelCode = GenerateModelClass(structure, sourceGuid);
                        var filePath = Path.Combine(outputDirectory, $"GlslStruct.{structure.Name}.g.cs");
                        File.WriteAllText(filePath, modelCode, Encoding.UTF8);

                        result.Add(structure.Name);
                        processedAny = true;
                    }
                    else
                    {
                        remainingStructures.Add(structure);
                    }
                }

                if (!processedAny && remainingStructures.Count > 0)
                {
                    var circularDeps = string.Join(", ", remainingStructures.Select(s => s.Name));
                    throw new Exception($"Circular dependencies detected between structures: {circularDeps}");
                }

                pendingStructures = remainingStructures;
            }

            return result;
        }

        /// <summary>
        /// Проверяет, может ли структура быть обработана (все ее зависимости уже сгенерированы)
        /// </summary>
        private static bool CanProcessStructure(GlslStructure structure, HashSet<string> generatedTypes)
        {
            foreach (var (type, _, _) in structure.Fields)
            {
                // Если это не базовый GLSL тип и тип еще не был сгенерирован
                if (!GlslParser.IsGlslBaseType(type) && !generatedTypes.Contains(type))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Генерирует код модельного класса для структуры GLSL
        /// </summary>
        private static string GenerateModelClass(GlslStructure structure, string sourceGuid)
        {
            var builder = new StringBuilder();
            var construcBuilder = new StringBuilder();
            List<string> constructor_lines = new List<string>();

            // Добавляем комментарии для авто-генерированного кода
            WriteGeneratedCodeHeader(builder, sourceGuid);

            builder.AppendLine("using Silk.NET.Maths;");
            builder.AppendLine("using Silk.NET.OpenGL;");
            builder.AppendLine("using AtomEngine;");
            builder.AppendLine();
            builder.AppendLine("namespace OpenglLib");
            builder.AppendLine("{");
            builder.AppendLine("    //");
            builder.AppendLine($"    public class {structure.Name} : CustomStruct");
            builder.AppendLine("    {");
            builder.AppendLine("*construct*");
            builder.AppendLine($"");

            construcBuilder.AppendLine($"        public {structure.Name}(Silk.NET.OpenGL.GL gl) : base(gl) {{");

            foreach (var (type, name, arraySize) in structure.Fields)
            {
                var csharpType = GlslParser.MapGlslTypeToCSharp(type, _generatedTypes);
                bool isCustomType = GlslParser.IsCustomType(csharpType, type);
                string cashFieldName = $"_{name}";
                string locationName = $"{name}Location";

                if (!isCustomType)
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
                        builder.AppendLine($"        public int {locationName} " + "{" + " get ; set; } = -1;");
                        builder.AppendLine($"        private {csharpType} {cashFieldName};");
                        builder.AppendLine($"        public {csharpType} {name}");
                        builder.AppendLine("        {");
                        builder.Append(GetSetter(type, locationName, cashFieldName));
                        builder.AppendLine("        }");
                    }
                }
                else
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
                builder.AppendLine("");
                builder.AppendLine("");
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
            builder.AppendLine($"                   DebLogger.Warn(\"You try to set value to -1 lcation field\");");
            builder.AppendLine($"                   return;");
            builder.AppendLine($"                }}");
            builder.AppendLine($"                {cashFieldName} = value;");

            switch (type)
            {
                // Скалярные типы
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

                // Векторные типы bool
                case "bvec2":
                    builder.AppendLine($"                _gl.Uniform2({locationFieldName}, value.X ? 1 : 0, value.Y ? 1 : 0);");
                    break;
                case "bvec3":
                    builder.AppendLine($"                _gl.Uniform3({locationFieldName}, value.X ? 1 : 0, value.Y ? 1 : 0, value.Z ? 1 : 0);");
                    break;
                case "bvec4":
                    builder.AppendLine($"                _gl.Uniform4({locationFieldName}, value.X ? 1 : 0, value.Y ? 1 : 0, value.Z ? 1 : 0, value.W ? 1 : 0);");
                    break;

                // Векторные типы int
                case "ivec2":
                    builder.AppendLine($"                _gl.Uniform2({locationFieldName}, value.X, value.Y);");
                    break;
                case "ivec3":
                    builder.AppendLine($"                _gl.Uniform3({locationFieldName}, value.X, value.Y, value.Z);");
                    break;
                case "ivec4":
                    builder.AppendLine($"                _gl.Uniform4({locationFieldName}, value.X, value.Y, value.Z, value.W);");
                    break;

                // Векторные типы uint
                case "uvec2":
                    builder.AppendLine($"                _gl.Uniform2({locationFieldName}, value.X, value.Y);");
                    break;
                case "uvec3":
                    builder.AppendLine($"                _gl.Uniform3({locationFieldName}, value.X, value.Y, value.Z);");
                    break;
                case "uvec4":
                    builder.AppendLine($"                _gl.Uniform4({locationFieldName}, value.X, value.Y, value.Z, value.W);");
                    break;

                // Векторные типы float
                case "vec2":
                    builder.AppendLine($"                _gl.Uniform2({locationFieldName}, value.X, value.Y);");
                    break;
                case "vec3":
                    builder.AppendLine($"                _gl.Uniform3({locationFieldName}, value.X, value.Y, value.Z);");
                    break;
                case "vec4":
                    builder.AppendLine($"                _gl.Uniform4({locationFieldName}, value.X, value.Y, value.Z, value.W);");
                    break;

                // Матричные типы float
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

                // Sampler типы
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