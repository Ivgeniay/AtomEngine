using System.Collections.Generic;
using System.IO;
using System;
using System.Text;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using OpenglLib;
using System.Reflection.Metadata;

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

            string sourceGuid = ServiceHub.Get<MetadataManager>().GetMetadata(glslFilePath)?.Guid;

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

        internal unsafe static CompilationResult TryToCompile(FileEvent e)
        {
            CompilationResult result = new CompilationResult();
            result.FilePath = e.FileFullPath;
            GL gl = null;

            try
            {
                result.Log.Append("Cheking existing file: ");
                if (!File.Exists(e.FileFullPath))
                {
                    result.Success = false;
                    result.Message = "File not found";
                    result.Log.AppendLine($"{e.FileFullPath} is not exist");
                    return result;
                }
                result.Log.AppendLine($"{e.FileName} found");

                string shaderSource = File.ReadAllText(e.FileFullPath);

                result.Log.Append("Cheking full shader: ");
                if (!GlslParser.IsCompleteShaderFile(shaderSource))
                {
                    result.Success = false;
                    result.Message = "Not full";
                    result.Log.AppendLine("There is no complete shader (required #vertex and #fragment sections)");
                    return result;
                }
                result.Log.AppendLine("full shader");

                var (vertexSource, fragmentSource) = GlslParser.ExtractShaderSources(shaderSource);

                var options = WindowOptions.Default;
                options.Size = new Silk.NET.Maths.Vector2D<int>(1, 1);
                options.Title = "GLSL Compiler";
                options.VSync = false;
                options.ShouldSwapAutomatically = false;
                options.IsVisible = false;
                options.API = new GraphicsAPI(
                    ContextAPI.OpenGL,
                    ContextProfile.Core,
                    ContextFlags.Debug,
                    new APIVersion(3, 3)
                );

                using var window = Window.Create(options);
                window.Initialize();

                gl = window.CreateOpenGL();

                if (gl != null)
                {
                    var glVersion = gl.GetString(StringName.Version);
                    var shaderVersion = gl.GetString(StringName.ShadingLanguageVersion);

                    result.Log.AppendLine($"OpenGL версия: {*glVersion}");
                    result.Log.AppendLine($"GLSL версия: {*shaderVersion}");

                    uint vertexShader = CompileShader(gl, vertexSource, ShaderType.VertexShader, result);
                    if (vertexShader == 0)
                    {
                        result.Success = false;
                        result.Message = "Fail compiling #vertex shader";
                        return result;
                    }

                    uint fragmentShader = CompileShader(gl, fragmentSource, ShaderType.FragmentShader, result);
                    if (fragmentShader == 0)
                    {
                        gl.DeleteShader(vertexShader);

                        result.Success = false;
                        result.Message = "Fail compiling #fragment shader";
                        return result;
                    }

                    result.Log.Append("Starting creating shader programm: ");
                    uint program = gl.CreateProgram();
                    gl.AttachShader(program, vertexShader);
                    gl.AttachShader(program, fragmentShader);
                    gl.LinkProgram(program);

                    gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out int linkStatus);
                    if (linkStatus == 0)
                    {
                        string linkLog = gl.GetProgramInfoLog(program);

                        gl.DeleteShader(vertexShader);
                        gl.DeleteShader(fragmentShader);
                        gl.DeleteProgram(program);

                        result.Success = false;
                        result.Log.AppendLine($"Error linking programm: {linkLog}");
                        result.Message = "Error linking programm: " + linkLog;
                        return result;
                    }
                    result.Log.AppendLine("Done");

                    CacheAttributes(gl, program, result);
                    CacheUniforms(gl, program, result);

                    gl.DeleteShader(vertexShader);
                    gl.DeleteShader(fragmentShader);
                    gl.DeleteProgram(program);

                    result.Success = true;
                    result.Message = "Shader succefully compiled";
                    return result;
                }
                else
                {
                    result.Success = false;
                    result.Message = "No access to GL context";
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = "Произошла ошибка: " + ex.Message;
                return result;
            }
            finally
            {
                gl?.Dispose();
            }
        }

        private static uint CompileShader(GL gl, string source, ShaderType type, CompilationResult result)
        {
            result.Log.Append($"Cheking ${type} comlilation: ");
            try
            {
                uint shader = gl.CreateShader(type);
                gl.ShaderSource(shader, source);
                gl.CompileShader(shader);

                gl.GetShader(shader, ShaderParameterName.CompileStatus, out int status);
                if (status == 0)
                {
                    string log = gl.GetShaderInfoLog(shader);
                    result.Log.AppendLine($" {log}");
                    gl.GetShader(shader, ShaderParameterName.InfoLogLength, out int logLength);
                    if (logLength > 0)
                    {
                        string log6 = gl.GetShaderInfoLog(shader);
                    }

                    gl.DeleteShader(shader);
                    return 0;
                }
                result.Log.AppendLine(" Done");
                return shader;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Исключение при компиляции {type}: {ex.Message}");
                return 0;
            }
        }
        private static void CacheAttributes(GL gl, uint handle, CompilationResult result)
        {
            gl.GetProgram(handle, GLEnum.ActiveAttributes, out int attributeCount);

            result.Log.AppendLine($"======== ATTRIBUTES ======");
            for (uint i = 0; i < attributeCount; i++)
            {
                string attributeName = gl.GetActiveAttrib(handle, i, out int size, out AttributeType type);
                uint location = (uint)gl.GetAttribLocation(handle, attributeName);
                result.Log.AppendLine($"Attribute:{attributeName} Location:{location} Type:{type}");
            }
            result.Log.AppendLine("\n"); 
        }
        private static void CacheUniforms(GL gl, uint handle, CompilationResult result)
        {
            result.Log.AppendLine($"======== UNIFORMS ======");
            gl.GetProgram(handle, GLEnum.ActiveUniforms, out int uniformCount);

            for (int i = 0; i < uniformCount; i++)
            {
                string uniformName = gl.GetActiveUniform(handle, (uint)i, out int size, out UniformType type);
                int location = gl.GetUniformLocation(handle, uniformName);

                result.Log.AppendLine($"Name:{uniformName} Location:{location} Size:{size} Type:{type}");
            }
            result.Log.AppendLine($"===================");
        }
    }


    public class CompilationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public StringBuilder Log { get; set; } = new StringBuilder();

        public override string ToString()
        {
            return $"{(Success ? "Успех" : "Ошибка")}: {Message}\n{Log.ToString()}";
        }
    }
}
