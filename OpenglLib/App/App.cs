using Silk.NET.Windowing;
using Silk.NET.OpenGL;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Assimp;

namespace OpenglLib
{
    public class App : IDisposable
    {
        public GL? Gl => _gl;
        public Assimp? Assimp => _assimp;
        public IWindow? NativeWindow => _window;
        public IInputContext? Input => _input;


        private IWindow? _window;
        private IInputContext? _input;
        private AppOptions appOptions;
        private GL? _gl;
        private Assimp? _assimp;

        private Queue<double> _fpsHistory = new Queue<double>();
        private const int FPS_SAMPLE_SIZE = 60;
        private bool debug = false;

        public App(AppOptions options)
        {
            appOptions = options;
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
            _gl = GL.GetApi(_window);
            _gl.Enable(EnableCap.DepthTest);
            _gl.Enable(EnableCap.Blend);
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            _assimp = Assimp.GetApi();
            _input = _window?.CreateInput(); 
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
            _gl?.ClearColor(appOptions.BackgroundColor.Item1, appOptions.BackgroundColor.Item2, appOptions.BackgroundColor.Item3, appOptions.BackgroundColor.Item4);
            _gl?.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
        }

        private void OnClose()
        {
            
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
}
