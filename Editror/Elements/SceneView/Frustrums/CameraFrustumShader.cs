using System.Numerics;
using Silk.NET.OpenGL;
using System;
using AtomEngine;

namespace Editor
{
    internal class CameraFrustumShader : IDisposable
    {
        private uint _program;
        private uint _vao;
        private uint _vbo;
        private uint _ebo;
        private GL _gl;
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

        public CameraFrustumShader(GL gl)
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
            uint[] frustumIndices = {
        // Ближняя плоскость
        0, 1, 1, 2, 2, 3, 3, 0,
        // Дальняя плоскость
        4, 5, 5, 6, 6, 7, 7, 4,
        // Соединения между плоскостями
        0, 4, 1, 5, 2, 6, 3, 7
    };

            _indexCount = frustumIndices.Length;

            DebLogger.Info($"Создание буферов для фрустума. Количество индексов: {_indexCount}");

            _vao = _gl.GenVertexArray();
            _vbo = _gl.GenBuffer();
            _ebo = _gl.GenBuffer();

            _gl.BindVertexArray(_vao);

            float[] initialVertices = new float[8 * 3];
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
            fixed (float* v = initialVertices)
            {
                _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(initialVertices.Length * sizeof(float)), v, BufferUsageARB.DynamicDraw);
            }

            _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);
            _gl.EnableVertexAttribArray(0);

            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
            fixed (uint* i = frustumIndices)
            {
                _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(frustumIndices.Length * sizeof(uint)), i, BufferUsageARB.StaticDraw);
            }

            int bufferSize;
            _gl.GetBufferParameter(BufferTargetARB.ElementArrayBuffer, BufferPNameARB.BufferSize, out bufferSize);
            DebLogger.Info($"Размер индексного буфера после инициализации: {bufferSize} байт");

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

        public unsafe void UpdateFrustumVertices(Vector3[] frustumCorners)
        {
            if (frustumCorners.Length != 8)
            {
                throw new ArgumentException("Фрустум должен содержать 8 вершин");
            }

            float[] vertexData = new float[frustumCorners.Length * 3];
            for (int i = 0; i < frustumCorners.Length; i++)
            {
                vertexData[i * 3] = frustumCorners[i].X;
                vertexData[i * 3 + 1] = frustumCorners[i].Y;
                vertexData[i * 3 + 2] = frustumCorners[i].Z;
            }

            Use();

            _gl.BindVertexArray(_vao);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);

            unsafe
            {
                fixed (float* v = vertexData)
                {
                    _gl.BufferSubData(BufferTargetARB.ArrayBuffer, 0, (nuint)(vertexData.Length * sizeof(float)), v);
                }
            }

            _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);
            _gl.EnableVertexAttribArray(0);

            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
            _gl.BindVertexArray(0);
        }

        public unsafe void Draw()
        {
            Use();

            _gl.BindVertexArray(_vao);
            _gl.LineWidth(1.0f);
            _gl.DrawElements(PrimitiveType.Lines, (uint)_indexCount, DrawElementsType.UnsignedInt, (void*)0);
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