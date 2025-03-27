using Silk.NET.Windowing;
using Silk.NET.OpenGL;
using System.IO;
using System;
using System.Text;
using OpenglLib;

namespace Editor
{
    internal static class GlslCompiler
    {
        internal unsafe static CompilationGlslCodeResult TryToCompile(FileEvent e)
        {
            CompilationGlslCodeResult result = new CompilationGlslCodeResult();
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

                shaderSource = GlslParser.ProcessIncludesRecursively(shaderSource, e.FileFullPath);
                shaderSource = RSParser.RemoveServiceMarkers(shaderSource);
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
                        result.Log.AppendLine(vertexSource);
                        return result;
                    }

                    uint fragmentShader = CompileShader(gl, fragmentSource, ShaderType.FragmentShader, result);
                    if (fragmentShader == 0)
                    {
                        gl.DeleteShader(vertexShader);

                        result.Success = false;
                        result.Message = "Fail compiling #fragment shader";
                        result.Log.AppendLine(fragmentSource);
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
                Console.WriteLine($"Исключение при компиляции {type}: {ex.Message}");
                return 0;
            }
        }
        private static void CacheAttributes(GL gl, uint handle, CompilationGlslCodeResult result)
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
        private static void CacheUniforms(GL gl, uint handle, CompilationGlslCodeResult result)
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

    public class CompilationGlslCodeResult
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
