using System.Threading.Tasks;
using OpenglLib;

namespace Editor
{
    public class EditorRuntimeResourceManager : OpenGLRuntimeResourceManager
    {
        public override Task InitializeAsync()
        {
            GLController.OnGLInitialized += OnGLInitialized;
            GLController.OnGLDeInitialized += Dispose;

            return base.InitializeAsync();
        }

    }
}
