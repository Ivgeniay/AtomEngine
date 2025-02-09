using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis; 
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;

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
                if (IsCompleteShaderFile(sourceText))
                {
                    ProcessShaderFile(context, file.Path, sourceText);
                }
            }
        }

        private bool IsCompleteShaderFile(string source)
        {
            return source.Contains("#vertex") || source.Contains("#fragment");
        }

        private void ProcessShaderFile(GeneratorExecutionContext context, string filePath, string sourceText)
        {
            try
            {
                var materialName = Path.GetFileNameWithoutExtension(filePath);
                var (vertexSource, fragmentSource) = ExtractShaderSources(context, sourceText);
                ValidateMainFunctions(context, vertexSource, fragmentSource);
                var uniforms = ExtractUniforms(vertexSource + "\n" + fragmentSource);
                var materialCode = GenerateMaterialClass(materialName, vertexSource, fragmentSource, uniforms);
                context.AddSource($"{materialName}Material.g.cs",
                    SourceText.From(materialCode, Encoding.UTF8));
            }
            catch (Exception ex)
            {
                Reporter.ReportMessage(context, "MG013", "Generation Error",
                    $"Error processing {filePath}: {ex.Message}", DiagnosticSeverity.Error);
            }
        }

        private (string vertex, string fragment) ExtractShaderSources(GeneratorExecutionContext context, string source)
        {
            string vertexSource = "";
            string fragmentSource = "";
            try
            {
                var vertexRegex = new Regex(@"#vertex\r?\n(.*?)(?=#fragment|$)", RegexOptions.Singleline);
                var fragmentRegex = new Regex(@"#fragment\r?\n(.*?)$", RegexOptions.Singleline);

                var vertexMatch = vertexRegex.Match(source);
                var fragmentMatch = fragmentRegex.Match(source);

                if (vertexMatch.Success)
                {
                    vertexSource = vertexMatch.Groups[1].Value.Trim();
                    vertexSource = ProcessIncludes(context, vertexSource);
                }

                if (fragmentMatch.Success)
                {
                    fragmentSource = fragmentMatch.Groups[1].Value.Trim();
                    fragmentSource = ProcessIncludes(context, fragmentSource);
                }
            }
            catch (Exception ex)
            {
                Reporter.ReportMessage(context, "MG304", "Extraction Error",
                    $"Error during source extraction: {ex.Message}", DiagnosticSeverity.Error);
                throw;
            }

            return (vertexSource, fragmentSource);
        }

        private string ProcessIncludes(GeneratorExecutionContext context, string source)
        {

            var includeRegex = new Regex(@"#include\s+""([^""]+)""");

            // Получаем все доступные файлы для логирования
            var allFiles = context.AdditionalFiles
                .Select(f => f.Path.Replace('\\', '/'))
                .ToList();

            try
            {
                return includeRegex.Replace(source, match =>
                {
                    var includePath = match.Groups[1].Value;
                    var foundFile = allFiles.FirstOrDefault(f =>
                        f.EndsWith(includePath, StringComparison.OrdinalIgnoreCase));

                    if (foundFile != null)
                    {
                        var includeFile = context.AdditionalFiles.First(f => f.Path.Replace('\\', '/') == foundFile);
                        var content = includeFile.GetText()?.ToString() ?? string.Empty;
                        return $"{content}";
                    }

                    Reporter.ReportMessage(context, "MG405", "Include Not Found",
                        $"File not found for include: {includePath}\nTried to find file ending with: {includePath}\nAvailable files:\n{string.Join("\n", allFiles)}",
                        DiagnosticSeverity.Error);
                    throw new Exception($"Include file not found: {includePath}");
                });
            }
            catch (Exception ex)
            {
                Reporter.ReportMessage(context, "MG406", "Include Processing Error",
                    $"Error processing includes: {ex.Message}\nStack trace: {ex.StackTrace}",
                    DiagnosticSeverity.Error);
                throw;
            }
        }

        private void ValidateMainFunctions(GeneratorExecutionContext context, string vertexSource, string fragmentSource)
        {
            var mainRegex = new Regex(@"void\s+main\s*\(\s*\)\s*{");

            var vertexMainCount = mainRegex.Matches(vertexSource).Count;
            var fragmentMainCount = mainRegex.Matches(fragmentSource).Count;

            if (vertexMainCount != 1)
            {
                throw new Exception($"Vertex shader must have exactly one main function. Found: {vertexMainCount}");
            }

            if (fragmentMainCount != 1)
            {
                throw new Exception($"Fragment shader must have exactly one main function. Found: {fragmentMainCount}");
            }
        }

        private List<(string type, string name)> ExtractUniforms(string source)
        {
            var uniforms = new List<(string type, string name)>();
            var uniformRegex = new Regex(@"uniform\s+(?!layout)(\w+)\s+(\w+)\s*;");

            foreach (Match match in uniformRegex.Matches(source))
            {
                var type = match.Groups[1].Value;
                var name = match.Groups[2].Value;
                uniforms.Add((type, name));
            }

            return uniforms;
        }

        private string GenerateMaterialClass(string materialName, string vertexSource,
            string fragmentSource, List<(string type, string name)> uniforms)
        {
            var builder = new StringBuilder();

            builder.AppendLine("using Silk.NET.OpenGL;");
            builder.AppendLine("using Silk.NET.Maths;");
            builder.AppendLine("using AtomEngine;");
            builder.AppendLine();
            builder.AppendLine("namespace OpenglLib");
            builder.AppendLine("{");
            builder.AppendLine($"    public class {materialName}Material : Mat");
            builder.AppendLine("    {");

            // Конструктор
            builder.AppendLine($"        protected string VertexSource = @\"{vertexSource.Replace("\"", "\"\"")}\";");
            builder.AppendLine($"        protected string FragmentSource = @\"{fragmentSource.Replace("\"", "\"\"")}\";");
            builder.AppendLine($"        public {materialName}Material(GL gl) : base(gl)");
            builder.AppendLine("        {");
            builder.AppendLine("            DebLogger.Info(\"Material constructor\");");
            builder.AppendLine("            SetUpShader(VertexSource, FragmentSource);");
            builder.AppendLine("            SetLocation();"); 
            builder.AppendLine("        }");
            builder.AppendLine("");

            // Uniform свойства
            foreach (var (type, name) in uniforms)
            {
                string csharpType = MapGlslTypeToCSharp(type);
                bool isCustomType = csharpType == type && type != "float" && type != "bool" && type != "int" && type != "uint" && type != "float" && type != "double";

                string cashFieldName = $"_{name}";
                string locationName = $"{name}Location";
                if (isCustomType)
                {
                    builder.AppendLine($"        private {csharpType} {cashFieldName} = new {csharpType}();");
                    builder.AppendLine($"        public {csharpType} {name}");
                    builder.AppendLine("        {");
                    builder.Append(ShaderTypes.GetGetter(cashFieldName));
                    builder.AppendLine("        }");
                }
                else
                {
                    builder.AppendLine($"        private {csharpType} {cashFieldName};");
                    builder.AppendLine($"        public {csharpType} {name}"); 
                    builder.AppendLine("        {");
                    builder.Append(ShaderTypes.GetSetter(type, locationName, cashFieldName));
                    builder.AppendLine("        }");
                    builder.AppendLine($"        public int {locationName} " + "{" + " get ; protected set; } = -1;");

                }
                builder.AppendLine("");
            }

            builder.AppendLine("    }");
            builder.AppendLine("}");

            return builder.ToString();
        }

        private string MapGlslTypeToCSharp(string glslType) => ShaderTypes.MapGlslTypeToCSharp(glslType); 

        public void Initialize(GeneratorInitializationContext context) { }
    }
}
