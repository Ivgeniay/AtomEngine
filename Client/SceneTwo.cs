using AtomEngine.Diagnostic;
using AtomEngine.Input;
using AtomEngine.Math;
using AtomEngine.Scenes;

namespace Client
{
    internal class SceneTwo : Scene
    {
        public SceneTwo(string name, ILogger logger = null) : base(name, logger) { }

        protected override void OnLoadHandler()
        {
            _logger?.LogInformation("SceneTwo loaded");
        }

        protected override void OnRenderHandler(double value) { }

        protected override void OnResizeHandler(Vector2D<int> value)
        {
            _logger?.LogInformation("SceneTwo resized");
        }

        protected override void OnUnloadHandler()
        {
            _logger?.LogInformation("SceneTwo unloaded");
        }

        protected override void OnUpdateHandler(double value)
        {
            if (InputManager.IsKeyPressed(key: OpenTK.Windowing.GraphicsLibraryFramework.Keys.Space))
            {
                _logger?.LogInformation("Space key pressed from scene two");
            } 
        }
    }
}
