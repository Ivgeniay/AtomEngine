using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis; 
using System.Reflection; 
using System.Text;

namespace OpenglLib.Generator
{
    [Generator]
    public class MaterialGenerator : ISourceGenerator
    {
        private Dictionary<string, string> embeddedResources = new Dictionary<string, string>();

        public void Execute(GeneratorExecutionContext context)
        {
            Reporter.ReportMessage(context, "MG200", "Start Debug",
                "=== Starting material generation with embedded resources ===", DiagnosticSeverity.Warning);

            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames();
            foreach (var resourceName in resourceNames.Where(n => n.EndsWith(".glsl")))
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    var content = reader.ReadToEnd();
                    var includePath = resourceName
                        .Replace("OpenglLib.Shaders.", "")
                        .Replace(".", "/");

                    embeddedResources[includePath] = content;
                }
            }

            var shaderFiles = context.AdditionalFiles
                .Where(file => file.Path.EndsWith(".glsl"))
                .ToList();

            foreach (var file in shaderFiles)
            {
                var sourceText = file.GetText()?.ToString() ?? string.Empty;
                if (GeneratorHelper.IsCompleteShaderFile(sourceText))
                {
                    ProcessShaderFile(context, file.Path, sourceText);
                }
            }
        }

        private void ProcessShaderFile(GeneratorExecutionContext context, string filePath, string sourceText)
        {
            try
            {
                var materialName = Path.GetFileNameWithoutExtension(filePath);
                var (vertexSource, fragmentSource) = GeneratorHelper.ExtractShaderSources(context, sourceText);
                GeneratorHelper.ValidateMainFunctions(context, vertexSource, fragmentSource);
                var uniforms = ExtractUniforms(vertexSource + "\n" + fragmentSource);
                var uniform_blocks = GeneratorHelper.ParseUniformBlocks(vertexSource + "\n" + fragmentSource);
                var materialCode = GenerateMaterialClass(materialName, vertexSource, fragmentSource, uniforms, uniform_blocks);

                context.AddSource($"{materialName}Material.g.cs", SourceText.From(materialCode, Encoding.UTF8));
            }
            catch (Exception ex)
            {
                Reporter.ReportMessage(context, "MG013", "Generation Error",
                    $"Error processing {filePath}: {ex.Message}", DiagnosticSeverity.Error);
            }
        }

        private List<(string type, string name, int? arraySize)> ExtractUniforms(string source)
        {
            var uniforms = new List<(string type, string name, int? arraySize)>();
            var uniformRegex = new Regex(@"uniform\s+(?!layout)(\w+)\s+(\w+)(?:\[(\d+)\])?\s*;");

            foreach (Match match in uniformRegex.Matches(source))
            {
                var type = match.Groups[1].Value;
                var name = match.Groups[2].Value;
                int? arraySize = null;

                if (match.Groups[3].Success)
                {
                    arraySize = int.Parse(match.Groups[3].Value);
                }

                uniforms.Add((type, name, arraySize));
            }

            return uniforms;
        }


        private string GenerateMaterialClass(string materialName, string vertexSource,
            string fragmentSource, List<(string type, string name, int? arraySize)> uniforms, List<UniformBlockStructure> uniformBlocks)
        {
            var builder = new StringBuilder();

            var construcBuilder = new StringBuilder();
            List<string> constructor_lines = new List<string>();
            int samplers = 0;

            builder.AppendLine("using OpenglLib.Buffers;");
            builder.AppendLine("using Silk.NET.OpenGL;");
            builder.AppendLine("using Silk.NET.Maths;");
            builder.AppendLine("using AtomEngine;");
            builder.AppendLine();
            builder.AppendLine("namespace OpenglLib");
            builder.AppendLine("{");
            builder.AppendLine($"    public partial class {materialName}Material : Mat");
            builder.AppendLine("    {");
            builder.AppendLine($"        protected string VertexSource = @\"{vertexSource.Replace("\"", "\"\"")}\";");
            builder.AppendLine($"        protected string FragmentSource = @\"{fragmentSource.Replace("\"", "\"\"")}\";");
            // Конструктор
            builder.AppendLine("*construct*");
            construcBuilder.AppendLine($"        public {materialName}Material(GL gl) : base(gl)");
            construcBuilder.AppendLine("        {");
            
            builder.AppendLine("");

            // Uniform свойства
            foreach (var (type, name, arraySize) in uniforms)
            {
                string csharpType = MapGlslTypeToCSharp(type);
                bool isCustomType = GeneratorHelper.IsCustomType(csharpType, type);

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
                        builder.Append(GeneratorHelper.GetSimpleGetter(cashFieldName));
                        builder.AppendLine("        }");
                        constructor_lines.Add($"            {cashFieldName}  = new StructArray<{csharpType}>({arraySize.Value}, _gl);");
                    }
                    else
                    {
                        builder.AppendLine($"        private {csharpType} {cashFieldName};");
                        builder.AppendLine($"        public {csharpType} {name}");
                        builder.AppendLine("        {");
                        builder.Append(GeneratorHelper.GetSimpleGetter(cashFieldName));
                        builder.AppendLine("        }");
                        constructor_lines.Add($"            {cashFieldName} = new {csharpType}(_gl);");
                    }
                }
                else
                {
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
                        if (type.IndexOf("sampler") != -1)
                        {
                            //builder.AppendLine($"        [SamplerAtrribute(\"{name}\", \"Texture{samplers++}\", \"{GeneratorHelper.GetTextureTarget(type)}\")]");
                            builder.AppendLine($"        public void {name}_SetTexture(OpenglLib.Texture texture) => SetTexture(\"Texture{samplers}\", \"{GeneratorHelper.GetTextureTarget(type)}\", {locationName}, {samplers++}, texture);");
                        }
                        builder.AppendLine($"        public int {locationName} " + "{" + " get ; protected set; } = -1;");
                        builder.AppendLine($"        private {csharpType} {cashFieldName};");
                        builder.AppendLine($"        public {_unsafe}{csharpType} {name}"); 
                        builder.AppendLine("        {");
                        builder.Append(GeneratorHelper.GetSetter(type, locationName, cashFieldName));
                        builder.AppendLine("        }");
                    }

                }
                builder.AppendLine("");
                builder.AppendLine("");
            }

            foreach(var block in uniformBlocks)
            {
                if (block.InstanceName == null)
                    block.InstanceName = GetBlockDefaultName(block);

                if (block.Binding != null)
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
                foreach (var type in constructor_lines)
                {
                    construcBuilder.AppendLine(type);
                }
            }
            construcBuilder.AppendLine("            SetUpShader(VertexSource, FragmentSource);");
            construcBuilder.AppendLine("            SetupUniformLocations();");
            construcBuilder.AppendLine("        }");
            builder.Replace("*construct*", construcBuilder.ToString());

            return builder.ToString();
        }

        private string GetBlockDefaultName(UniformBlockStructure block)
        {
            if (block.Binding.HasValue) return $"UniformBlockBinding{block.Binding.Value}";
            return $"AnonymousBlock_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
        }
        private string MapGlslTypeToCSharp(string glslType) => GeneratorHelper.MapGlslTypeToCSharp(glslType); 

        public void Initialize(GeneratorInitializationContext context) { }
    }
}
