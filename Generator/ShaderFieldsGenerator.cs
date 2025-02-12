using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;


namespace OpenglLib.Generator
{
    [Generator]
    public class ShaderFieldsGenerator : ISourceGenerator
    {
        private StringBuilder sourceBuilder = new StringBuilder();
        public void Execute(GeneratorExecutionContext context)
        {
            var allFiles = context.AdditionalFiles.Select(f => f.Path).ToList();

            var configFile = context.AdditionalFiles
                .FirstOrDefault(f => f.Path.EndsWith("ShaderConfig.json"));

            if (configFile == null)
            {
                return;
            }


            var content = configFile.GetText(context.CancellationToken)?.ToString();
            if (content == null)
            {
                return;
            }

            GenerateCode(context, content);
        }

        private void GenerateCode(GeneratorExecutionContext context, string jsonContent)
        {
            try
            {
                var parser = new SimpleJsonParser();
                var fields = parser.Parse(jsonContent);

                sourceBuilder.Clear();
                sourceBuilder.AppendLine("namespace OpenglLib {");
                sourceBuilder.AppendLine("    public partial class ShaderFields {");

                foreach (var field in fields)
                {
                    sourceBuilder.AppendLine($"        public string {field.Key} => base[\"{field.Key}\"]; // {field.Value}");
                }

                sourceBuilder.AppendLine();
                sourceBuilder.AppendLine("        public ShaderFields() {");
                foreach (var field in fields)
                {
                    sourceBuilder.AppendLine($"            base[\"{field.Key}\"] = \"{field.Value}\";");
                }
                sourceBuilder.AppendLine("        }");

                sourceBuilder.AppendLine("    }");
                sourceBuilder.AppendLine("}");

                var generatedCode = sourceBuilder.ToString();
                context.AddSource("Generated.ShaderFields.g.cs", SourceText.From(generatedCode, Encoding.UTF8));
            }
            catch (Exception ex)
            {
                Reporter.ReportMessage(context, "SG002", "Generation Error",
                    $"Error during generation: {ex.Message}", DiagnosticSeverity.Error);
            }
        }

        public void Initialize(GeneratorInitializationContext context) { }
    }

    
}
