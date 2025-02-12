using System.Reflection;

namespace OpenglLib.Utils
{
    internal static class Loader
    {
        private const string BaseNamespace = "OpenglLib";

        public static Result<string, Error> LoadConfigurationFileAsText(string fileName, Assembly assembly = null)
        {
            if (assembly == null) assembly = Assembly.GetExecutingAssembly();
            var resources = assembly.GetManifestResourceNames();
            var normalizedShaderName = fileName.Replace('/', '.').Replace('\\', '.');
            var resourceName = resources.FirstOrDefault(r =>
                r.StartsWith(BaseNamespace) &&
                r.EndsWith(normalizedShaderName, StringComparison.OrdinalIgnoreCase));

            if (resourceName == null)
            {
                var availableShaders = string.Join("\n", resources
                    .Where(r => r.StartsWith(BaseNamespace))
                    .Select(r => r.Substring(BaseNamespace.Length + 1)));

                return new Result<string, Error>(new ShaderError(
                    $"Shader resource not found: {fileName}\n" +
                    $"Available shaders:\n{availableShaders}"));
            }

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new ShaderError($"Failed to load shader stream: {resourceName}");

            using var reader = new StreamReader(stream);
            return new Result<string, Error>(reader.ReadToEnd());
        }
    }
}