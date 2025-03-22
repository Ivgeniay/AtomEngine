using System.Collections.Generic;
using Silk.NET.OpenGL;
using OpenglLib;
using System;

using Texture = OpenglLib.Texture;
using System.Threading.Tasks;
using AtomEngine.RenderEntity;
using EngineLib;

namespace Editor
{
    public class EditorRuntimeResourceManager : OpenGLRuntimeResourceManager
    {
        private EditorMaterialFactory _materialFactory;

        public override Task InitializeAsync()
        {
            GLController.OnGLInitialized += OnGLInitialized;
            GLController.OnGLDeInitialized += Dispose;

            _materialFactory = ServiceHub.Get<EditorMaterialFactory>();

            return base.InitializeAsync();
        }

        protected override ShaderBase LoadMaterialResource(string guid)
        {
            if (!_isGLInitialized || _gl == null)
                return null;

            var material = _materialFactory.GetShaderFormMaterialAssetGUID(_gl, guid);
            return material ;
        }


 
        public override void Dispose()
        {
            _materialFactory.Dispose();

            base.Dispose();
        }

    }
}
