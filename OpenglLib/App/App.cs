﻿using Silk.NET.Windowing;
using Silk.NET.OpenGL;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Assimp;
using AtomEngine;

namespace OpenglLib
{
    // TODO: GatherCollisionPairs в BVH сейчас обрабатывает все пары объектов, что неэффективно.
    public class App : IDisposable
    {
        public event Action OnFixedUpdate;
        public GL? Gl => _gl;
        public Assimp? Assimp => _assimp;
        public IWindow? NativeWindow => _window;
        public IInputContext? Input => _input;


        private IWindow? _window;
        private IInputContext? _input;
        private AppOptions appOptions;
        private GL? _gl;
        private Assimp? _assimp;

        private double _accumulatedTime = 0.0;

        public App(AppOptions options)
        {
            appOptions = options;
            GLSLTypeManager.Instance.LazyInitializer();

            var win_options = WindowOptions.Default;
            win_options.Size = new Vector2D<int>(options.Width, options.Height);
            win_options.Title = options.Title;

            if (appOptions.Debug)
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
            Time.Update(deltaTime);
            Application.Update(deltaTime);
            _accumulatedTime += deltaTime;

            if (appOptions.Debug)
            {
                if (Time.SecondsSinceStart % 2 == 0)
                    _window.Title = $"FPS: {Application.FPS:0} | Raw FPS: {Application.FPS_raw:0}";
            }

            while (_accumulatedTime >= Time.FIXED_TIME_STEP)
            {
                OnFixedUpdate?.Invoke();
                _accumulatedTime -= Time.FIXED_TIME_STEP;
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
            //_input?.Dispose();
        }
    }
}
