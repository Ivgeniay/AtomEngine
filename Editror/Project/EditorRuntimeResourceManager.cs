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
        public override Task InitializeAsync()
        {
            GLController.OnGLInitialized += OnGLInitialized;
            GLController.OnGLDeInitialized += Dispose;

            return base.InitializeAsync();
        }

    }
}
