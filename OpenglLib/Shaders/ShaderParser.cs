using System.Text.RegularExpressions;

namespace OpenglLib
{
    public class ShaderParser
    {
        public static string ProcessIncludes(string source, string shaderName, HashSet<string>? processedFiles = null)
        {
            processedFiles ??= new HashSet<string> { shaderName };
            string[] lines = source.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            List<string> result = new List<string>();
            var includeRegex = new Regex(@"^\s*#\s*include\s+""([^""]+)""\s*$");

            foreach (string line in lines)
            {
                var match = includeRegex.Match(line);
                if (match.Success)
                {
                    string includeFileName = match.Groups[1].Value;
                    if (processedFiles.Contains(includeFileName))
                    {
                        throw new CyclicDependenceError(
                            $"Cyclic dependency detected while processing shader '{shaderName}'. " +
                            $"Inclusion chain: {string.Join(" -> ", processedFiles)} -> {includeFileName}");
                    }

                    try
                    {
                        string includeContent = ShaderLoader.LoadShader(includeFileName);
                        processedFiles.Add(includeFileName);
                        string processedContent = ProcessIncludes(includeContent, includeFileName, processedFiles);
                        result.Add($"// Begin included file: {includeFileName}");
                        result.AddRange(processedContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None));
                        result.Add($"// End included file: {includeFileName}");
                        processedFiles.Remove(includeFileName);
                    }
                    catch (ShaderError e)
                    {
                        throw new ShaderError($"Error processing include in shader '{shaderName}': {e.Message}");
                    }
                }
                else
                {
                    result.Add(line);
                }
            }

            return string.Join(Environment.NewLine, result);
        }

        public static string ProcessConstants(string source)
        { 
            var shaderFields = new ShaderFields(); 
            var result = source; 
            var literals = new Dictionary<string, string>();
            var literalCounter = 0; 
            result = Regex.Replace(result, @"""[^""\\]*(?:\\.[^""\\]*)*""", match =>
            {
                var placeholder = $"__STRING_LITERAL_{literalCounter}__";
                literals[placeholder] = match.Value;
                literalCounter++;
                return placeholder;
            });
             
            result = Regex.Replace(result, @"//[^\n]*", match =>
            {
                var placeholder = $"__LINE_COMMENT_{literalCounter}__";
                literals[placeholder] = match.Value;
                literalCounter++;
                return placeholder;
            });
             
            result = Regex.Replace(result, @"/\*[\s\S]*?\*/", match =>
            {
                var placeholder = $"__BLOCK_COMMENT_{literalCounter}__";
                literals[placeholder] = match.Value;
                literalCounter++;
                return placeholder;
            }); 
            var wordRegex = new Regex(@"\b[a-zA-Z_]\w*\b");
            result = wordRegex.Replace(result, match =>
            {
                string word = match.Value;
                return shaderFields.TryGetValue(word, out string? replacement)
                    ? replacement
                    : word;
            });

            // Восстанавливаем строковые литералы и комментарии
            foreach (var literal in literals)
            {
                result = result.Replace(literal.Key, literal.Value);
            }

            return result;
        }

        internal static List<StructDefinition> ParseStructs(string source)
        {
            // Создаем список для хранения всех найденных структур
            var structDefinitions = new List<StructDefinition>();

            // Это регулярное выражение ищет определения структур в GLSL.
            // struct Name { ... };
            // Группы захвата:
            // 1 - имя структуры
            // 2 - содержимое структуры (все поля)
            var structRegex = new Regex(@"struct\s+(\w+)\s*\{([^}]+)\}\s*;",
                RegexOptions.Multiline | RegexOptions.Singleline);

            // Находим все определения структур в исходном коде
            var matches = structRegex.Matches(source);

            foreach (Match match in matches)
            {
                // Извлекаем имя структуры из первой группы захвата
                string structName = match.Groups[1].Value;

                // Создаем новый экземпляр структуры
                var structDef = new StructDefinition(structName);

                // Получаем содержимое структуры (все её поля)
                string structContent = match.Groups[2].Value;

                // Разбираем поля структуры и добавляем их в определение
                ParseStructFields(structContent, structDef);

                // Добавляем готовое определение структуры в список
                structDefinitions.Add(structDef);
            }

            return structDefinitions;
        }

        private static void ParseStructFields(string structContent, StructDefinition structDef)
        {
            // Это регулярное выражение разбирает отдельные поля структуры.
            // Например: "vec3 position;" или "DirectionalLight lights[2];"
            // Группы захвата:
            // 1 - тип поля (например, vec3, float, DirectionalLight)
            // 2 - имя поля
            // 3 - размер массива (опционально)
            var fieldRegex = new Regex(@"\s*(\w+(?:\s+\w+)*)\s+(\w+)(?:\[(\d+)\])?\s*;");
            var matches = fieldRegex.Matches(structContent);

            foreach (Match match in matches)
            {
                string fieldType = match.Groups[1].Value.Trim();
                string fieldName = match.Groups[2].Value;

                // Определяем размер массива, если поле является массивом
                int arraySize = 0;
                if (match.Groups[3].Success)
                {
                    arraySize = int.Parse(match.Groups[3].Value);
                }

                // Создаем новое определение поля и добавляем его в структуру
                var fieldDef = new StructFieldDefinition(fieldName, fieldType, arraySize);
                structDef.Fields.Add(fieldDef);
            }
        }
    }
}
