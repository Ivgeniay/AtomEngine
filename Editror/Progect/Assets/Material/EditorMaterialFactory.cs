using System.Collections.Generic;
using System.Threading.Tasks;
using OpenglLib;

namespace Editor
{
    internal class EditorMaterialFactory : MaterialFactory
    {
        private SceneViewController sceneViewController;

        public override Task InitializeAsync() =>
            base.InitializeAsync();

        public void SetSceneViewController(SceneViewController instance)
        {
            this.sceneViewController = instance;
        }

        public override void ApplyUniformValues(string materialGuid, Dictionary<string, object> uniformValues)
        {
            sceneViewController?.EnqueueGLCommand(gl => {
                if (_shaderInstanceCache.TryGetValue(materialGuid, out var shader))
                {
                    ApplyUniformValues(shader, uniformValues);
                }
            });
        }
        public override void ApplyTextures(string materialGuid, Dictionary<string, string> textureReferences)
        {
            if (_shaderInstanceCache.TryGetValue(materialGuid, out var shaderInstance)) 
            {
                sceneViewController?.EnqueueGLCommand( gl =>
                {
                    ApplyTextures(gl, shaderInstance, textureReferences);
                });
            }
        }
    }
}
