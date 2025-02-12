using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Text; 
using Microsoft.CodeAnalysis;
using System.Text;

namespace OpenglLib.Generator
{
    [Generator]
    public class UniformBlockGenerator : ISourceGenerator
    {
        private static GeneratorExecutionContext context_;
        public void Execute(GeneratorExecutionContext context)
        {
            context_ = context;
            var shaderFiles = context.AdditionalFiles
                .Where(file => file.Path.EndsWith(".glsl"))
                .ToList();

            foreach (var file in shaderFiles)
            {
                try
                {
                    var sourceText = file.GetText()?.ToString() ?? string.Empty;

                    if (!GeneratorHelper.IsCompleteShaderFile(sourceText))
                        continue;

                    var (vertexSource, fragmentSource) = GeneratorHelper.ExtractShaderSources(context, sourceText);
                    GeneratorHelper.ValidateMainFunctions(context, vertexSource, fragmentSource);

                    var uniformBlocks = new List<UniformBlockStructure>();
                    uniformBlocks.AddRange(GeneratorHelper.ParseUniformBlocks(vertexSource));
                    uniformBlocks.AddRange(GeneratorHelper.ParseUniformBlocks(fragmentSource));

                    var uniqueBlocks = uniformBlocks
                        .GroupBy(block => block.Name)
                        .Select(group => group.First())
                        .ToList();

                    foreach (var block in uniqueBlocks)
                    {
                        var splitedStr = file.Path.Split('\\');
                        var class_name = splitedStr[splitedStr.Length-1];
                        class_name = class_name.Replace(".glsl", "");
                        class_name = $"{block.Name}_{class_name}";
                        var blockCode = GenerateUniformBlockClass(block, class_name);
                        context.AddSource($"UBO.{class_name}.g.cs", SourceText.From(blockCode, Encoding.UTF8));
                    }
                }
                catch (Exception ex)
                {
                    Reporter.ReportMessage(context, "UB001", "Processing Error",
                        $"Error processing file {file.Path}: {ex.Message}",
                        DiagnosticSeverity.Error);
                }
            }
        }

        private string GenerateUniformBlockClass(UniformBlockStructure block, string className)
        {
            var builder = new StringBuilder();
            var construcBuilder = new StringBuilder();
            List<string> constructor_lines = new List<string>();

            builder.AppendLine("using System.Runtime.InteropServices;");
            builder.AppendLine("using Silk.NET.Maths;");
            builder.AppendLine();
            builder.AppendLine("namespace OpenglLib");
            builder.AppendLine("{");
            builder.AppendLine("    [StructLayout(LayoutKind.Sequential)]");
            //builder.AppendLine($"    public class {className} : Std140UniformBlock");
            builder.AppendLine($"    public struct {className}");
            builder.AppendLine("    {");
            //builder.AppendLine("        public string BlockName { get; private set; } = string.Empty;");
            //builder.AppendLine("        public int? BlockBinding { get; private set; } = null;");
            //builder.AppendLine("");
            //builder.AppendLine("*constructor");
            // Конструктор
            construcBuilder.AppendLine($"        public {className}()");
            construcBuilder.AppendLine("        {");
            construcBuilder.AppendLine($"            BlockName = \"{block.Name}\";");
            if (block.Binding.HasValue)
            {
                construcBuilder.AppendLine($"            BlockBinding = {block.Binding.Value};");
            }
            else
            {
                construcBuilder.AppendLine($"            BlockBinding = null;");
            }

            // Поля
            foreach (var (type, name, arraySize) in block.Fields)
            {
                var csharpType = GeneratorHelper.MapGlslTypeToCSharp(type);
                var isCastomType = GeneratorHelper.IsCustomType(csharpType, type);
                if (!isCastomType)
                {
                    if (arraySize.HasValue)
                    {
                        //builder.AppendLine($"        public {csharpType}[] {name} = new {csharpType}[{arraySize.Value}];");
                    }
                    else
                    {
                        builder.AppendLine($"        public {csharpType} {name};");
                    }
                }
            }

            builder.AppendLine("    }");
            builder.AppendLine("}");

            construcBuilder.AppendLine("        }");

            builder.Replace("*constructor", construcBuilder.ToString());

            return builder.ToString();
        }

        public void Initialize(GeneratorInitializationContext context) { }
    }
}
