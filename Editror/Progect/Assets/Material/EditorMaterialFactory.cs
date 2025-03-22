using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EngineLib;
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

        public override void SetTextures(Material material, Dictionary<string, string> textureReferences)
        {
            sceneViewController?.EnqueueGLCommand(gl =>
            {
                base.SetTextures(material, textureReferences);
            });
        }


        public override void SetTextures(string materialAssetGuid, Dictionary<string, string> textureReferences)
        {
            sceneViewController?.EnqueueGLCommand(gl =>
            {
                base.SetTextures(materialAssetGuid, textureReferences);
            });
        }


        //public override void ApplyUniformValues(string materialAssetGuid, Dictionary<string, object> uniformValues)
        //{
        //    sceneViewController?.EnqueueGLCommand(gl => 
        //    {
        //        var materials = GetMaterialsFrom(materialAssetGuid);
        //        foreach (var material in materials)
        //        {
        //            ApplyUniformValues(material.Shader, uniformValues);
        //        }
        //    });
        //}
        //public override void ApplyTextures(string materialAssetGuid, Dictionary<string, string> textureReferences)
        //{
        //    sceneViewController?.EnqueueGLCommand(gl =>
        //    {
        //        var materials = GetMaterialsFrom(materialAssetGuid);
        //        foreach (var material in materials)
        //        {
        //            ApplyTextures(gl, shaderInstance, textureReferences);
        //        }
        //    });

        //    //if (_shaderInstanceCache.TryGetValue(materialAssetGuid, out var shaderInstance)) 
        //    //{
        //    //    sceneViewController?.EnqueueGLCommand(gl =>
        //    //    {
        //    //        ApplyTextures(gl, shaderInstance, textureReferences);
        //    //    });
        //    //}
        //}
    }
}
