using System.Collections.Generic;
using Silk.NET.Windowing;
using Silk.NET.OpenGL;
using System.Text;
using OpenglLib;
using System.IO;
using System;

namespace Editor
{
    internal static class GlslCompiler
    {
        public static Action<CompilationGlslCodeResult>? OnCompiled;

        internal unsafe static CompilationGlslCodeResult TryToCompile(FileEvent e, bool withAvoke = true)
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

                result.AttributeLocations[attributeName] = location;
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

                result.UniformLocations[uniformName] = location;
                result.UniformInfo[uniformName] = new UniformInfo
                {
                    Location = location,
                    Size = size,
                    Type = type,
                    Name = uniformName
                };

                result.Log.AppendLine($"Name:{uniformName} Location:{location} Size:{size} Type:{type}");
            }
            result.Log.AppendLine($"===================");
        }
        private static unsafe void CacheUniformBlocks(GL gl, uint handle, CompilationGlslCodeResult result)
        {
            //gl.GetProgram(handle, GLEnum.ActiveUniformBlocks, out int uniformBlockCount);

            //for (uint i = 0; i < uniformBlockCount; i++)
            //{
            //    byte[] nameBuffer = new byte[256];
            //    uint nameLength = 0;

            //    fixed (byte* namePtr = nameBuffer)
            //    {
            //        gl.GetActiveUniformBlockName(handle, i, (uint)nameBuffer.Length, &nameLength, namePtr);
            //        string name = Encoding.ASCII.GetString(nameBuffer, 0, (int)nameLength);
            //        uint blockIndex = gl.GetUniformBlockIndex(handle, name);
            //        result.UniformBlocks.Add(new UniformBlockData(name, blockIndex));
            //    }
            //}

            gl.GetProgram(handle, GLEnum.ActiveUniformBlocks, out int uniformBlockCount);
            for (uint i = 0; i < uniformBlockCount; i++)
            {
                byte[] nameBuffer = new byte[256];
                uint nameLength = 0;
                fixed (byte* namePtr = nameBuffer)
                {
                    gl.GetActiveUniformBlockName(handle, i, (uint)nameBuffer.Length, &nameLength, namePtr);
                    string name = Encoding.ASCII.GetString(nameBuffer, 0, (int)nameLength);
                    uint blockIndex = gl.GetUniformBlockIndex(handle, name);

                    int blockSize = 0;
                    gl.GetActiveUniformBlock(handle, i, GLEnum.UniformBlockDataSize, &blockSize);

                    int activeUniforms = 0;
                    gl.GetActiveUniformBlock(handle, i, GLEnum.UniformBlockActiveUniforms, &activeUniforms);

                    int[] uniformIndices = new int[activeUniforms];
                    gl.GetActiveUniformBlock(handle, i, GLEnum.UniformBlockActiveUniformIndices, uniformIndices);

                    List<UniformMemberData> members = new List<UniformMemberData>();

                    for (int j = 0; j < uniformIndices.Length; j++)
                    {
                        int uniformIndex = uniformIndices[j];

                        byte[] uniformNameBuffer = new byte[256];
                        uint uniformNameLength = 0;
                        fixed (byte* uniformNamePtr = uniformNameBuffer)
                        {
                            gl.GetActiveUniformName(handle, (uint)uniformIndex, (uint)uniformNameBuffer.Length, &uniformNameLength, uniformNamePtr);
                            string uniformName = Encoding.ASCII.GetString(uniformNameBuffer, 0, (int)uniformNameLength);

                            int[] offsets = new int[1];
                            fixed (int* offsetsPtr = offsets)
                            {
                                uint[] indices = new uint[] { (uint)uniformIndex };
                                fixed (uint* indicesPtr = indices)
                                {
                                    gl.GetActiveUniforms(handle, 1, indicesPtr, GLEnum.UniformOffset, offsetsPtr);
                                }
                            }
                            int offset = offsets[0];

                            gl.GetActiveUniform(handle, (uint)uniformIndex, out int size, out UniformType type);

                            int[] arrayStrides = new int[1];
                            fixed (int* arrayStridesPtr = arrayStrides)
                            {
                                uint[] indices = new uint[] { (uint)uniformIndex };
                                fixed (uint* indicesPtr = indices)
                                {
                                    gl.GetActiveUniforms(handle, 1, indicesPtr, GLEnum.UniformArrayStride, arrayStridesPtr);
                                }
                            }
                            int arrayStride = arrayStrides[0];

                            int[] matrixStrides = new int[1];
                            fixed (int* matrixStridesPtr = matrixStrides)
                            {
                                uint[] indices = new uint[] { (uint)uniformIndex };
                                fixed (uint* indicesPtr = indices)
                                {
                                    gl.GetActiveUniforms(handle, 1, indicesPtr, GLEnum.UniformMatrixStride, matrixStridesPtr);
                                }
                            }
                            int matrixStride = matrixStrides[0];

                            members.Add(new UniformMemberData(
                                uniformName,
                                (uint)uniformIndex,
                                offset,
                                size,
                                type,
                                arrayStride,
                                matrixStride
                            ));
                        }
                    }

                    var blockData = new UniformBlockData(
                        name,
                        blockIndex,
                        blockSize,
                        activeUniforms,
                        members
                    );

                    result.UniformBlocks.Add(blockData);
                }
            }

        }

    }

    public class CompilationGlslCodeResult
    {
        public readonly Dictionary<string, int> UniformLocations = new Dictionary<string, int>();
        public readonly Dictionary<string, uint> AttributeLocations = new Dictionary<string, uint>();
        public readonly Dictionary<string, UniformInfo> UniformInfo = new Dictionary<string, UniformInfo>();
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
