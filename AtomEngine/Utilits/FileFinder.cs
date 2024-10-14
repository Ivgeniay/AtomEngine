namespace AtomEngine.Utilits
{
    internal static class FileFinder
    {
        public static string? FindAndReadFile(string name, string type)
        {
            try
            {
                string fileName = $"{name}.{type}";
                string projectRoot = GetProjectRoot();
                string? filePath = FindFileInDirectory(projectRoot, fileName);

                return filePath != null ? File.ReadAllText(filePath) : null;
            }
            catch
            {
                return null;
            }
        }

        private static string? FindFileInDirectory(string rootDirectory, string fileName)
        {
            return Directory.EnumerateFiles(rootDirectory, fileName, SearchOption.AllDirectories)
                            .FirstOrDefault();
        }

        /// <summary>
        /// ЗАЛЕПУХА ПЕРЕДЕЛАТЬ
        /// </summary>
        /// <returns></returns>
        /// <exception cref="DirectoryNotFoundException"></exception>
        private static string GetProjectRoot()
        {
            string? currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            while (currentDirectory != null)
            {
                if (currentDirectory.EndsWith("Engine")) return currentDirectory;
                currentDirectory = Directory.GetParent(currentDirectory)?.FullName;
            }
            throw new DirectoryNotFoundException("Could not find project root directory.");
        }
    }
}
