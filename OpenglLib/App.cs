using Silk.NET.Windowing;
using Silk.NET.OpenGL;
using Silk.NET.Input;
using Silk.NET.Maths;

namespace OpenglLib
{
    public class App : IDisposable
    {
        public GL Gl => _gl;
        public IWindow NativeWindow => _window;
        public IInputContext Input => _input;


        private IWindow? _window;
        private IInputContext? _input;
        private ILogger? _logger;
        private GL? _gl;

        private Queue<double> _fpsHistory = new Queue<double>();
        private const int FPS_SAMPLE_SIZE = 60;
        private bool debug = false;

        public App(AppOptions options)
        {
            this._logger = options.Logger;
            GLSLTypeManager.Instance.Logger = this._logger;
            GLSLTypeManager.Instance.LazyInitializer();

            var win_options = WindowOptions.Default;
            win_options.Size = new Vector2D<int>(options.Width, options.Height);
            win_options.Title = options.Title;
            this.debug = options.Debug;

            if (debug)
            {
                win_options.VSync = false;
                win_options.UpdatesPerSecond = 0;
                win_options.FramesPerSecond = 0;
            }

            _window = Window.Create(win_options);

            _window.Load += OnLoad;
            _window.Update += OnUpdate;
            _window.Render += OnRender;
            _window.Closing += OnClose;
            _window.Resize += OnResize;
        }

        private void OnLoad()
        {
            // Инициализация OpenGL
            _gl = GL.GetApi(_window);
            // Инициализация системы ввода
            _input = _window?.CreateInput();

            string vertexShaderSource =
            @"#version 330 core
layout (location = 0) in vec3 aPosition;

uniform mat4 transform;

void main()
{
    gl_Position = transform * vec4(aPosition, 1.0);
}";

            string fragmentShaderSource =
            @"#version 330 core
out vec4 FragColor;

void main()
{
    FragColor = vec4(1.0, 0.5, 0.2, 1.0);
}";

            var shader = new Shader(_gl, vertexShaderSource, fragmentShaderSource);
            float[] vertices = {
             0.5f, -0.5f, 0.0f,  // Нижний правый
            -0.5f, -0.5f, 0.0f,  // Нижний левый
             0.0f,  0.5f, 0.0f   // Верхний
        };

            uint[] indices = { 0, 1, 2 };
            var mesh = new Mesh(_gl, vertices, indices);

            NativeWindow.Update += delta =>
            {
                float angle = (float)(DateTime.Now.Millisecond / 1000.0f * Math.PI * 2.0);
                var transform = Matrix4X4.CreateRotationZ(angle);

                shader.SetUniform("transform", transform);
            };

            // Обновляем логику рендеринга
            NativeWindow.Render += delta =>
            {
                _gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
                mesh.Draw(shader);
            };
        }

        private void OnUpdate(double deltaTime)
        {
            if (debug)
            {
                _fpsHistory.Enqueue(1 / deltaTime);
                if (_fpsHistory.Count > FPS_SAMPLE_SIZE)
                    _fpsHistory.Dequeue();
                double averageFps = _fpsHistory.Average();

                _window.Title = $"FPS: {averageFps:0} | Raw FPS: {1 / deltaTime:0}";
            }
        }

        private void OnRender(double deltaTime)
        {
            _gl?.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            _gl?.Clear((uint)ClearBufferMask.ColorBufferBit);
        }

        private void OnClose()
        {
            // Очистка ресурсов при закрытии
        }

        private void OnResize(Vector2D<int> newSize)
        {
            _gl?.Viewport(0, 0, (uint)newSize.X, (uint)newSize.Y);
        }

        public void Run()
        {
            _window?.Run();
        }

        public void Dispose()
        {
            _window?.Dispose();
            _input?.Dispose();
        }
    }

    public class AppOptions
    {
        public int Width { get; set; } = 800;
        public int Height { get; set; } = 600;
        public string Title { get; set; } = "Engine";
        public bool Debug { get; set; } = false;
        public ILogger? Logger { get; set; }
        public Platform Platform { get; set; } = Platform.Exe;
    }

    public enum Platform
    {
        Exe,
        Web
    }
}
