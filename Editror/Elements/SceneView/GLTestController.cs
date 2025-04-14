using Avalonia.OpenGL.Controls;
using Avalonia.OpenGL;
using System;
using Silk.NET.OpenGL;
using AtomEngine;
using System.Diagnostics;

namespace Editor
{
    internal class GLTestController : OpenGlControlBase
    {
        private GL _gl;
        private uint _vbo;
        private uint _vao;
        private uint _shader;
        private float _rotationAngle = 0.0f;
        private Stopwatch _stopwatch;
        private bool _isDisposed = false;

        protected override void OnOpenGlInit(GlInterface gl)
        {
            base.OnOpenGlInit(gl);
            DebLogger.Info("Open gl init");

            _gl = GL.GetApi(gl.GetProcAddress);

            if (_gl == null)
            {
                DebLogger.Error("Не удалось получить GL API");
                return;
            }
            _isDisposed = false;

            _stopwatch = new Stopwatch();
            _stopwatch.Start();

            float[] vertices = {
             0.0f,  0.5f, 0.0f,  
            -0.5f, -0.5f, 0.0f,  
             0.5f, -0.5f, 0.0f   
        };

            _vao = _gl.GenVertexArray();
            _gl.BindVertexArray(_vao);

            _vbo = _gl.GenBuffer();
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);

            _gl.BufferData(BufferTargetARB.ArrayBuffer, (ReadOnlySpan<float>)vertices, BufferUsageARB.StaticDraw);

            _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            _gl.EnableVertexAttribArray(0);

            string vertexShaderSource =
    @"#version 330 core
layout (location = 0) in vec3 aPosition;
uniform mat4 transform;
void main()
{
    gl_Position = transform * vec4(aPosition, 1.0);
}";

            uint vertexShader = _gl.CreateShader(ShaderType.VertexShader);
            _gl.ShaderSource(vertexShader, vertexShaderSource);
            _gl.CompileShader(vertexShader);
            CheckShaderCompileErrors(vertexShader);

            // Фрагментный шейдер
            string fragmentShaderSource =
    @"#version 330 core
out vec4 FragColor;
void main()
{
    FragColor = vec4(1.0, 0.5, 0.2, 1.0);
}";

            uint fragmentShader = _gl.CreateShader(ShaderType.FragmentShader);
            _gl.ShaderSource(fragmentShader, fragmentShaderSource);
            _gl.CompileShader(fragmentShader);
            CheckShaderCompileErrors(fragmentShader);

            _shader = _gl.CreateProgram();
            _gl.AttachShader(_shader, vertexShader);
            _gl.AttachShader(_shader, fragmentShader);
            _gl.LinkProgram(_shader);
            CheckProgramLinkErrors(_shader);

            _gl.DeleteShader(vertexShader);
            _gl.DeleteShader(fragmentShader);
        }

        protected override void OnOpenGlDeinit(GlInterface gl)
        {
            if (_isDisposed) return;

            DebLogger.Info("Open gl DEinit");
            try
            {
                _gl.DeleteVertexArray(_vao);
                _gl.DeleteBuffer(_vbo);
                _gl.DeleteProgram(_shader);
                _isDisposed = true;
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при деинициализации OpenGL: {ex}");
            }

            base.OnOpenGlDeinit(gl);
        }

        protected override unsafe void OnOpenGlRender(GlInterface gl, int fb)
        {
            if (_isDisposed || _gl == null) return;

            try
            {
                _rotationAngle = (float)(_stopwatch.ElapsedMilliseconds % 3600) / 10.0f;

                _gl.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
                _gl.Clear((uint)ClearBufferMask.ColorBufferBit);

                _gl.UseProgram(_shader);

                float[] transformMatrix = CreateRotationMatrix(_rotationAngle);

                int transformLoc = _gl.GetUniformLocation(_shader, "transform");
                //_gl.UniformMatrix4(transformLoc, 1, false, transformMatrix);
                fixed (float* ptr = transformMatrix)
                {
                    _gl.UniformMatrix4(transformLoc, 1, false, ptr);
                }

                _gl.BindVertexArray(_vao);
                _gl.DrawArrays(PrimitiveType.Triangles, 0, 3);

                //InvalidateVisual();
                RequestNextFrameRendering();
            }
            catch(Exception ex)
            {
                DebLogger.Error("Ошибка рендера: " + ex);
            }
        }

        private float[] CreateRotationMatrix(float angleDegrees)
        {
            float angleRadians = angleDegrees * (float)Math.PI / 180.0f;
            float cos = (float)Math.Cos(angleRadians);
            float sin = (float)Math.Sin(angleRadians);

            return new float[]
            {
            cos,  -sin, 0.0f, 0.0f,
            sin,  cos, 0.0f, 0.0f,
            0.0f, 0.0f, 1.0f, 0.0f,
            0.0f, 0.0f, 0.0f, 1.0f
            };
        }


        private void CheckShaderCompileErrors(uint shader)
        {
            _gl.GetShader(shader, ShaderParameterName.CompileStatus, out int success);
            if (success == 0)
            {
                string infoLog = _gl.GetShaderInfoLog(shader);
                DebLogger.Error($"Ошибка компиляции шейдера: {infoLog}");
            }
        }

        private void CheckProgramLinkErrors(uint program)
        {
            _gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out int success);
            if (success == 0)
            {
                string infoLog = _gl.GetProgramInfoLog(program);
                DebLogger.Error($"Ошибка линковки программы: {infoLog}");
            }
        }

    }

}
