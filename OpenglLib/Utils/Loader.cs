
using System.Reflection;

namespace OpenglLib.Utils
{
    internal static class Loader
    {
        private const string BaseNamespace = "OpenglLib";

        public static Result<string, Error> LoadConfigurationFileAsText(string fileName, Assembly assembly = null)
        {
            assembly = assembly ?? typeof(Loader).Assembly;
            Result<Stream, Error> mb_stream = GetConfigurationFile(fileName, assembly);

            using (var stream = mb_stream.Unwrap())
            {
                if (stream == null)
                    return new Result<string, Error>(new FileNotFoundError($"Resource not found: {fileName}"));

                string result = "";
                using (var reader = new StreamReader(stream))
                {
                    result = reader.ReadToEnd();
                }
                return new Result<string, Error>(result);
            }
        }

        public static Result<string, Error> LoadAsText(string filePath, string extension)
        {
            if (!File.Exists(filePath))
                return new Result<string, Error>(new FileNotFoundError($"File not found: {filePath}"));

            if (!filePath.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                return new Result<string, Error>(new ArgumentError($"File must have {extension} extension"));

            return new Result<string, Error>(File.ReadAllText(filePath));

        }

        public static Result<string, Error> FindFile(string fileName)
        {
            string rootDirectory = GetRootDirectory();
            Result<string, Error> mb_filePath = FindFileRecursively(rootDirectory, fileName);
            string filepath = mb_filePath.Expect($"File not found: {fileName}");
            return new Result<string, Error>(filepath);
        }

        public static string LoadFromResourcesAsText(string shaderName)
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


        private static Result<string, Error> FindFileRecursively(string currentDirectory, string fileName)
        {
            string filePath = Path.Combine(currentDirectory, fileName);
            if (File.Exists(filePath))
                return new Result<string, Error>(filePath);

            try
            {
                foreach (string dir in Directory.GetDirectories(currentDirectory))
                {
                    Result<string, Error> mb_found = FindFileRecursively(dir, fileName);
                    if (mb_found.IsOk())
                    {
                        return mb_found;
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                return new Result<string, Error>(new UnauthorizedAccessError($"There is no access to directory: {currentDirectory}"));
            }

            return new Result<string, Error>(new FileNotFoundError($"File {fileName} not found"));
        }

        private static string GetRootDirectory()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        private static Result<Stream,Error> GetConfigurationFile(string fileName, Assembly assembly)
        {
            string resourcePath = $"{assembly.GetName().Name}.Config.{fileName}";
            Stream? stream = assembly.GetManifestResourceStream(resourcePath);
            if (stream == null)
                return new Result<Stream, Error>(new FileNotFoundError($"Resource not found: {fileName}"));
            return new Result<Stream, Error>(stream);
        }
    }
}