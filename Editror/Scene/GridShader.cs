using System.Numerics;
using Silk.NET.OpenGL;
using System;
using AtomEngine;

namespace Editor
{
    public class GridShader : OpenglLib.Mat
    {
        private readonly string _vertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 aPosition;
        layout (location = 1) in vec3 aColor;
        
        out vec3 outColor;
        
        uniform mat4 model;
        uniform mat4 view;
        uniform mat4 projection;
        
        void main()
        {
            gl_Position = projection * view * model * vec4(aPosition, 1.0);
            outColor = aColor;
        }
        ";
        private readonly string _fragmentShaderSource = @"
        #version 330 core
        in vec3 outColor;
        out vec4 FragColor;
        
        void main()
        {
            FragColor = vec4(outColor, 1.0);
        }
        ";

        private uint _vao;
        private uint _vbo;
        private uint _ebo;
        private int _indexCount;

        private int _modelLocation;
        private int _viewLocation;
        private int _projectionLocation;

        private bool isCreated = false;
        private bool isDisposed = false;

        public GridShader(GL gl) : base(gl)
        {
            CompileShaders();

            _modelLocation = gl.GetUniformLocation(handle, "model");
            _viewLocation = gl.GetUniformLocation(handle, "view");
            _projectionLocation = gl.GetUniformLocation(handle, "projection");

            CreateGrid(gl, 20, 20, 1.0f);
        }

        private void CompileShaders()
        {
            uint vertexShader = _gl.CreateShader(ShaderType.VertexShader);
            _gl.ShaderSource(vertexShader, _vertexShaderSource);
            _gl.CompileShader(vertexShader);
            CheckShaderCompilation(vertexShader, "вершинного");

            uint fragmentShader = _gl.CreateShader(ShaderType.FragmentShader);
            _gl.ShaderSource(fragmentShader, _fragmentShaderSource);
            _gl.CompileShader(fragmentShader);
            CheckShaderCompilation(fragmentShader, "фрагментного");

            handle = _gl.CreateProgram();
            _gl.AttachShader(handle, vertexShader);
            _gl.AttachShader(handle, fragmentShader);
            _gl.LinkProgram(handle);

            _gl.GetProgram(handle, ProgramPropertyARB.LinkStatus, out var status);
            if (status == 0)
            {
                string infoLog = _gl.GetProgramInfoLog(handle);
                throw new Exception($"Ошибка линковки шейдерной программы: {infoLog}");
            }

            _gl.DeleteShader(vertexShader);
            _gl.DeleteShader(fragmentShader);
        }

        private void CheckShaderCompilation(uint shader, string shaderType)
        {
            _gl.GetShader(shader, ShaderParameterName.CompileStatus, out var status);
            if (status == 0)
            {
                string infoLog = _gl.GetShaderInfoLog(shader);
                throw new Exception($"Ошибка компиляции {shaderType} шейдера: {infoLog}");
            }
        }

        private unsafe void CreateGrid(GL gl, int width, int height, float cellSize)
        {
            // Количество линий сетки
            int numLinesX = width + 1;
            int numLinesZ = height + 1;
            int numVertices = (numLinesX + numLinesZ) * 2;

            // Создаем массив вершин
            float[] vertices = new float[numVertices * 6]; // x, y, z, r, g, b для каждой вершины

            int vertexIndex = 0;
            float halfWidth = width * cellSize / 2.0f;
            float halfHeight = height * cellSize / 2.0f;

            // Линии вдоль оси X (красный цвет для оси X)
            for (int i = 0; i < numLinesX; i++)
            {
                float x = i * cellSize - halfWidth;
                float r = (i == width / 2) ? 1.0f : 0.5f; // Ось X выделена другим цветом
                float g = (i == width / 2) ? 0.0f : 0.5f;
                float b = (i == width / 2) ? 0.0f : 0.5f;

                // Начало линии
                vertices[vertexIndex++] = x;
                vertices[vertexIndex++] = 0;
                vertices[vertexIndex++] = -halfHeight;
                vertices[vertexIndex++] = r;
                vertices[vertexIndex++] = g;
                vertices[vertexIndex++] = b;

                // Конец линии
                vertices[vertexIndex++] = x;
                vertices[vertexIndex++] = 0;
                vertices[vertexIndex++] = halfHeight;
                vertices[vertexIndex++] = r;
                vertices[vertexIndex++] = g;
                vertices[vertexIndex++] = b;
            }

            // Линии вдоль оси Z (синий цвет для оси Z)
            for (int i = 0; i < numLinesZ; i++)
            {
                float z = i * cellSize - halfHeight;
                float r = (i == height / 2) ? 0.0f : 0.5f;
                float g = (i == height / 2) ? 0.0f : 0.5f;
                float b = (i == height / 2) ? 1.0f : 0.5f; // Ось Z выделена другим цветом

                // Начало линии
                vertices[vertexIndex++] = -halfWidth;
                vertices[vertexIndex++] = 0;
                vertices[vertexIndex++] = z;
                vertices[vertexIndex++] = r;
                vertices[vertexIndex++] = g;
                vertices[vertexIndex++] = b;

                // Конец линии
                vertices[vertexIndex++] = halfWidth;
                vertices[vertexIndex++] = 0;
                vertices[vertexIndex++] = z;
                vertices[vertexIndex++] = r;
                vertices[vertexIndex++] = g;
                vertices[vertexIndex++] = b;
            }

            // Добавляем ось Y (зеленый цвет)
            float[] yAxis = new float[] {
                0, -1, 0, 0, 1, 0,
                0, 10, 0, 0, 1, 0
            };

            // Создаем индексы для линий
            _indexCount = numVertices;
            ushort[] indices = new ushort[_indexCount];
            for (ushort i = 0; i < _indexCount; i++)
            {
                indices[i] = i;
            }

            // Создаем буферы OpenGL
            gl.GenVertexArrays(1, out _vao);
            gl.GenBuffers(1, out _vbo);
            gl.GenBuffers(1, out _ebo);

            gl.BindVertexArray(_vao);

            // Заполняем буфер вершин
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
            unsafe
            {
                fixed (void* data = vertices)
                {
                    gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)), data, BufferUsageARB.StaticDraw);
                }
            }

            // Заполняем буфер индексов
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
            unsafe
            {
                fixed (void* data = indices)
                {
                    gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indices.Length * sizeof(ushort)), data, BufferUsageARB.StaticDraw);
                }
            }

            // Настраиваем формат вершин
            // Позиция
            gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), (void*)0);
            gl.EnableVertexAttribArray(0);

            // Цвет
            gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), (void*)(3 * sizeof(float)));
            gl.EnableVertexAttribArray(1);

            // Отвязываем VAO
            gl.BindVertexArray(0);

            isCreated = true;
        }

        public void SetMVP(Matrix4x4 model, Matrix4x4 view, Matrix4x4 projection)
        {
            // Устанавливаем uniform-переменные
            unsafe
            {
                _gl.UniformMatrix4(_modelLocation, 1, false, (float*)&model);
                _gl.UniformMatrix4(_viewLocation, 1, false, (float*)&view);
                _gl.UniformMatrix4(_projectionLocation, 1, false, (float*)&projection);
            }
        }

        public unsafe void Draw()
        {
            _gl.BindVertexArray(_vao);
            _gl.DrawElements(PrimitiveType.Lines, (uint)_indexCount, DrawElementsType.UnsignedShort, (void*)0);
            _gl.BindVertexArray(0);
        }

        public override void Dispose()
        {
            if (isDisposed || !isCreated) return;

            if (_gl != null)
            {
                //_gl.DeleteVertexArray(_vao);
                //_gl.DeleteBuffer(_vbo);
                //_gl.DeleteBuffer(_ebo);
            }
            isDisposed = true;
        }
    }


}