﻿using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis; 
using System.Text;
using System.Runtime.CompilerServices;

namespace OpenglLib.Generator
{
    [Generator]
    public class GlslStructGenerator : ISourceGenerator
    {
        private static readonly string[] ShaderExtensions = { ".glsl", ".vert", ".frag" };

        public void Execute(GeneratorExecutionContext context)
        {
            GeneratorHelper.GeneratedTypes = new();
            var processedStructures = GeneratorHelper.GeneratedTypes;
            var pendingStructures = new List<GlslStructure>();
            var generatedTypes = new HashSet<string>();

            var resourceFiles = context.AdditionalFiles
                .Where(file => ShaderExtensions.Contains(Path.GetExtension(file.Path)))
                .ToList();

            // Первый проход: собираем все структуры
            foreach (var file in resourceFiles)
            {
                var sourceText = file.GetText()?.ToString() ?? string.Empty;
                var structures = ParseGlslStructures(sourceText);
                pendingStructures.AddRange(structures);
            }

            while (pendingStructures.Count > 0)
            {
                bool processedAny = false;
                var remainingStructures = new List<GlslStructure>();

                foreach (var structure in pendingStructures)
                {
                    if (CanProcessStructure(structure, generatedTypes))
                    { 

                        if (!processedStructures.Add(structure.Name))
                        {
                            continue;
                        }

                        var modelCode = GenerateModelClass(structure, generatedTypes);
                        context.AddSource($"GlslStruct.{structure.Name}.g.cs", SourceText.From(modelCode, Encoding.UTF8));
                        generatedTypes.Add(structure.Name);
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
                    break;
                }

                pendingStructures = remainingStructures;
            }
        }

        private class GlslStructure
        {
            public string Name { get; set; }
            public List<(string Type, string Name, int? ArraySize)> Fields { get; set; }
        }

        private List<GlslStructure> ParseGlslStructures(string sourceCode)
        {
            var structures = new List<GlslStructure>();
            var structRegex = new Regex(@"struct\s+(\w+)\s*\{([^}]+)\}", RegexOptions.Multiline);
            var matches = structRegex.Matches(sourceCode);

            foreach (Match match in matches)
            {
                var structure = new GlslStructure
                {
                    Name = match.Groups[1].Value,
                    Fields = ParseFields(match.Groups[2].Value)
                };
                structures.Add(structure);
            }

            return structures;
        }

        private List<(string Type, string Name, int? ArraySize)> ParseFields(string fieldsText)
        {
            var fields = new List<(string Type, string Name, int? ArraySize)>();
             
            fieldsText = Regex.Replace(fieldsText, @"//.*$", "", RegexOptions.Multiline); 
            fieldsText = Regex.Replace(fieldsText, @"/\*[\s\S]*?\*/", ""); 
            var fieldRegex = new Regex(@"(?<type>\w+)\s+(?<name>\w+)(?:\[(?<size>\d+)\])?\s*;", RegexOptions.Multiline);

            var matches = fieldRegex.Matches(fieldsText);
            foreach (Match match in matches)
            {
                var type = match.Groups["type"].Value;
                var name = match.Groups["name"].Value;
                int? arraySize = null;

                if (match.Groups["size"].Success)
                {
                    arraySize = int.Parse(match.Groups["size"].Value);
                }

                fields.Add((type, name, arraySize));
            }

            return fields;
        }

        private string GenerateModelClass(GlslStructure structure, HashSet<string> generatedTypes)
        {
            var builder = new StringBuilder();
            var construcBuilder = new StringBuilder();
            List<string> constructor_lines = new List<string>();


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
                var csharpType = MapGlslTypeToCSharp(type, generatedTypes);
                bool isCustomType = GeneratorHelper.IsCustomType(csharpType, type);
                string cashFieldName = $"_{name}";
                string locationName = $"{name}Location";

                if (!isCustomType)
                {
                    var _unsafe = type.StartsWith("mat") ? "unsafe " : "";
                    if (arraySize.HasValue)
                    {
                        var localeProperty = GeneratorHelper.GetPropertyForLocaleArray(csharpType, name, locationName);
                        builder.Append(localeProperty);
                        builder.AppendLine($"        private LocaleArray<{csharpType}> {cashFieldName};");
                        builder.AppendLine($"        public LocaleArray<{csharpType}> {name}");
                        builder.AppendLine("        {");
                        builder.Append(GeneratorHelper.GetSimpleGetter(cashFieldName));
                        builder.AppendLine("        }");
                        constructor_lines.Add($"            {cashFieldName}  = new LocaleArray<{csharpType}>({arraySize.Value}, _gl);");

                    }
                    else
                    {
                        builder.AppendLine($"        public int {locationName} " + "{" + " get ; set; } = -1;");
                        builder.AppendLine($"        private {csharpType} {cashFieldName};");
                        builder.AppendLine($"        public {_unsafe}{csharpType} {name}");
                        builder.AppendLine("        {");
                        builder.Append(GeneratorHelper.GetSetter(type, locationName, cashFieldName));
                        //builder.Append(ShaderTypes.GetSimpleGetter(cashFieldName));
                        builder.AppendLine("        }");
                    }
                }
                else
                {
                    var _unsafe = type.StartsWith("mat") ? "unsafe " : "";
                    if (arraySize.HasValue)
                    {
                        builder.AppendLine($"        private StructArray<{csharpType}> {cashFieldName};");
                        builder.AppendLine($"        public StructArray<{csharpType}> {name}");
                        builder.AppendLine("        {");
                        builder.Append(GeneratorHelper.GetSimpleGetter(cashFieldName));
                        builder.AppendLine("        }");
                        constructor_lines.Add($"            {cashFieldName}  = new StructArray<{csharpType}>({arraySize.Value}, _gl);");
                    }
                    else
                    {
                        builder.AppendLine($"        private {csharpType} {cashFieldName};");
                        builder.AppendLine($"        public {_unsafe}{csharpType} {name}");
                        builder.AppendLine("        {");
                        builder.Append(GeneratorHelper.GetSimpleGetter(cashFieldName));
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
                foreach (var type in constructor_lines)
                {
                    construcBuilder.AppendLine(type);
                }
            }
            construcBuilder.AppendLine("        }");
            builder.Replace("*construct*", construcBuilder.ToString());

            return builder.ToString();
        }

        private string MapGlslTypeToCSharp(string glslType, HashSet<string> generatedTypes) =>
            GeneratorHelper.MapGlslTypeToCSharp(glslType, generatedTypes);

        private bool IsGlslBaseType(string type)
        {
            // Список базовых GLSL типов (без кастомных структур)
            return type switch
            {
                // Скалярные типы
                "bool" or "int" or "uint" or "float" or "double" or
                // Векторные типы
                "bvec2" or "bvec3" or "bvec4" or
                "ivec2" or "ivec3" or "ivec4" or
                "uvec2" or "uvec3" or "uvec4" or
                "vec2" or "vec3" or "vec4" or
                "dvec2" or "dvec3" or "dvec4" or
                // Матричные типы
                "mat2" or "mat3" or "mat4" or
                "mat2x2" or "mat2x3" or "mat2x4" or
                "mat3x2" or "mat3x3" or "mat3x4" or
                "mat4x2" or "mat4x3" or "mat4x4" or
                "dmat2" or "dmat3" or "dmat4" or
                "dmat2x2" or "dmat2x3" or "dmat2x4" or
                "dmat3x2" or "dmat3x3" or "dmat3x4" or
                "dmat4x2" or "dmat4x3" or "dmat4x4" => true,
                _ => false
            };
        }
        private bool CanProcessStructure(GlslStructure structure, HashSet<string> generatedTypes)
        {
            foreach (var (type, _, _) in structure.Fields)
            {
                // Если это не базовый GLSL тип и тип еще не был сгенерирован
                if (!IsGlslBaseType(type) && !generatedTypes.Contains(type))
                {
                    return false;
                }
            }
            return true;
        }
        public void Initialize(GeneratorInitializationContext context) { }
    }
}
