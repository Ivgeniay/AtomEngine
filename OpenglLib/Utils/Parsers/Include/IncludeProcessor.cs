using System.Text.RegularExpressions;
using System.Reflection;
using EngineLib;
using AtomEngine;
using System.Text;

namespace OpenglLib
{
    public static class IncludeProcessor
    {
        private static readonly Regex IncludeRegex = new Regex(@"#include\s+""([^""]+)""");
        private static readonly Regex VersionRegex = new Regex(@"#version\s+\d+\s+\w*");

        public static void RegisterContentProvider(IContentProvider provider)
        {
            FileLoader.RegisterContentProvider(provider);
        }

        // Базовые методы ProcessIncludes остаются без изменений
        public static string ProcessIncludes(string source, string sourcePath, HashSet<string> processedPaths)
        {
            return ProcessIncludesInternal(source, sourcePath, processedPaths, null);
        }

        public static string ProcessIncludes(string source, string sourcePath, HashSet<string> processedPaths, out List<RSFileInfo> rsFiles)
        {
            rsFiles = new List<RSFileInfo>();
            return ProcessIncludesInternal(source, sourcePath, processedPaths, rsFiles);
        }

        public static string ProcessIncludes(string source, string sourcePath, HashSet<string> processedPaths, bool collectRsFiles, out List<RSFileInfo> rsFiles)
        {
            rsFiles = collectRsFiles ? new List<RSFileInfo>() : null;
            return ProcessIncludesInternal(source, sourcePath, processedPaths, rsFiles);
        }

        private static string ProcessIncludesInternal(string source, string sourcePath, HashSet<string> processedPaths, List<RSFileInfo> rsFiles)
        {
            processedPaths ??= new HashSet<string>();

            if (processedPaths.Contains(sourcePath))
                throw new CircularDependencyError($"Cyclic dependency detected: {sourcePath}");

            processedPaths.Add(sourcePath);

            return IncludeRegex.Replace(source, match => {
                var includePath = match.Groups[1].Value;
                string fullPath;
                string includeContent;

                try
                {
                    fullPath = FileLoader.ResolvePath(sourcePath, includePath);
                    includeContent = FileLoader.LoadFile(fullPath);
                }
                catch (Exception ex)
                {
                    throw new ShaderError($"Error processing include: '{includePath}': {ex.Message}");
                }

                try
                {
                    if (fullPath.EndsWith(".rs", StringComparison.OrdinalIgnoreCase) && rsFiles != null)
                    {
                        var rsInfo = RSParser.ParseContent(includeContent, fullPath);
                        rsFiles.Add(rsInfo);
                        return rsInfo.ProcessedCode;
                    }
                    return ProcessIncludesInternal(includeContent, fullPath, new HashSet<string>(processedPaths), rsFiles);
                }
                catch (Exception ex)
                {
                    throw new ShaderError($"Error processing include: '{includePath}': {ex.Message}");
                }
            });
        }

        // Класс для представления файла
        private class FileInfo
        {
            public string Path { get; set; }
            public string Content { get; set; }
            public List<string> Dependencies { get; } = new List<string>();
            public List<IncludeInfo> Includes { get; } = new List<IncludeInfo>();
            public bool IsProcessed { get; set; }
            public bool IsRsFile { get; set; }
            public RSFileInfo RsInfo { get; set; }
            public string ProcessedContent { get; set; }
            public int DependencyLevel { get; set; } = 0;

            // Для обнаружения циклов
            public bool InCurrentPath { get; set; }
        }

        // Класс для представления включения
        private class IncludeInfo
        {
            public int Position { get; set; }
            public string SourcePath { get; set; }
            public string IncludePath { get; set; }
            public string FullPath { get; set; }
            public Match Match { get; set; }
        }

        // Метод для обработки шейдера без дублирования включений
        public static string ProcessIncludesWithoutDuplication(string source, string sourcePath)
        {
            return ProcessIncludesWithoutDuplication(source, sourcePath, out _);
        }

        public static string ProcessIncludesWithoutDuplication(string source, string sourcePath, out List<RSFileInfo> rsFiles)
        {
            rsFiles = new List<RSFileInfo>();

            // Извлекаем директиву #version, если она существует
            var versionMatch = VersionRegex.Match(source);
            string versionDirective = null;
            string sourceWithoutVersion = source;

            if (versionMatch.Success)
            {
                versionDirective = versionMatch.Value;
                // Удаляем все директивы #version из исходного кода
                sourceWithoutVersion = VersionRegex.Replace(source, string.Empty);
            }

            // Создаем словарь файлов для отслеживания всех включений
            var files = new Dictionary<string, FileInfo>();

            // Добавляем исходный файл (без директивы #version)
            files[sourcePath] = new FileInfo
            {
                Path = sourcePath,
                Content = sourceWithoutVersion,
                ProcessedContent = sourceWithoutVersion
            };

            // Строим граф включений
            BuildIncludeGraph(sourcePath, files, rsFiles);

            // Проверяем наличие циклических зависимостей
            foreach (var file in files.Values)
            {
                if (HasCycle(file.Path, files, new HashSet<string>()))
                {
                    throw new CircularDependencyError($"Cyclic dependency detected in file: {file.Path}");
                }
            }

            // Вычисляем уровни зависимостей для определения порядка включения
            CalculateDependencyLevels(files);

            // Генерируем результат
            var result = new StringBuilder();

            // Если есть директива #version, добавляем её в начало
            if (versionDirective != null)
            {
                result.AppendLine(versionDirective);
                result.AppendLine();
            }

            // Обрабатываем общие включения в порядке уровней зависимостей
            var includedFiles = new HashSet<string>();
            ProcessCommonIncludes(files, includedFiles, result);

            // Обрабатываем основной файл
            var processedSource = ProcessFileIncludes(sourcePath, files, includedFiles);
            result.Append(processedSource);

            return result.ToString();
        }

        // Метод для обработки шейдера с секциями
        public static string ProcessShaderWithSections(string shaderSource, string shaderPath, out List<RSFileInfo> rsFiles)
        {
            rsFiles = new List<RSFileInfo>();

            if (shaderSource.Contains("#vertex") && shaderSource.Contains("#fragment"))
            {
                int vertexPos = shaderSource.IndexOf("#vertex");
                int fragmentPos = shaderSource.IndexOf("#fragment");

                StringBuilder resultBuilder = new StringBuilder();

                if (vertexPos < fragmentPos)
                {
                    // Сначала вершинный, затем фрагментный шейдер
                    string vertexMarker = "#vertex";
                    string vertexSection = shaderSource.Substring(vertexPos + vertexMarker.Length, fragmentPos - vertexPos - vertexMarker.Length);
                    string fragmentSection = shaderSource.Substring(fragmentPos + "#fragment".Length);

                    List<RSFileInfo> vertexRsFiles = new List<RSFileInfo>();
                    List<RSFileInfo> fragmentRsFiles = new List<RSFileInfo>();

                    resultBuilder.AppendLine("#vertex");
                    resultBuilder.Append(ProcessIncludesWithoutDuplication(vertexSection, shaderPath + ".vertex", out vertexRsFiles));

                    resultBuilder.AppendLine("#fragment");
                    resultBuilder.Append(ProcessIncludesWithoutDuplication(fragmentSection, shaderPath + ".fragment", out fragmentRsFiles));

                    // Добавляем уникальные RS-файлы
                    AddUniqueRsFiles(rsFiles, vertexRsFiles);
                    AddUniqueRsFiles(rsFiles, fragmentRsFiles);
                }
                else
                {
                    // Сначала фрагментный, затем вершинный шейдер
                    string fragmentMarker = "#fragment";
                    string fragmentSection = shaderSource.Substring(fragmentPos + fragmentMarker.Length, vertexPos - fragmentPos - fragmentMarker.Length);
                    string vertexSection = shaderSource.Substring(vertexPos + "#vertex".Length);

                    List<RSFileInfo> vertexRsFiles = new List<RSFileInfo>();
                    List<RSFileInfo> fragmentRsFiles = new List<RSFileInfo>();

                    resultBuilder.AppendLine("#fragment");
                    resultBuilder.Append(ProcessIncludesWithoutDuplication(fragmentSection, shaderPath + ".fragment", out fragmentRsFiles));

                    resultBuilder.AppendLine("#vertex");
                    resultBuilder.Append(ProcessIncludesWithoutDuplication(vertexSection, shaderPath + ".vertex", out vertexRsFiles));

                    // Добавляем уникальные RS-файлы
                    AddUniqueRsFiles(rsFiles, fragmentRsFiles);
                    AddUniqueRsFiles(rsFiles, vertexRsFiles);
                }

                return resultBuilder.ToString();
            }
            else
            {
                return ProcessIncludesWithoutDuplication(shaderSource, shaderPath, out rsFiles);
            }
        }

        // Строит граф включений
        private static void BuildIncludeGraph(string rootPath, Dictionary<string, FileInfo> files, List<RSFileInfo> rsFiles)
        {
            // Используем очередь для итеративного обхода
            var queue = new Queue<string>();
            queue.Enqueue(rootPath);

            while (queue.Count > 0)
            {
                var currentPath = queue.Dequeue();

                if (!files.TryGetValue(currentPath, out var file))
                    continue;

                // Пропускаем уже обработанные файлы
                if (file.IsProcessed)
                    continue;

                // Помечаем файл как обработанный
                file.IsProcessed = true;

                // Ищем все включения в текущем файле
                var matches = IncludeRegex.Matches(file.Content);

                foreach (Match match in matches)
                {
                    var includePath = match.Groups[1].Value;

                    try
                    {
                        string fullPath = FileLoader.ResolvePath(currentPath, includePath);

                        // Создаем информацию о включении
                        var includeInfo = new IncludeInfo
                        {
                            Position = match.Index,
                            SourcePath = currentPath,
                            IncludePath = includePath,
                            FullPath = fullPath,
                            Match = match
                        };

                        // Добавляем включение в текущий файл
                        file.Includes.Add(includeInfo);

                        // Добавляем зависимость
                        if (!file.Dependencies.Contains(fullPath))
                        {
                            file.Dependencies.Add(fullPath);
                        }

                        // Если файл уже загружен, пропускаем
                        if (files.ContainsKey(fullPath))
                            continue;

                        // Загружаем содержимое файла
                        string includeContent;
                        try
                        {
                            includeContent = FileLoader.LoadFile(fullPath);

                            // Удаляем директиву #version из включаемых файлов
                            includeContent = VersionRegex.Replace(includeContent, string.Empty);
                        }
                        catch (Exception ex)
                        {
                            throw new ShaderError($"Error loading file: '{fullPath}': {ex.Message}");
                        }

                        // Создаем новый узел для включаемого файла
                        var includeFile = new FileInfo
                        {
                            Path = fullPath,
                            Content = includeContent,
                            ProcessedContent = includeContent
                        };

                        // Специальная обработка для RS-файлов
                        if (fullPath.EndsWith(".rs", StringComparison.OrdinalIgnoreCase))
                        {
                            var rsInfo = RSParser.ParseContent(includeContent, fullPath);
                            includeFile.IsRsFile = true;
                            includeFile.RsInfo = rsInfo;
                            includeFile.ProcessedContent = rsInfo.ProcessedCode;

                            // Добавляем в список уникальных RS-файлов
                            if (!rsFiles.Any(rs => rs.SourcePath == fullPath))
                            {
                                rsFiles.Add(rsInfo);
                            }
                        }

                        // Добавляем файл в словарь
                        files[fullPath] = includeFile;

                        // Добавляем файл в очередь для обработки
                        queue.Enqueue(fullPath);
                    }
                    catch (Exception ex)
                    {
                        throw new ShaderError($"Error processing include: '{includePath}': {ex.Message}");
                    }
                }
            }
        }

        // Проверяет наличие циклических зависимостей
        private static bool HasCycle(string filePath, Dictionary<string, FileInfo> files, HashSet<string> inPath)
        {
            if (!files.TryGetValue(filePath, out var file))
                return false;

            if (inPath.Contains(filePath))
                return true;

            inPath.Add(filePath);

            foreach (var dep in file.Dependencies)
            {
                if (HasCycle(dep, files, new HashSet<string>(inPath)))
                    return true;
            }

            return false;
        }

        // Вычисляет уровни зависимостей для определения порядка включения
        private static void CalculateDependencyLevels(Dictionary<string, FileInfo> files)
        {
            bool changed;
            do
            {
                changed = false;

                foreach (var file in files.Values)
                {
                    foreach (var depPath in file.Dependencies)
                    {
                        if (files.TryGetValue(depPath, out var depFile))
                        {
                            // Зависимость должна иметь уровень на 1 ниже, чем зависящий от неё файл
                            int newLevel = file.DependencyLevel + 1;
                            if (depFile.DependencyLevel < newLevel)
                            {
                                depFile.DependencyLevel = newLevel;
                                changed = true;
                            }
                        }
                    }
                }
            } while (changed);
        }

        // Обрабатывает общие включения
        private static void ProcessCommonIncludes(Dictionary<string, FileInfo> files, HashSet<string> includedFiles, StringBuilder result)
        {
            // Находим файлы, которые используются несколькими другими файлами
            var referenceCount = new Dictionary<string, int>();

            foreach (var file in files.Values)
            {
                foreach (var dep in file.Dependencies)
                {
                    if (referenceCount.ContainsKey(dep))
                        referenceCount[dep]++;
                    else
                        referenceCount[dep] = 1;
                }
            }

            // Добавляем только файлы, используемые несколько раз, в порядке уровней зависимостей
            var multiReferencedFiles = files.Values
                .Where(f => referenceCount.TryGetValue(f.Path, out var count) && count > 1)
                .OrderByDescending(f => f.DependencyLevel)
                .Select(f => f.Path)
                .ToList();

            foreach (var path in multiReferencedFiles)
            {
                var file = files[path];

                // Добавляем файл в результат
                result.AppendLine($"// Included from: {path}");
                result.AppendLine(file.ProcessedContent);
                result.AppendLine();

                includedFiles.Add(path);
            }
        }

        // Обрабатывает включения в одном файле
        private static string ProcessFileIncludes(string filePath, Dictionary<string, FileInfo> files, HashSet<string> includedFiles)
        {
            if (!files.TryGetValue(filePath, out var file))
                return string.Empty;

            var content = file.Content;
            var result = new StringBuilder();
            int currentPos = 0;

            // Сортируем включения по позиции для корректной обработки
            var sortedIncludes = file.Includes.OrderBy(incl => incl.Position).ToList();

            foreach (var include in sortedIncludes)
            {
                // Добавляем текст до текущего включения
                result.Append(content.Substring(currentPos, include.Position - currentPos));

                // Обрабатываем включение
                if (includedFiles.Contains(include.FullPath))
                {
                    // Файл уже включен, добавляем комментарий
                    result.AppendLine($"// #include \"{include.IncludePath}\" (already included)");
                }
                else if (files.TryGetValue(include.FullPath, out var includeFile))
                {
                    // Добавляем комментарий и содержимое файла
                    result.AppendLine($"// Included from: {include.IncludePath}");

                    // Рекурсивно обрабатываем включения в этом файле
                    string processedInclude = ProcessFileIncludes(include.FullPath, files, includedFiles);
                    result.Append(processedInclude);

                    includedFiles.Add(include.FullPath);
                }
                else
                {
                    // Файл не найден, оставляем директиву включения
                    result.Append(include.Match.Value);
                }

                currentPos = include.Position + include.Match.Length;
            }

            // Добавляем оставшийся текст
            if (currentPos < content.Length)
            {
                result.Append(content.Substring(currentPos));
            }

            return result.ToString();
        }

        // Добавляет уникальные RS-файлы в список
        private static void AddUniqueRsFiles(List<RSFileInfo> targetList, List<RSFileInfo> sourceList)
        {
            var existingPaths = new HashSet<string>(targetList.Select(file => file.SourcePath));

            foreach (var file in sourceList)
            {
                if (!existingPaths.Contains(file.SourcePath))
                {
                    targetList.Add(file);
                    existingPaths.Add(file.SourcePath);
                }
            }
        }
    }




    //public static class IncludeProcessor
    //{
    //    private static readonly Regex IncludeRegex = new Regex(@"#include\s+""([^""]+)""");
    //    private static readonly Regex VersionRegex = new Regex(@"#version\s+\d+\s+\w*");

    //    public static void RegisterContentProvider(IContentProvider provider)
    //    {
    //        FileLoader.RegisterContentProvider(provider);
    //    }

    //    // Базовые методы ProcessIncludes остаются неизменными
    //    public static string ProcessIncludes(string source, string sourcePath, HashSet<string> processedPaths)
    //    {
    //        return ProcessIncludesInternal(source, sourcePath, processedPaths, null);
    //    }

    //    public static string ProcessIncludes(string source, string sourcePath, HashSet<string> processedPaths, out List<RSFileInfo> rsFiles)
    //    {
    //        rsFiles = new List<RSFileInfo>();
    //        return ProcessIncludesInternal(source, sourcePath, processedPaths, rsFiles);
    //    }

    //    public static string ProcessIncludes(string source, string sourcePath, HashSet<string> processedPaths, bool collectRsFiles, out List<RSFileInfo> rsFiles)
    //    {
    //        rsFiles = collectRsFiles ? new List<RSFileInfo>() : null;
    //        return ProcessIncludesInternal(source, sourcePath, processedPaths, rsFiles);
    //    }

    //    private static string ProcessIncludesInternal(string source, string sourcePath, HashSet<string> processedPaths, List<RSFileInfo> rsFiles)
    //    {
    //        processedPaths ??= new HashSet<string>();

    //        if (processedPaths.Contains(sourcePath))
    //            throw new CircularDependencyError($"Cyclic dependency detected: {sourcePath}");

    //        processedPaths.Add(sourcePath);

    //        return IncludeRegex.Replace(source, match => {
    //            var includePath = match.Groups[1].Value;
    //            string fullPath;
    //            string includeContent;

    //            try
    //            {
    //                fullPath = FileLoader.ResolvePath(sourcePath, includePath);
    //                includeContent = FileLoader.LoadFile(fullPath);
    //            }
    //            catch (Exception ex)
    //            {
    //                throw new ShaderError($"Error processing include: '{includePath}': {ex.Message}");
    //            }

    //            try
    //            {
    //                if (fullPath.EndsWith(".rs", StringComparison.OrdinalIgnoreCase) && rsFiles != null)
    //                {
    //                    var rsInfo = RSParser.ParseContent(includeContent, fullPath);
    //                    rsFiles.Add(rsInfo);
    //                    return rsInfo.ProcessedCode;
    //                }
    //                return ProcessIncludesInternal(includeContent, fullPath, new HashSet<string>(processedPaths), rsFiles);
    //            }
    //            catch (Exception ex)
    //            {
    //                throw new ShaderError($"Error processing include: '{includePath}': {ex.Message}");
    //            }
    //        });
    //    }

    //    // Класс для представления файла
    //    private class FileInfo
    //    {
    //        public string Path { get; set; }
    //        public string Content { get; set; }
    //        public List<string> Dependencies { get; } = new List<string>();
    //        public List<IncludeInfo> Includes { get; } = new List<IncludeInfo>();
    //        public bool IsProcessed { get; set; }
    //        public bool IsRsFile { get; set; }
    //        public RSFileInfo RsInfo { get; set; }
    //        public string ProcessedContent { get; set; }
    //        public int DependencyLevel { get; set; } = 0;

    //        // Для обнаружения циклов
    //        public bool InCurrentPath { get; set; }
    //    }

    //    // Класс для представления включения
    //    private class IncludeInfo
    //    {
    //        public int Position { get; set; }
    //        public string SourcePath { get; set; }
    //        public string IncludePath { get; set; }
    //        public string FullPath { get; set; }
    //        public Match Match { get; set; }
    //    }

    //    // Метод для обработки шейдера без дублирования включений
    //    public static string ProcessIncludesWithoutDuplication(string source, string sourcePath)
    //    {
    //        return ProcessIncludesWithoutDuplication(source, sourcePath, out _);
    //    }

    //    public static string ProcessIncludesWithoutDuplication(string source, string sourcePath, out List<RSFileInfo> rsFiles)
    //    {
    //        rsFiles = new List<RSFileInfo>();

    //        // Извлекаем директиву #version, если она существует
    //        var versionMatch = VersionRegex.Match(source);
    //        string versionDirective = null;
    //        if (versionMatch.Success)
    //        {
    //            versionDirective = versionMatch.Value;
    //            // Не удаляем директиву из исходного кода - мы её сохраним на месте
    //        }

    //        // Создаем словарь файлов для отслеживания всех включений
    //        var files = new Dictionary<string, FileInfo>();

    //        // Добавляем исходный файл
    //        files[sourcePath] = new FileInfo
    //        {
    //            Path = sourcePath,
    //            Content = source,
    //            ProcessedContent = source
    //        };

    //        // Строим граф включений
    //        BuildIncludeGraph(sourcePath, files, rsFiles);

    //        // Проверяем наличие циклических зависимостей
    //        foreach (var file in files.Values)
    //        {
    //            if (HasCycle(file.Path, files, new HashSet<string>()))
    //            {
    //                throw new CircularDependencyError($"Cyclic dependency detected in file: {file.Path}");
    //            }
    //        }

    //        // Вычисляем уровни зависимостей для определения порядка включения
    //        CalculateDependencyLevels(files);

    //        // Генерируем результат
    //        var result = new StringBuilder();

    //        // Если есть директива #version, добавляем её в начало
    //        if (versionDirective != null)
    //        {
    //            result.AppendLine(versionDirective);
    //            result.AppendLine();
    //        }

    //        // Обрабатываем общие включения в порядке уровней зависимостей
    //        var includedFiles = new HashSet<string>();
    //        ProcessCommonIncludes(files, includedFiles, result);

    //        // Обрабатываем основной файл
    //        var processedSource = ProcessFileIncludes(sourcePath, files, includedFiles, versionDirective);
    //        result.Append(processedSource);

    //        return result.ToString();
    //    }

    //    // Метод для обработки шейдера с секциями
    //    public static string ProcessShaderWithSections(string shaderSource, string shaderPath, out List<RSFileInfo> rsFiles)
    //    {
    //        rsFiles = new List<RSFileInfo>();

    //        if (shaderSource.Contains("#vertex") && shaderSource.Contains("#fragment"))
    //        {
    //            int vertexPos = shaderSource.IndexOf("#vertex");
    //            int fragmentPos = shaderSource.IndexOf("#fragment");

    //            StringBuilder resultBuilder = new StringBuilder();

    //            if (vertexPos < fragmentPos)
    //            {
    //                // Сначала вершинный, затем фрагментный шейдер
    //                string vertexMarker = "#vertex";
    //                string vertexSection = shaderSource.Substring(vertexPos + vertexMarker.Length, fragmentPos - vertexPos - vertexMarker.Length);
    //                string fragmentSection = shaderSource.Substring(fragmentPos + "#fragment".Length);

    //                List<RSFileInfo> vertexRsFiles = new List<RSFileInfo>();
    //                List<RSFileInfo> fragmentRsFiles = new List<RSFileInfo>();

    //                resultBuilder.AppendLine("#vertex");
    //                resultBuilder.Append(ProcessIncludesWithoutDuplication(vertexSection, shaderPath + ".vertex", out vertexRsFiles));

    //                resultBuilder.AppendLine("#fragment");
    //                resultBuilder.Append(ProcessIncludesWithoutDuplication(fragmentSection, shaderPath + ".fragment", out fragmentRsFiles));

    //                // Добавляем уникальные RS-файлы
    //                AddUniqueRsFiles(rsFiles, vertexRsFiles);
    //                AddUniqueRsFiles(rsFiles, fragmentRsFiles);
    //            }
    //            else
    //            {
    //                // Сначала фрагментный, затем вершинный шейдер
    //                string fragmentMarker = "#fragment";
    //                string fragmentSection = shaderSource.Substring(fragmentPos + fragmentMarker.Length, vertexPos - fragmentPos - fragmentMarker.Length);
    //                string vertexSection = shaderSource.Substring(vertexPos + "#vertex".Length);

    //                List<RSFileInfo> vertexRsFiles = new List<RSFileInfo>();
    //                List<RSFileInfo> fragmentRsFiles = new List<RSFileInfo>();

    //                resultBuilder.AppendLine("#fragment");
    //                resultBuilder.Append(ProcessIncludesWithoutDuplication(fragmentSection, shaderPath + ".fragment", out fragmentRsFiles));

    //                resultBuilder.AppendLine("#vertex");
    //                resultBuilder.Append(ProcessIncludesWithoutDuplication(vertexSection, shaderPath + ".vertex", out vertexRsFiles));

    //                // Добавляем уникальные RS-файлы
    //                AddUniqueRsFiles(rsFiles, fragmentRsFiles);
    //                AddUniqueRsFiles(rsFiles, vertexRsFiles);
    //            }

    //            return resultBuilder.ToString();
    //        }
    //        else
    //        {
    //            return ProcessIncludesWithoutDuplication(shaderSource, shaderPath, out rsFiles);
    //        }
    //    }

    //    // Строит граф включений
    //    private static void BuildIncludeGraph(string rootPath, Dictionary<string, FileInfo> files, List<RSFileInfo> rsFiles)
    //    {
    //        // Используем очередь для итеративного обхода
    //        var queue = new Queue<string>();
    //        queue.Enqueue(rootPath);

    //        while (queue.Count > 0)
    //        {
    //            var currentPath = queue.Dequeue();

    //            if (!files.TryGetValue(currentPath, out var file))
    //                continue;

    //            // Пропускаем уже обработанные файлы
    //            if (file.IsProcessed)
    //                continue;

    //            // Помечаем файл как обработанный
    //            file.IsProcessed = true;

    //            // Ищем все включения в текущем файле
    //            var matches = IncludeRegex.Matches(file.Content);

    //            foreach (Match match in matches)
    //            {
    //                var includePath = match.Groups[1].Value;

    //                try
    //                {
    //                    string fullPath = FileLoader.ResolvePath(currentPath, includePath);

    //                    // Создаем информацию о включении
    //                    var includeInfo = new IncludeInfo
    //                    {
    //                        Position = match.Index,
    //                        SourcePath = currentPath,
    //                        IncludePath = includePath,
    //                        FullPath = fullPath,
    //                        Match = match
    //                    };

    //                    // Добавляем включение в текущий файл
    //                    file.Includes.Add(includeInfo);

    //                    // Добавляем зависимость
    //                    if (!file.Dependencies.Contains(fullPath))
    //                    {
    //                        file.Dependencies.Add(fullPath);
    //                    }

    //                    // Если файл уже загружен, пропускаем
    //                    if (files.ContainsKey(fullPath))
    //                        continue;

    //                    // Загружаем содержимое файла
    //                    string includeContent;
    //                    try
    //                    {
    //                        includeContent = FileLoader.LoadFile(fullPath);
    //                    }
    //                    catch (Exception ex)
    //                    {
    //                        throw new ShaderError($"Error loading file: '{fullPath}': {ex.Message}");
    //                    }

    //                    // Создаем новый узел для включаемого файла
    //                    var includeFile = new FileInfo
    //                    {
    //                        Path = fullPath,
    //                        Content = includeContent,
    //                        ProcessedContent = includeContent
    //                    };

    //                    // Специальная обработка для RS-файлов
    //                    if (fullPath.EndsWith(".rs", StringComparison.OrdinalIgnoreCase))
    //                    {
    //                        var rsInfo = RSParser.ParseContent(includeContent, fullPath);
    //                        includeFile.IsRsFile = true;
    //                        includeFile.RsInfo = rsInfo;
    //                        includeFile.ProcessedContent = rsInfo.ProcessedCode;

    //                        // Добавляем в список уникальных RS-файлов
    //                        if (!rsFiles.Any(rs => rs.SourcePath == fullPath))
    //                        {
    //                            rsFiles.Add(rsInfo);
    //                        }
    //                    }

    //                    // Добавляем файл в словарь
    //                    files[fullPath] = includeFile;

    //                    // Добавляем файл в очередь для обработки
    //                    queue.Enqueue(fullPath);
    //                }
    //                catch (Exception ex)
    //                {
    //                    throw new ShaderError($"Error processing include: '{includePath}': {ex.Message}");
    //                }
    //            }
    //        }
    //    }

    //    // Проверяет наличие циклических зависимостей
    //    private static bool HasCycle(string filePath, Dictionary<string, FileInfo> files, HashSet<string> inPath)
    //    {
    //        if (!files.TryGetValue(filePath, out var file))
    //            return false;

    //        if (inPath.Contains(filePath))
    //            return true;

    //        inPath.Add(filePath);

    //        foreach (var dep in file.Dependencies)
    //        {
    //            if (HasCycle(dep, files, new HashSet<string>(inPath)))
    //                return true;
    //        }

    //        return false;
    //    }

    //    // Вычисляет уровни зависимостей для определения порядка включения
    //    private static void CalculateDependencyLevels(Dictionary<string, FileInfo> files)
    //    {
    //        bool changed;
    //        do
    //        {
    //            changed = false;

    //            foreach (var file in files.Values)
    //            {
    //                foreach (var depPath in file.Dependencies)
    //                {
    //                    if (files.TryGetValue(depPath, out var depFile))
    //                    {
    //                        // Зависимость должна иметь уровень на 1 ниже, чем зависящий от неё файл
    //                        int newLevel = file.DependencyLevel + 1;
    //                        if (depFile.DependencyLevel < newLevel)
    //                        {
    //                            depFile.DependencyLevel = newLevel;
    //                            changed = true;
    //                        }
    //                    }
    //                }
    //            }
    //        } while (changed);
    //    }

    //    // Обрабатывает общие включения
    //    private static void ProcessCommonIncludes(Dictionary<string, FileInfo> files, HashSet<string> includedFiles, StringBuilder result)
    //    {
    //        // Находим файлы, которые используются несколькими другими файлами
    //        var referenceCount = new Dictionary<string, int>();

    //        foreach (var file in files.Values)
    //        {
    //            foreach (var dep in file.Dependencies)
    //            {
    //                if (referenceCount.ContainsKey(dep))
    //                    referenceCount[dep]++;
    //                else
    //                    referenceCount[dep] = 1;
    //            }
    //        }

    //        // Добавляем только файлы, используемые несколько раз, в порядке уровней зависимостей
    //        var multiReferencedFiles = files.Values
    //            .Where(f => referenceCount.TryGetValue(f.Path, out var count) && count > 1)
    //            .OrderByDescending(f => f.DependencyLevel)
    //            .Select(f => f.Path)
    //            .ToList();

    //        foreach (var path in multiReferencedFiles)
    //        {
    //            var file = files[path];

    //            // Добавляем файл в результат
    //            result.AppendLine($"// Included from: {path}");
    //            result.AppendLine(file.ProcessedContent);
    //            result.AppendLine();

    //            includedFiles.Add(path);
    //        }
    //    }

    //    // Обрабатывает включения в одном файле
    //    private static string ProcessFileIncludes(string filePath, Dictionary<string, FileInfo> files, HashSet<string> includedFiles, string versionDirective)
    //    {
    //        if (!files.TryGetValue(filePath, out var file))
    //            return string.Empty;

    //        var content = file.Content;
    //        var result = new StringBuilder();
    //        int currentPos = 0;

    //        // Обрабатываем директиву #version
    //        if (versionDirective != null)
    //        {
    //            var versionMatch = VersionRegex.Match(content);
    //            if (versionMatch.Success)
    //            {
    //                // Добавляем текст до директивы #version
    //                if (versionMatch.Index > 0)
    //                {
    //                    result.Append(content.Substring(0, versionMatch.Index));
    //                }

    //                // Добавляем директиву #version
    //                result.AppendLine(versionMatch.Value);

    //                // Обновляем позицию
    //                currentPos = versionMatch.Index + versionMatch.Length;
    //            }
    //        }

    //        // Сортируем включения по позиции для корректной обработки
    //        var sortedIncludes = file.Includes.OrderBy(incl => incl.Position).ToList();

    //        foreach (var include in sortedIncludes)
    //        {
    //            // Добавляем текст до текущего включения
    //            result.Append(content.Substring(currentPos, include.Position - currentPos));

    //            // Обрабатываем включение
    //            if (includedFiles.Contains(include.FullPath))
    //            {
    //                // Файл уже включен, добавляем комментарий
    //                result.AppendLine($"// #include \"{include.IncludePath}\" (already included)");
    //            }
    //            else if (files.TryGetValue(include.FullPath, out var includeFile))
    //            {
    //                // Добавляем комментарий и содержимое файла
    //                result.AppendLine($"// Included from: {include.IncludePath}");

    //                // Рекурсивно обрабатываем включения в этом файле
    //                string processedInclude = ProcessFileIncludes(include.FullPath, files, includedFiles, null);
    //                result.Append(processedInclude);

    //                includedFiles.Add(include.FullPath);
    //            }
    //            else
    //            {
    //                // Файл не найден, оставляем директиву включения
    //                result.Append(include.Match.Value);
    //            }

    //            currentPos = include.Position + include.Match.Length;
    //        }

    //        // Добавляем оставшийся текст
    //        if (currentPos < content.Length)
    //        {
    //            result.Append(content.Substring(currentPos));
    //        }

    //        return result.ToString();
    //    }

    //    // Добавляет уникальные RS-файлы в список
    //    private static void AddUniqueRsFiles(List<RSFileInfo> targetList, List<RSFileInfo> sourceList)
    //    {
    //        var existingPaths = new HashSet<string>(targetList.Select(file => file.SourcePath));

    //        foreach (var file in sourceList)
    //        {
    //            if (!existingPaths.Contains(file.SourcePath))
    //            {
    //                targetList.Add(file);
    //                existingPaths.Add(file.SourcePath);
    //            }
    //        }
    //    }
    //}





    //public static class IncludeProcessor
    //{
    //    private static readonly Regex IncludeRegex = new Regex(@"#include\s+""([^""]+)""");

    //    public static void RegisterContentProvider(IContentProvider provider)
    //    {
    //        FileLoader.RegisterContentProvider(provider);
    //    }

    //    public static string ProcessIncludes(string source, string sourcePath, HashSet<string> processedPaths, bool collectRsFiles, out List<RSFileInfo> rsFiles)
    //    {
    //        rsFiles = collectRsFiles ? new List<RSFileInfo>() : null;
    //        return ProcessIncludesInternal(source, sourcePath, processedPaths, rsFiles);
    //    }

    //    public static string ProcessIncludes(string source, string sourcePath, HashSet<string> processedPaths)
    //    {
    //        return ProcessIncludesInternal(source, sourcePath, processedPaths, null);
    //    }

    //    public static string ProcessIncludes(string source, string sourcePath, HashSet<string> processedPaths, out List<RSFileInfo> rsFiles)
    //    {
    //        rsFiles = new List<RSFileInfo>();
    //        return ProcessIncludesInternal(source, sourcePath, processedPaths, rsFiles);
    //    }

    //    private static string ProcessIncludesInternal(string source, string sourcePath, HashSet<string> processedPaths, List<RSFileInfo> rsFiles)
    //    {
    //        processedPaths ??= new HashSet<string>();

    //        if (processedPaths.Contains(sourcePath))
    //            throw new CircularDependencyError($"Cyclic dependency detected: {sourcePath}");

    //        processedPaths.Add(sourcePath);

    //        return IncludeRegex.Replace(source, match => {
    //            var includePath = match.Groups[1].Value;
    //            string fullPath;
    //            string includeContent;

    //            try
    //            {
    //                fullPath = FileLoader.ResolvePath(sourcePath, includePath);
    //                includeContent = FileLoader.LoadFile(fullPath);
    //            }
    //            catch (Exception ex)
    //            {
    //                throw new ShaderError($"Error processing include: '{includePath}': {ex.Message}");
    //            }

    //            try
    //            {
    //                if (fullPath.EndsWith(".rs", StringComparison.OrdinalIgnoreCase) && rsFiles != null)
    //                {
    //                    var rsInfo = RSParser.ParseContent(includeContent, fullPath);
    //                    rsFiles.Add(rsInfo);
    //                    return rsInfo.ProcessedCode;
    //                }
    //                return ProcessIncludesInternal(includeContent, fullPath, new HashSet<string>(processedPaths), rsFiles);
    //            }
    //            catch (Exception ex)
    //            {
    //                throw new ShaderError($"Error processing include: '{includePath}': {ex.Message}");
    //            }
    //        });
    //    }
    //}


}
