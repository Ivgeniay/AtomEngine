using OpenTK.Windowing.Desktop; 
using AtomEngine.Scenes;
using AtomEngine.Math;
using AtomEngine.Diagnostic;

namespace OpenGLCore
{
    public class App : IDisposable
    {
        private static App lazy;
        public static App Instance { get 
            {
                if (lazy == null) lazy = new App();
                return lazy; 
            }}


        internal Window? Window;
        internal ISceneDisposer SceneDisposer { get; private set; }
        public Scene? CurrentSceen => SceneDisposer.CurrentScene;
        protected readonly ILogger? logger;

        private App() :this (new SceneDisposer(), new AppConfiguration() { Resolution = new Vector2D<int>(1024, 720) }) { } 
        public App(ISceneDisposer sceneDisposer, AppConfiguration appConfiguration, ILogger logger = null) 
        {
            if (lazy == null) lazy = this;

            this.logger = logger; 
            SceneDisposer = sceneDisposer;

            CreateWindow(
                resolution: appConfiguration.Resolution, 
                title: appConfiguration.Title, 
                updateFrequency: appConfiguration.UpdateFrequency);
        }

        public void CreateWindow(Vector2D<int> resolution, string title = null, double updateFrequency = 60)
        {
            CreateWindow(resolution.X, resolution.Y, title, updateFrequency);
        }

        private void CreateWindow(int width, int height, string title = null, double updateFrequency = 60)
        {
            GameWindowSettings gameWindowSettings = new()
            {
                UpdateFrequency = updateFrequency
            };

            NativeWindowSettings nativeWindowSettings = new()
            {
                ClientSize = new OpenTK.Mathematics.Vector2i(width, height),
                Title = string.IsNullOrWhiteSpace(title) ? "Atom Engine" : title,
                Vsync = OpenTK.Windowing.Common.VSyncMode.On,
                WindowState = OpenTK.Windowing.Common.WindowState.Normal,
                WindowBorder = OpenTK.Windowing.Common.WindowBorder.Resizable,
                API = OpenTK.Windowing.Common.ContextAPI.OpenGL,
                APIVersion = new Version(4, 6),
                IsEventDriven = false,
                Profile = OpenTK.Windowing.Common.ContextProfile.Core,
                NumberOfSamples = 8
            };

            Window = new Window(gameWindowSettings, nativeWindowSettings);
        }

        public void ToggleFullscreen()
        {
            if (Window == null) return;

            Window.WindowState =
                Window.WindowState == OpenTK.Windowing.Common.WindowState.Fullscreen ?
                    OpenTK.Windowing.Common.WindowState.Normal :
                    OpenTK.Windowing.Common.WindowState.Fullscreen;
        }

        public void Run()
        {
            Window?.Run();
        }

        public void Close()
        {
            Window?.Close();
        }

        public void Dispose()
        {
            Window?.Dispose();
        }
    }

    public class AppConfiguration
    {
        public Vector2D<int> Resolution { get; set; }
        public string? Title { get; set; } = null;
        public double UpdateFrequency { get; set; } = 60;
    }
}
