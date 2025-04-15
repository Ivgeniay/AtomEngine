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

        public static void RegisterContentProvider(IContentProvider provider)
        {
            FileLoader.RegisterContentProvider(provider);
        }

        public static string ProcessIncludes(string source, string sourcePath, HashSet<string> processedPaths, bool collectRsFiles, out List<RSFileInfo> rsFiles)
        {
            rsFiles = collectRsFiles ? new List<RSFileInfo>() : null;
            return ProcessIncludesInternal(source, sourcePath, processedPaths, rsFiles);
        }

        public static string ProcessIncludes(string source, string sourcePath, HashSet<string> processedPaths)
        {
            return ProcessIncludesInternal(source, sourcePath, processedPaths, null);
        }

        public static string ProcessIncludes(string source, string sourcePath, HashSet<string> processedPaths, out List<RSFileInfo> rsFiles)
        {
            rsFiles = new List<RSFileInfo>();
            return ProcessIncludesInternal(source, sourcePath, processedPaths, rsFiles);
        }

        public static string ProcessIncludesWithoutDuplication(string source, string sourcePath)
        {
            return ProcessIncludesWithoutDuplication(source, sourcePath, out _);
        }

        public static string ProcessIncludesWithoutDuplication(string source, string sourcePath, out List<RSFileInfo> rsFiles)
        {
            rsFiles = new List<RSFileInfo>();
            var includeGraph = new IncludeDependencyGraph();

            AnalyzeIncludes(source, sourcePath, includeGraph, rsFiles);

            includeGraph.CheckForCyclicDependencies();

            return GenerateProcessedCode(source, sourcePath, includeGraph);
        }

        public static string ProcessShaderWithSections(string shaderSource, string shaderPath, out List<RSFileInfo> rsFiles)
        {
            rsFiles = new List<RSFileInfo>();

            if (shaderSource.Contains("#vertex") && shaderSource.Contains("#fragment"))
            {
                int vertexPos = shaderSource.IndexOf("#vertex");
                int fragmentPos = shaderSource.IndexOf("#fragment");

                if (vertexPos < fragmentPos)
                {
                    string vertexMarker = "#vertex";
                    string vertexSection = shaderSource.Substring(vertexPos + vertexMarker.Length, fragmentPos - vertexPos - vertexMarker.Length);
                    string fragmentSection = shaderSource.Substring(fragmentPos + "#fragment".Length);

                    List<RSFileInfo> vertexRsFiles;
                    List<RSFileInfo> fragmentRsFiles;

                    string processedVertexSection = ProcessIncludesWithoutDuplication(vertexSection, shaderPath, out vertexRsFiles);
                    string processedFragmentSection = ProcessIncludesWithoutDuplication(fragmentSection, shaderPath, out fragmentRsFiles);

                    rsFiles.AddRange(vertexRsFiles);
                    rsFiles.AddRange(fragmentRsFiles);

                    return $"#vertex\n{processedVertexSection}\n#fragment\n{processedFragmentSection}";
                }
                else
                {
                    string fragmentMarker = "#fragment";
                    string fragmentSection = shaderSource.Substring(fragmentPos + fragmentMarker.Length, vertexPos - fragmentPos - fragmentMarker.Length);
                    string vertexSection = shaderSource.Substring(vertexPos + "#vertex".Length);

                    List<RSFileInfo> vertexRsFiles;
                    List<RSFileInfo> fragmentRsFiles;

                    string processedFragmentSection = ProcessIncludesWithoutDuplication(fragmentSection, shaderPath, out fragmentRsFiles);
                    string processedVertexSection = ProcessIncludesWithoutDuplication(vertexSection, shaderPath, out vertexRsFiles);

                    rsFiles.AddRange(fragmentRsFiles);
                    rsFiles.AddRange(vertexRsFiles);

                    return $"#fragment\n{processedFragmentSection}\n#vertex\n{processedVertexSection}";
                }
            }
            else
            {
                return ProcessIncludesWithoutDuplication(shaderSource, shaderPath, out rsFiles);
            }
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

        private class IncludeDependencyGraph
        {
            public class IncludeNode
            {
                public string Path { get; set; }
                public string Content { get; set; }
                public string ProcessedContent { get; set; }
                public bool IsRsFile { get; set; }
                public RSFileInfo RsInfo { get; set; }
                public List<string> Dependencies { get; set; }
                public List<IncludeLocation> IncludeLocations { get; set; }
                public bool Visited { get; set; } = false;
                public bool InStack { get; set; } = false;
                public bool IncludesAnalyzed { get; set; } = false;

                public IncludeNode()
                {
                    Dependencies = new List<string>();
                    IncludeLocations = new List<IncludeLocation>();
                }
            }

            public class IncludeLocation
            {
                public string SourceFile { get; set; }
                public int Position { get; set; }
                public Match RegexMatch { get; set; }
            }

            private Dictionary<string, IncludeNode> nodes = new Dictionary<string, IncludeNode>();
            private HashSet<string> includedInResult = new HashSet<string>();

            public IncludeNode AddNode(string path, string content)
            {
                if (!nodes.TryGetValue(path, out var node))
                {
                    node = new IncludeNode
                    {
                        Path = path,
                        Content = content,
                        ProcessedContent = content,
                        IncludesAnalyzed = false
                    };
                    nodes[path] = node;
                }
                return node;
            }

            public void UpdateNodeWithRsInfo(string path, RSFileInfo rsInfo)
            {
                if (nodes.TryGetValue(path, out var node))
                {
                    node.IsRsFile = true;
                    node.RsInfo = rsInfo;
                    node.ProcessedContent = rsInfo.ProcessedCode;
                }
            }

            public void AddDependency(string fromPath, string toPath)
            {
                if (nodes.TryGetValue(fromPath, out var fromNode) &&
                    !fromNode.Dependencies.Contains(toPath))
                {
                    fromNode.Dependencies.Add(toPath);
                }
            }

            public void AddIncludeLocation(string targetPath, string sourcePath, int position, Match match)
            {
                if (nodes.TryGetValue(targetPath, out var node))
                {
                    node.IncludeLocations.Add(new IncludeLocation
                    {
                        SourceFile = sourcePath,
                        Position = position,
                        RegexMatch = match
                    });
                }
            }

            public void CheckForCyclicDependencies()
            {
                foreach (var node in nodes.Values)
                {
                    node.Visited = false;
                    node.InStack = false;
                }

                foreach (var node in nodes.Values)
                {
                    if (!node.Visited && IsCyclicUtil(node))
                    {
                        throw new CircularDependencyError($"Cyclic dependency detected: {node.Path}");
                    }
                }
            }

            private bool IsCyclicUtil(IncludeNode node)
            {
                node.Visited = true;
                node.InStack = true;

                foreach (var depPath in node.Dependencies)
                {
                    if (nodes.TryGetValue(depPath, out var depNode))
                    {
                        if (!depNode.Visited)
                        {
                            if (IsCyclicUtil(depNode))
                                return true;
                        }
                        else if (depNode.InStack)
                        {
                            return true;
                        }
                    }
                }

                node.InStack = false;
                return false;
            }

            public Dictionary<string, IncludeNode> GetNodes()
            {
                return nodes;
            }

            public bool IsIncludedInResult(string path)
            {
                return includedInResult.Contains(path);
            }

            public void MarkAsIncluded(string path)
            {
                includedInResult.Add(path);
            }

            public int GetUsageCount(string path)
            {
                return nodes.TryGetValue(path, out var node) ? node.IncludeLocations.Count : 0;
            }
        }

        private static void AnalyzeIncludes(string source, string sourcePath, IncludeDependencyGraph graph, List<RSFileInfo> rsFiles)
        {
            var node = graph.AddNode(sourcePath, source);

            if (node.IncludesAnalyzed)
            {
                return;
            }

            // Помечаем узел как анализируемый перед началом анализа
            // чтобы избежать циклической рекурсии
            node.IncludesAnalyzed = true;

            var matches = IncludeRegex.Matches(source);

            foreach (Match match in matches)
            {
                var includePath = match.Groups[1].Value;
                string fullPath;
                string includeContent;

                try
                {
                    fullPath = FileLoader.ResolvePath(sourcePath, includePath);

                    graph.AddDependency(sourcePath, fullPath);
                    graph.AddIncludeLocation(fullPath, sourcePath, match.Index, match);

                    // Проверяем, существует ли узел для этого включения
                    if (!graph.GetNodes().TryGetValue(fullPath, out var existingNode))
                    {
                        // Загружаем содержимое файла
                        includeContent = FileLoader.LoadFile(fullPath);

                        if (fullPath.EndsWith(".rs", StringComparison.OrdinalIgnoreCase))
                        {
                            var rsInfo = RSParser.ParseContent(includeContent, fullPath);
                            rsFiles.Add(rsInfo);

                            // Создаем новый узел с исходным содержимым
                            var newNode = graph.AddNode(fullPath, includeContent);
                            graph.UpdateNodeWithRsInfo(fullPath, rsInfo);

                            // ВАЖНО: Анализируем включения в исходном содержимом файла RS, 
                            // а не в обработанном (rsInfo.ProcessedCode)
                            AnalyzeIncludes(includeContent, fullPath, graph, rsFiles);
                        }
                        else
                        {
                            // Для обычных файлов просто добавляем узел и рекурсивно обрабатываем
                            graph.AddNode(fullPath, includeContent);
                            AnalyzeIncludes(includeContent, fullPath, graph, rsFiles);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new ShaderError($"Error processing include: '{includePath}': {ex.Message}");
                }
            }
        }

        private static string GenerateProcessedCode(string source, string sourcePath, IncludeDependencyGraph graph)
        {
            var result = new System.Text.StringBuilder();
            var nodes = graph.GetNodes();

            // Сначала добавляем все файлы, используемые более одного раза
            foreach (var node in nodes.Values)
            {
                int usageCount = graph.GetUsageCount(node.Path);

                if (usageCount > 1 && !graph.IsIncludedInResult(node.Path))
                {
                    result.AppendLine($"// Included from: {node.Path}");
                    result.AppendLine(node.ProcessedContent);
                    result.AppendLine();

                    graph.MarkAsIncluded(node.Path);
                }
            }

            // Затем обрабатываем исходный файл
            ProcessSourceWithIncludes(source, sourcePath, graph, result);

            return result.ToString();
        }

        private static void ProcessSourceWithIncludes(string source, string sourcePath, IncludeDependencyGraph graph, System.Text.StringBuilder result)
        {
            // Заменяем все включения непосредственно в исходном тексте
            int currentPos = 0;
            var matches = IncludeRegex.Matches(source);

            foreach (Match match in matches)
            {
                // Добавляем текст до текущего включения
                result.Append(source.Substring(currentPos, match.Index - currentPos));

                var includePath = match.Groups[1].Value;
                string fullPath;

                try
                {
                    fullPath = FileLoader.ResolvePath(sourcePath, includePath);

                    if (graph.IsIncludedInResult(fullPath))
                    {
                        // Файл уже включен, заменяем директиву комментарием
                        result.AppendLine($"// #include \"{includePath}\" (already included)");
                    }
                    else if (graph.GetNodes().TryGetValue(fullPath, out var node))
                    {
                        // Однократно используемый файл - включаем его содержимое
                        result.AppendLine($"// Included from: {includePath}");
                        result.Append(node.ProcessedContent);

                        graph.MarkAsIncluded(fullPath);
                    }
                    else
                    {
                        // Файл не найден, оставляем директиву без изменений
                        result.Append(match.Value);
                    }
                }
                catch
                {
                    // В случае ошибки оставляем директиву
                    result.Append(match.Value);
                }

                currentPos = match.Index + match.Length;
            }

            // Добавляем оставшийся текст
            if (currentPos < source.Length)
            {
                result.Append(source.Substring(currentPos));
            }
        }

    }




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
