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

        public override void SetUniformValues(string materialAssetGuid, Dictionary<string, object> uniformValues)
        {
            sceneViewController?.EnqueueGLCommand(gl =>
            {
                base.SetUniformValues(materialAssetGuid, uniformValues);
            });
        }
        public override void SetUniformValues(Shader instance, Dictionary<string, object> uniformValues)
        {
            sceneViewController?.EnqueueGLCommand(gl =>
            {
                base.SetUniformValues(instance, uniformValues);
            });
        }
        public override void SetUniformValue(MaterialAsset materialAsset, string name, object value)
        {
            sceneViewController?.EnqueueGLCommand(gl =>
            {
                base.SetUniformValue(materialAsset, name, value);
            });
        }
        public override void SetUniformValue(string materialAssetGuid, string name, object value)
        {
            sceneViewController?.EnqueueGLCommand(gl =>
            {
                base.SetUniformValue(materialAssetGuid, name, value);
            });
        }
        public override void SetUniformValue(Material material, string name, object value)
        {
            sceneViewController?.EnqueueGLCommand(gl =>
            {
                base.SetUniformValue(material, name, value);
            });
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
        public override void SetTexture(MaterialAsset materialAsset, string samplerName, string textureGuid)
        {
            sceneViewController?.EnqueueGLCommand(gl =>
            {
                base.SetTexture(materialAsset, samplerName, textureGuid);
            });
        }
        public override void SetTexture(string materialAssetGuid, string samplerName, string textureGuid)
        {
            sceneViewController?.EnqueueGLCommand(gl =>
            {
                base.SetTexture(materialAssetGuid, samplerName, textureGuid);
            });
        }
        public override void SetTexture(Material material, string samplerName, string textureGuid)
        {
            sceneViewController?.EnqueueGLCommand(gl =>
            {
                base.SetTexture(material, samplerName, textureGuid);
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
