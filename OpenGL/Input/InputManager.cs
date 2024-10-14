using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenGLCore;

namespace AtomEngine.Input
{
    public static class InputManager
    {
        public static bool IsKeyPressed(Keys key)
        {
            return App.Instance.Window?.IsKeyPressed(key) ?? false;
        }
    }
}
