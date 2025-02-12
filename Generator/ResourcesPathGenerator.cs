using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;
using System.Text;

namespace OpenglLib.Generator
{
    [Generator]
    public class ResourcesPathGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            Reporter.ReportMessage(context, "MG200", "Start Debug",
                $"=== Starting ResourcesPathGenerator generation with embedded resources witn ===", DiagnosticSeverity.Warning);

            ProcessShaderFile(context);
        }

        private void ProcessShaderFile(GeneratorExecutionContext context)
        {
            try
            {
                List<Resourse> resourses = GettingResourses(context);
                string materialCode = GenerateResourceClass(resourses);
                context.AddSource($"PathStorage.g.cs", SourceText.From(materialCode, Encoding.UTF8));
            }
            catch (Exception ex)
            {
                Reporter.ReportMessage(context, "MG013", "Generation Error",
                    $"Error processing: {ex.Message}", DiagnosticSeverity.Error);
            }
        }
        private List<Resourse> GettingResourses(GeneratorExecutionContext context)
        {
            List<Resourse> resourses = new();

            var resourcesFiles = context.AdditionalFiles.ToList();
            string assemblyName = "OpenglLib\\";
            foreach (var file in resourcesFiles)
            {
                int index = file.Path.IndexOf(assemblyName) + assemblyName.Length;
                var path = file.Path.Substring(index);
                var domain = path.Replace('/', '.').Replace('\\', '.').Replace("\\\\", ".");
                var t = domain.Split('.');
                var name = t[t.Length - 2];
                var exten = t[t.Length - 1];

                Resourse resourse = new()
                {
                    Name = name,
                    Path = path,
                    Domain = domain,
                    Extension = exten,
                };

                resourses.Add(resourse);
                Reporter.ReportMessage(context, "MG201", "Start Debug", $"Processing file {resourse}", DiagnosticSeverity.Warning);
            }

            return resourses;
        }
        private string GenerateResourceClass(List<Resourse> resourses)
        {
            Dictionary<string, int> replays = new Dictionary<string, int>();
            var builder = new StringBuilder();

            builder.AppendLine("using AtomEngine;");
            builder.AppendLine();
            builder.AppendLine("namespace OpenglLib");
            builder.AppendLine("{");
            builder.AppendLine($"    public static class PathStorage");
            builder.AppendLine("    {");

            foreach (var resourse in resourses)
            {
                var name = GeneratorHelper.ConvertToValidCSharpIdentifier(resourse.Name.ToUpper());
                if (!string.IsNullOrEmpty(resourse.Extension)) name += "_" + resourse.Extension.ToUpper();

                if (replays.TryGetValue(name, out int raplayCount))
                {
                    replays[name]++;
                    name += "_" + raplayCount;
                }
                else
                {
                    replays.Add(name, 1);
                }

                builder.AppendLine($"        public const string {name} = \"{resourse.Domain}\";");
            } 
            builder.AppendLine("    }");
            builder.AppendLine("}");

            return builder.ToString();
        }


        private class Resourse
        {
            public string Name { get; set; } = string.Empty;
            public string Extension { get; set; } = string.Empty;
            public string Path { get; set; } = string.Empty;
            public string Domain { get;set; } = string.Empty;

            public override string ToString()
            {
                return "Name: " + Name + " Path: " + Path + " Domain: " + Domain;
            }
        }

        public void Initialize(GeneratorInitializationContext context) {}
    }
}
