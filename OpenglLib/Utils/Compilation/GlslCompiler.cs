using Silk.NET.Windowing;
using Silk.NET.OpenGL;
using System.Text;
using EngineLib;
using AtomEngine;

namespace OpenglLib
{
    public static class GlslCompiler
    {
        public static Action<CompilationGlslCodeResult>? OnCompiled;

        public unsafe static CompilationGlslCodeResult TryToCompile(FileEvent e, bool withAvoke = true)
        {
            CompilationGlslCodeResult result = new CompilationGlslCodeResult();
            result.File = e;
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

                var shader = GlslExtractor.ExtractShaderModel(e.FileFullPath);
                result.ShadeModel = shader;

                string vertexSource = shader.Vertex.FullText;
                string fragmentSource = shader.Fragment.FullText;

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

                    result.ShaderVersion = (*shaderVersion).ToString();
                    result.GlVersion = (*glVersion).ToString();

                    uint vertexShader = CompileShader(gl, vertexSource, ShaderType.VertexShader, result);
                    if (vertexShader == 0)
                    {
                        result.Success = false;
                        result.Message = "Fail compiling #vertex shader";
                        result.Log.AppendLine(vertexSource);
                        return result;
                    }
                    result.VertexIsSucces = true;

                    uint fragmentShader = CompileShader(gl, fragmentSource, ShaderType.FragmentShader, result);
                    if (fragmentShader == 0)
                    {
                        gl.DeleteShader(vertexShader);

                        result.Success = false;
                        result.Message = "Fail compiling #fragment shader";
                        result.Log.AppendLine(fragmentSource);
                        return result;
                    }
                    result.FragmentIsSucces = true;

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
                    CacheUniformBlocks(gl, program, result);
                    CacheSamplerUniforms(gl, program, result, vertexSource, fragmentSource);

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
                result.Message = ex.Message;
                return result;
            }
            finally
            {
                gl?.Dispose();
                if (withAvoke) OnCompiled?.Invoke(result);
            }
        }

        private static uint CompileShader(GL gl, string source, ShaderType type, CompilationGlslCodeResult result)
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
                DebLogger.Error($"Исключение при компиляции {type}: {ex.Message}");
                return 0;
            }
        }
        private static void CacheAttributes(GL gl, uint handle, CompilationGlslCodeResult result)
        {
            Shader.CacheAttributes(gl, handle, result.AttributeLocations);
            result.Log.AppendLine($"======== ATTRIBUTES ======");
            foreach (var attribute in result.AttributeLocations)
            {
                result.Log.AppendLine($"Attribute:{attribute.Key} Location:{attribute.Value}");
            }

        }
        private static void CacheUniforms(GL gl, uint handle, CompilationGlslCodeResult result)
        {
            Shader.CacheUniforms(gl, handle, result.UniformLocations, result.UniformInfo);

            result.Log.AppendLine($"======== UNIFORMS ======");
            foreach (var kvp in result.UniformInfo)
            {
                result.Log.AppendLine($"Name:{kvp.Key} Location:{kvp.Value.Location} Size:{kvp.Value.Size} Type:{kvp.Value.Type}");
            }
            result.Log.AppendLine($"===================");
        }
        private static void CacheUniformBlocks(GL gl, uint handle, CompilationGlslCodeResult result)
        {
            Shader.CacheUniformBlocks(gl, handle, result.UniformBlocks);

            result.Log.AppendLine($"======== UNIFORM BLOCKS ======");
            foreach (var uniform in result.UniformBlocks)
            {
                result.Log.AppendLine($"Name:{uniform.Name} BlockIndex:{uniform.BlockIndex} Size:{uniform.BlockSize} ActiveUniforms:{uniform.ActiveUniforms}");
            }
            result.Log.AppendLine($"===================");

        }
        private static void CacheSamplerUniforms(GL gl, uint handle, CompilationGlslCodeResult result, string verterSource, string fragmentSource)
        {
            Shader.CacheSamplerUniforms(gl, handle, result.SamplerInfo, result.UniformLocations, verterSource, fragmentSource);

            result.Log.AppendLine($"======== UNIFORM BLOCKS ======");
            foreach (var kvp in result.SamplerInfo)
            {
                result.Log.AppendLine($"Name:{kvp.Key} Location:{kvp.Value.Location} Size:{kvp.Value.Size} Type:{kvp.Value.Type}");
            }
            result.Log.AppendLine($"===================");

        }

    }

    public class CompilationGlslCodeResult
    {
        public readonly Dictionary<string, int> UniformLocations = new Dictionary<string, int>();
        public readonly Dictionary<string, uint> AttributeLocations = new Dictionary<string, uint>();
        public readonly Dictionary<string, UniformInfo> UniformInfo = new Dictionary<string, UniformInfo>();
        public readonly Dictionary<string, UniformSamplerInfo> SamplerInfo = new Dictionary<string, UniformSamplerInfo>();
        public readonly List<UniformBlockData> UniformBlocks = new List<UniformBlockData>();
        public bool Success { get; set; }
        public string ShaderVersion { get; set; } = string.Empty;
        public string GlVersion { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool VertexIsSucces { get; set; } = false;
        public bool FragmentIsSucces { get; set; } = false;
        public GlslShaderModel ShadeModel { get; set; }
        public FileEvent? File { get; set; }
        public StringBuilder Log { get; set; } = new StringBuilder();



        public override string ToString()
        {
            return $"{(Success ? "Успех" : "Ошибка")}: {Message}\n{Log.ToString()}";
        }
    }
}
