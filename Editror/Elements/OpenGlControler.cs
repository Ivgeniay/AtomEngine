using Avalonia.OpenGL.Controls;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.OpenGL;
using Silk.NET.OpenGL;
using System;
using Avalonia;
using AtomEngine;

namespace Editor
{
    public class OpenGlController : OpenGlControlBase, IWindowed
    {
        public Action<object> OnClose { get; set; }

        private GL _gl;
        private uint _program;
        private uint _vao;
        private uint _vbo;
        private Stopwatch _stopwatch;
        private bool _initialized = false;
        private bool _isGLES = false;
        private Size _lastSize;


        private const string VertexShaderSourceGL = @"
            #version 330
            in vec3 aPosition;
            
            void main()
            {
                gl_Position = vec4(aPosition, 1.0);
            }
        ";

        private const string FragmentShaderSourceGL = @"
            #version 330
            out vec4 FragColor;
            
            uniform vec4 uColor;
            
            void main()
            {
                FragColor = uColor;
            }
        ";

        private readonly float[] _vertices = {
             0.0f,  0.5f, 0.0f,
            -0.5f, -0.5f, 0.0f,
             0.5f, -0.5f, 0.0f
        };

        public OpenGlController()
        {
            _stopwatch = new Stopwatch();
            _stopwatch.Start();

            PropertyChanged += OnPropertyChangedHandler;
        }

        private void OnPropertyChangedHandler(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == BoundsProperty)
            {
                //RequestNextFrameRendering();
            }
        }

        protected override void OnOpenGlInit(GlInterface gl)
        {
            try
            {
                _gl = GL.GetApi(gl.GetProcAddress);
                _program = CreateShaderProgram();
                _vao = _gl.GenVertexArray();
                _vbo = _gl.GenBuffer();

                _gl.BindVertexArray(_vao);
                _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
                unsafe
                {
                    fixed (float* v = _vertices)
                    {
                        _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(_vertices.Length * sizeof(float)), v, BufferUsageARB.StaticDraw);
                    }
                }

                _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), IntPtr.Zero);
                _gl.EnableVertexAttribArray(0);

                _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
                _gl.BindVertexArray(0);

                GLEnum error = (GLEnum)_gl.GetError();
                if (error != GLEnum.NoError)
                {
                    DebLogger.Error($"OpenGL error during initialization: {error}");
                }
                else
                {
                    _initialized = true;
                    DebLogger.Info("OpenGL initialization successful");
                }

                _lastSize = Bounds.Size;
            }
            catch (Exception ex)
            {
                DebLogger.Error($"OpenGL initialization error: {ex.Message}");
                DebLogger.Error(ex.StackTrace);
            }
        }

        protected override void OnOpenGlDeinit(GlInterface gl)
        {
            if (!_initialized) return;

            try
            {
                _gl.DeleteBuffer(_vbo);
                _gl.DeleteVertexArray(_vao);
                _gl.DeleteProgram(_program);
                _initialized = false;
            }
            catch (Exception ex)
            {
                DebLogger.Error($"OpenGL deinitialization error: {ex.Message}");
            }
        }

        protected override void OnOpenGlRender(GlInterface gl, int fb)
        {
            if (!_initialized)
            {
                DebLogger.Warn("Skipping render: OpenGL not initialized properly");
                return;
            }

            try
            {
                var currentSize = Bounds.Size;
                var pixelSize = new Size(
                    Math.Max(1, currentSize.Width),
                    Math.Max(1, currentSize.Height));

                bool sizeChanged = !_lastSize.Equals(currentSize);
                if (sizeChanged)
                {
                    Debug.WriteLine($"Size changed: {_lastSize} -> {currentSize}");
                    _lastSize = currentSize;
                }

                _gl.Viewport(0, 0, (uint)pixelSize.Width, (uint)pixelSize.Height);

                _gl.Enable(EnableCap.Blend);
                _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

                _gl.ClearColor(0.2f, 0.2f, 0.2f, 1.0f);
                _gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));

                _gl.UseProgram(_program);

                float time = (float)_stopwatch.Elapsed.TotalSeconds;
                float r = (MathF.Sin(time) + 1.0f) / 2.0f;
                float g = (MathF.Sin(time + 2.0f) + 1.0f) / 2.0f;
                float b = (MathF.Sin(time + 4.0f) + 1.0f) / 2.0f;

                int colorLoc = _gl.GetUniformLocation(_program, "uColor");
                _gl.Uniform4(colorLoc, r, g, b, 1.0f);

                _gl.BindVertexArray(_vao);
                _gl.DrawArrays(PrimitiveType.Triangles, 0, 3);
                _gl.BindVertexArray(0);

                GLEnum error = (GLEnum)_gl.GetError();
                if (error != GLEnum.NoError)
                {
                    DebLogger.Error($"OpenGL error during rendering: {error}");
                }

                this.RequestNextFrameRendering();
            }
            catch (Exception ex)
            {
                DebLogger.Error($"OpenGL render error: {ex.Message}");
                DebLogger.Error(ex.StackTrace);
            }
        }

        private uint CreateShaderProgram()
        {
            string vertexSource = VertexShaderSourceGL;
            string fragmentSource = FragmentShaderSourceGL;

            uint vertexShader = _gl.CreateShader(ShaderType.VertexShader);
            _gl.ShaderSource(vertexShader, vertexSource);
            _gl.CompileShader(vertexShader);
            CheckShaderCompilation(vertexShader, "vertex");

            uint fragmentShader = _gl.CreateShader(ShaderType.FragmentShader);
            _gl.ShaderSource(fragmentShader, fragmentSource);
            _gl.CompileShader(fragmentShader);
            CheckShaderCompilation(fragmentShader, "fragment");

            uint program = _gl.CreateProgram();
            _gl.AttachShader(program, vertexShader);
            _gl.AttachShader(program, fragmentShader);

            _gl.BindAttribLocation(program, 0, "aPosition");

            _gl.LinkProgram(program);
            CheckProgramLinking(program);

            _gl.DeleteShader(vertexShader);
            _gl.DeleteShader(fragmentShader);

            return program;
        }

        private void CheckShaderCompilation(uint shader, string type)
        {
            _gl.GetShader(shader, ShaderParameterName.CompileStatus, out int status);
            if (status != (int)GLEnum.True)
            {
                string infoLog = _gl.GetShaderInfoLog(shader);
                DebLogger.Error($"{type} shader compilation failed: {infoLog}");
                throw new Exception($"{type} shader compilation failed: {infoLog}");
            }
            else
            {
                DebLogger.Info($"{type} shader compiled successfully");
            }
        }

        private void CheckProgramLinking(uint program)
        {
            _gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out int status);
            if (status != (int)GLEnum.True)
            {
                string infoLog = _gl.GetProgramInfoLog(program);
                DebLogger.Error($"Program linking failed: {infoLog}");
                throw new Exception($"Program linking failed: {infoLog}");
            }
            else
            {
                DebLogger.Info("Program linked successfully");
            }
        }

        protected override void OnSizeChanged(SizeChangedEventArgs e)
        {
            base.OnSizeChanged(e);
            DebLogger.Info($"OnSizeChanged: {e.PreviousSize} -> {e.NewSize}");
            this.RequestNextFrameRendering();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            PropertyChanged -= OnPropertyChangedHandler;
        }

        public void Dispose()
        {
            OnClose?.Invoke(this);
            _gl.DeleteBuffer(_vbo);
            _gl.DeleteVertexArray(_vao);
            _gl.DeleteProgram(_program);
            _initialized = false;
            _gl.Dispose();
        }
    }
}