using System.Collections.Generic;
using System.IO;
using System;

namespace Editor.Utils.Generator
{
    internal static class GlslCodeGenerator
    {
        private static Dictionary<string, string> _includedFiles = new Dictionary<string, string>();

        /// <summary>
        /// Добавляет файл, который может быть включен через директиву #include
        /// </summary>
        /// <param name="includePath">Относительный путь к файлу (используется в директиве #include)</param>
        /// <param name="content">Содержимое файла</param>
        public static void AddIncludeFile(string includePath, string content)
        {
            _includedFiles[includePath] = content;
        }

        /// <summary>
        /// Добавляет файлы из указанной директории в список доступных для включения
        /// </summary>
        /// <param name="directory">Директория с файлами для включения</param>
        /// <param name="searchPattern">Шаблон поиска файлов</param>
        public static void AddIncludeFilesFromDirectory(string directory, string searchPattern = "*.glsl")
        {
            foreach (var file in Directory.GetFiles(directory, searchPattern, SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(directory, file).Replace('\\', '/');
                var content = File.ReadAllText(file);
                AddIncludeFile(relativePath, content);
            }
        }

        /// <summary>
        /// Очищает список файлов, доступных для включения
        /// </summary>
        public static void ClearIncludeFiles()
        {
            _includedFiles.Clear();
        }

        /// <summary>
        /// Генерирует код на основе файла шейдера
        /// </summary>
        /// <param name="glslFilePath">Путь к файлу шейдера</param>
        /// <param name="outputDirectory">Директория для сохранения сгенерированных файлов</param>
        /// <param name="generateStructs">Генерировать ли структуры</param>
        /// <returns>Имя сгенерированного представления</returns>
        public static string GenerateCode(string glslFilePath, string outputDirectory, bool generateStructs = true)
        {
            if (!File.Exists(glslFilePath))
            {
                throw new FileNotFoundException($"Shader file not found: {glslFilePath}");
            }

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            string shaderSource = File.ReadAllText(glslFilePath);

            if (!GlslParser.IsCompleteShaderFile(shaderSource))
            {
                throw new Exception($"The file {glslFilePath} is not a complete shader file (must contain #vertex and #fragment sections).");
            }

            string sourceGuid = MetadataManager.Instance.GetMetadata(glslFilePath)?.Guid;

            var representationName = Path.GetFileNameWithoutExtension(glslFilePath);
            var (vertexSource, fragmentSource) = GlslParser.ExtractShaderSources(shaderSource, _includedFiles);
            GlslParser.ValidateMainFunctions(vertexSource, fragmentSource);
            var combinedSource = vertexSource + "\n" + fragmentSource;

            if (generateStructs)
            {
                List<GlslStructure> structures = GlslParser.ParseGlslStructures(combinedSource);
                if (structures.Count > 0)
                {
                    GlslStructGenerator.GenerateStructs(
                        shaderSourceCode: combinedSource, 
                        outputDirectory: outputDirectory, 
                        sourceGuid: sourceGuid);
                }
            }

            List<UniformBlockStructure> uniformBlocks = GlslParser.ParseUniformBlocks(combinedSource);
            foreach (var block in uniformBlocks)
            {
                var blockClassName = $"{block.Name}_{representationName}";
                ShaderCodeRepresentationGenerator.GenerateUniformBlockClass(
                    block: block, 
                    className: blockClassName, 
                    outputDirectory: outputDirectory, 
                    representationName: representationName, 
                    sourceGuid: sourceGuid);
            }

            string resultRepresentationName = ShaderCodeRepresentationGenerator.GenerateRepresentationFromSource(
                representationName: representationName, 
                sourceText: shaderSource, 
                outputDirectory: outputDirectory, 
                includedFiles: _includedFiles, 
                sourceGuid: sourceGuid, 
                sourcePath: glslFilePath);

            return resultRepresentationName;
        }

        /// <summary>
        /// Генерирует код на основе всех шейдеров в указанной директории
        /// </summary>
        /// <param name="directoryPath">Путь к директории с шейдерами</param>
        /// <param name="outputDirectory">Директория для сохранения сгенерированных файлов</param>
        /// <param name="searchPattern">Шаблон поиска файлов шейдеров</param>
        /// <param name="generateStructs">Генерировать ли структуры</param>
        /// <returns>Список имен сгенерированных материалов</returns>
        public static List<string> GenerateCodeFromDirectory(string directoryPath, string outputDirectory,
            string searchPattern = "*.glsl", bool generateStructs = true)
        {
            if (!Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
            }

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var generatedMaterials = new List<string>();
            var shaderFiles = Directory.GetFiles(directoryPath, searchPattern, SearchOption.TopDirectoryOnly);

            foreach (var shaderFile in shaderFiles)
            {
                try
                {
                    var shaderSource = File.ReadAllText(shaderFile);
                    if (GlslParser.IsCompleteShaderFile(shaderSource))
                    {
                        var materialName = GenerateCode(shaderFile, outputDirectory, generateStructs);
                        generatedMaterials.Add(materialName);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing {shaderFile}: {ex.Message}");
                }
            }

            return generatedMaterials;
        }
    }
}
