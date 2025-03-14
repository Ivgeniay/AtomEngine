using Silk.NET.OpenGL;
using System.Numerics;
using System;

namespace Editor
{
    internal class AABBShader : IDisposable
    {
        private uint _program;
        private uint _vao;
        private uint _vbo;
        private uint _ebo;
        private GL _gl;
        private int _vertexCount;
        private int _indexCount;

        private const string VertexShaderSource = @"
            #version 330 core
            layout (location = 0) in vec3 aPosition;
            
            uniform mat4 model;
            uniform mat4 view;
            uniform mat4 projection;
            
            void main()
            {
                gl_Position = projection * view * model * vec4(aPosition, 1.0);
            }
        ";

        private const string FragmentShaderSource = @"
            #version 330 core
            out vec4 FragColor;
            
            uniform vec4 color;
            
            void main()
            {
                FragColor = color;
            }
        ";

        public AABBShader(GL gl)
        {
            _gl = gl;
            InitShader();
            SetupBuffers();
        }

        private void InitShader()
        {
            uint vertexShader = _gl.CreateShader(ShaderType.VertexShader);
            _gl.ShaderSource(vertexShader, VertexShaderSource);
            _gl.CompileShader(vertexShader);
            CheckShaderCompilation(vertexShader);

            uint fragmentShader = _gl.CreateShader(ShaderType.FragmentShader);
            _gl.ShaderSource(fragmentShader, FragmentShaderSource);
            _gl.CompileShader(fragmentShader);
            CheckShaderCompilation(fragmentShader);

            _program = _gl.CreateProgram();
            _gl.AttachShader(_program, vertexShader);
            _gl.AttachShader(_program, fragmentShader);
            _gl.LinkProgram(_program);
            CheckProgramLinking(_program);

            _gl.DeleteShader(vertexShader);
            _gl.DeleteShader(fragmentShader);
        }

        private void CheckShaderCompilation(uint shader)
        {
            _gl.GetShader(shader, ShaderParameterName.CompileStatus, out var status);
            if (status != (int)GLEnum.True)
            {
                string infoLog = _gl.GetShaderInfoLog(shader);
                throw new Exception($"Ошибка компиляции шейдера: {infoLog}");
            }
        }

        private void CheckProgramLinking(uint program)
        {
            _gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out var status);
            if (status != (int)GLEnum.True)
            {
                string infoLog = _gl.GetProgramInfoLog(program);
                throw new Exception($"Ошибка линковки программы: {infoLog}");
            }
        }

        private unsafe void SetupBuffers()
        {
            _vao = _gl.GenVertexArray();
            _vbo = _gl.GenBuffer();
            _ebo = _gl.GenBuffer();

            float[] cubeVertices = {
                -0.5f, -0.5f,  0.5f,
                 0.5f, -0.5f,  0.5f,
                 0.5f,  0.5f,  0.5f,
                -0.5f,  0.5f,  0.5f,
                
                -0.5f, -0.5f, -0.5f,
                 0.5f, -0.5f, -0.5f,
                 0.5f,  0.5f, -0.5f,
                -0.5f,  0.5f, -0.5f,
            };

            uint[] cubeIndices = {
                0, 1, 1, 2, 2, 3, 3, 0,
                4, 5, 5, 6, 6, 7, 7, 4,
                0, 4, 1, 5, 2, 6, 3, 7
            };

            _vertexCount = cubeVertices.Length / 3;
            _indexCount = cubeIndices.Length;

            _gl.BindVertexArray(_vao);

            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
            unsafe
            {
                fixed (float* v = cubeVertices)
                {
                    _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(cubeVertices.Length * sizeof(float)), v, BufferUsageARB.StaticDraw);
                }
            }

            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
            unsafe
            {
                fixed (uint* i = cubeIndices)
                {
                    _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(cubeIndices.Length * sizeof(uint)), i, BufferUsageARB.StaticDraw);
                }
            }

            _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);
            _gl.EnableVertexAttribArray(0);

            _gl.BindVertexArray(0);
        }

        public void Use()
        {
            _gl.UseProgram(_program);
        }

        public void SetMVP(Matrix4x4 model, Matrix4x4 view, Matrix4x4 projection)
        {
            int modelLoc = _gl.GetUniformLocation(_program, "model");
            int viewLoc = _gl.GetUniformLocation(_program, "view");
            int projLoc = _gl.GetUniformLocation(_program, "projection");

            unsafe
            {
                _gl.UniformMatrix4(modelLoc, 1, false, (float*)&model);
                _gl.UniformMatrix4(viewLoc, 1, false, (float*)&view);
                _gl.UniformMatrix4(projLoc, 1, false, (float*)&projection);
            }
        }

        public void SetColor(Vector4 color)
        {
            int colorLoc = _gl.GetUniformLocation(_program, "color");
            _gl.Uniform4(colorLoc, color.X, color.Y, color.Z, color.W);
        }

        public unsafe void DrawAABB(Matrix4x4 modelMatrix, Vector3 min, Vector3 max, Vector4 color, Matrix4x4 view, Matrix4x4 projection)
        {
            Use();

            Vector3 center = (min + max) * 0.5f;
            Vector3 scale = max - min;
            Matrix4x4 aabbModel = Matrix4x4.CreateScale(scale) * Matrix4x4.CreateTranslation(center);

            SetMVP(aabbModel, view, projection);
            SetColor(color);

            _gl.BindVertexArray(_vao);
            _gl.PolygonMode(GLEnum.FrontAndBack, GLEnum.Line);
            _gl.LineWidth(1.0f);

            _gl.DrawElements(PrimitiveType.Lines, (uint)_indexCount, DrawElementsType.UnsignedInt, (void*)0);
            _gl.PolygonMode(GLEnum.FrontAndBack, GLEnum.Fill);

            _gl.BindVertexArray(0);
        }

        public void Dispose()
        {
            _gl.DeleteBuffer(_vbo);
            _gl.DeleteBuffer(_ebo);
            _gl.DeleteVertexArray(_vao);
            _gl.DeleteProgram(_program);
        }
    }
}