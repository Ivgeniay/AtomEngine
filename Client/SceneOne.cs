using AtomEngine.Diagnostic;
using AtomEngine.Input;
using AtomEngine.Math;
using AtomEngine.Scenes;

namespace Client
{
    internal class SceneOne : Scene
    {
        public SceneOne(string name, ILogger logger = null) : base(name, logger) { }

        protected override void OnLoadHandler()
        {
            logger?.LogInformation("SceneOne loaded");
        }

        protected override void OnRenderHandler(double value)  {  }

        protected override void OnResizeHandler(Vector2D<int> value)
        {
            logger?.LogInformation($"SceneOne resized {value}");
        }

        protected override void OnUnloadHandler()
        {
            logger?.LogInformation("SceneOne unloaded");
        }

        protected override void OnUpdateHandler(double value)
        {
            if (InputManager.IsKeyPressed(key: OpenTK.Windowing.GraphicsLibraryFramework.Keys.Space))
            {
                logger?.LogInformation("Space key pressed from scene one");
            }
        }
    }
}
