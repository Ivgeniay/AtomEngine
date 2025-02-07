using System.Reflection;
using AtomEngine;

namespace OpenglLib
{
    public static class ShaderLoader
    {
        private const string BaseNamespace = "OpenglLib.Shaders.ShaderSource";
        public static string _customBasePath = AppContext.BaseDirectory; 

        /// <summary>
        /// Загружает шейдер из ресурсов или файла
        /// </summary>
        /// <param name="shaderName"> Имя файла или часть пути относительно Shader/ShaderSource </param>
        /// <param name="useEmbeddedResources"> Использование ресурсов (tree) или </param>
        /// <returns></returns>
        public static string LoadShader(string shaderName, bool useEmbeddedResources = true)
        {
            if (useEmbeddedResources)
            {
                return LoadFromResources(shaderName);
            }
            return LoadFromFile(shaderName);
        }

        private static string LoadFromResources(string shaderName)
        {
            var assembly = Assembly.GetExecutingAssembly(); 
            var resources = assembly.GetManifestResourceNames();
            var normalizedShaderName = shaderName.Replace('/', '.').Replace('\\', '.');
            var resourceName = resources.FirstOrDefault(r =>
                r.StartsWith(BaseNamespace) &&
                r.EndsWith(normalizedShaderName, StringComparison.OrdinalIgnoreCase));

            if (resourceName == null)
            {
                var availableShaders = string.Join("\n", resources
                    .Where(r => r.StartsWith(BaseNamespace))
                    .Select(r => r.Substring(BaseNamespace.Length + 1)));

                throw new ShaderError(
                    $"Shader resource not found: {shaderName}\n" +
                    $"Available shaders:\n{availableShaders}");
            }

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new ShaderError($"Failed to load shader stream: {resourceName}");

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        
        private static string LoadFromFile(string shaderName)
        {
            // Нормализуем имя шейдера, заменяя все возможные разделители на системный
            var normalizedShaderName = NormalizePath(shaderName);
            var basePath = _customBasePath;

            // Сначала пробуем найти файл по полному пути
            var fullPath = Path.Combine(basePath, normalizedShaderName);

            if (!File.Exists(fullPath))
            {
                // Если файл не найден, ищем все файлы с таким именем
                var fileName = Path.GetFileName(normalizedShaderName);
                var searchResults = Directory
                    .GetFiles(basePath, fileName, SearchOption.AllDirectories)
                    .Select(path => NormalizePath(Path.GetRelativePath(basePath, path)))
                    .ToList();

                if (!searchResults.Any())
                {
                    var availableShaders = Directory
                        .GetFiles(basePath, "*.glsl", SearchOption.AllDirectories)
                        .Select(path => NormalizePath(Path.GetRelativePath(basePath, path)));

                    throw new ShaderError(
                        $"Shader file not found: {shaderName}\n" +
                        $"Searched in: {basePath}\n" +
                        $"Available shaders:\n{string.Join("\n", availableShaders)}");
                }

                // Если найдено больше одного файла, проверяем на точное совпадение пути
                if (searchResults.Count > 1)
                {
                    var normalizedSearchPath = NormalizePath(normalizedShaderName);
                    var exactMatch = searchResults
                        .FirstOrDefault(path =>
                            string.Equals(path, normalizedSearchPath, StringComparison.OrdinalIgnoreCase));

                    if (exactMatch != null)
                    {
                        // Нашли точное совпадение по относительному пути
                        fullPath = Path.Combine(basePath, exactMatch);
                    }
                    else
                    {
                        // Если файл с таким именем существует в нескольких местах и нет точного совпадения пути
                        throw new ShaderError(
                            $"Ambiguous shader name: {shaderName}\n" +
                            $"Multiple matches found:\n{string.Join("\n", searchResults)}");
                    }
                }
                else
                {
                    // Если найден только один файл, используем его
                    fullPath = Path.Combine(basePath, searchResults[0]);
                }
            }

            return File.ReadAllText(fullPath);
        }

        private static string NormalizePath(string path)
        {
            string extension = Path.GetExtension(path);
            string pathWithoutExtension = path.Substring(0, path.Length - extension.Length);

            string normalizedPath = pathWithoutExtension
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('.', Path.DirectorySeparatorChar)
                .TrimStart(Path.DirectorySeparatorChar);

            return normalizedPath + extension;
        }
    }
}
