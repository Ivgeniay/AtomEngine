using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;


namespace Generator
{
    [Generator]
    public class ShaderFieldsGenerator : ISourceGenerator
    {
        private StringBuilder sourceBuilder = new StringBuilder();
        private Dictionary<string, string> allFields = new Dictionary<string, string>();

        private void ReportMessage(GeneratorExecutionContext context, string id, string title, string message, DiagnosticSeverity severity)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    id,
                    title,
                    message,
                    "ShaderGeneration",
                    severity,  // Error, Warning или Info
                    isEnabledByDefault: true),
                Location.None));
        }

        public void Execute(GeneratorExecutionContext context)
        {
            ReportMessage(context, "SG100", "Generator Start", "Starting shader field generation", DiagnosticSeverity.Warning);

            // Выведем список всех доступных файлов
            var allFiles = context.AdditionalFiles.Select(f => f.Path).ToList();
            ReportMessage(context, "SG101", "Available Files",
                $"Found {allFiles.Count} additional files: {string.Join(", ", allFiles)}",
                DiagnosticSeverity.Warning);

            var configFile = context.AdditionalFiles
                .FirstOrDefault(f => f.Path.EndsWith("ShaderConfig.json"));

            if (configFile == null)
            {
                ReportMessage(context, "SG001", "Config Not Found",
                    $"ShaderConfig.json not found in available files",
                    DiagnosticSeverity.Error);
                return;
            }

            ReportMessage(context, "SG102", "Config Found",
                $"Found config at: {configFile.Path}",
                DiagnosticSeverity.Warning);

            var content = configFile.GetText(context.CancellationToken)?.ToString();
            if (content == null)
            {
                ReportMessage(context, "SG002", "Empty Configuration",
                    "ShaderConfig.json file is empty or could not be read",
                    DiagnosticSeverity.Error);
                return;
            }

            ReportMessage(context, "SG103", "Content Read",
                $"Read content length: {content.Length}",
                DiagnosticSeverity.Warning);

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
                context.AddSource("ShaderFields.g.cs", SourceText.From(generatedCode, Encoding.UTF8));

                // Добавим отладочное сообщение
                ReportMessage(context, "SG999", "Generation Success",
                    $"Generated {fields.Count} fields", DiagnosticSeverity.Info);
            }
            catch (Exception ex)
            {
                ReportMessage(context, "SG002", "Generation Error",
                    $"Error during generation: {ex.Message}", DiagnosticSeverity.Error);
            }
        }

        

        public void Initialize(GeneratorInitializationContext context) { }
    }

    public class SimpleJsonParser
    {
        private Dictionary<string, string> _fields = new Dictionary<string, string>();
        private int _position;
        private string _json;

        public Dictionary<string, string> Parse(string json)
        {
            _json = json;
            _position = 0;
            _fields.Clear();
            ParseObject();
            return _fields;
        }

        private void SkipWhitespace()
        {
            while (_position < _json.Length && char.IsWhiteSpace(_json[_position]))
                _position++;
        }

        private void ParseObject()
        {
            SkipWhitespace();
            if (_position >= _json.Length || _json[_position] != '{')
                return;

            _position++; // Skip {

            while (_position < _json.Length)
            {
                SkipWhitespace();
                if (_json[_position] == '}')
                {
                    _position++;
                    break;
                }

                if (_json[_position] == ',')
                {
                    _position++;
                    continue;
                }

                ParseKeyValuePair();
            }
        }

        private void ParseKeyValuePair()
        {
            SkipWhitespace();
            string key = ParseString();

            SkipWhitespace();
            if (_position >= _json.Length || _json[_position] != ':')
                return;
            _position++; // Skip :

            SkipWhitespace();
            if (_position >= _json.Length)
                return;

            if (_json[_position] == '{')
            {
                ParseObject(); // Рекурсивно обрабатываем вложенный объект
            }
            else if (_json[_position] == '"')
            {
                string value = ParseString();
                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                {
                    _fields[key] = value;
                }
            }
        }

        private string ParseString()
        {
            if (_position >= _json.Length || _json[_position] != '"')
                return string.Empty;

            _position++; // Skip opening quote
            int start = _position;
            while (_position < _json.Length && _json[_position] != '"')
                _position++;

            if (_position >= _json.Length)
                return string.Empty;

            string result = _json.Substring(start, _position - start);
            _position++; // Skip closing quote
            return result;
        }
    }
}
