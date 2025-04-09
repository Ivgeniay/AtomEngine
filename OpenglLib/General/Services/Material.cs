using Silk.NET.OpenGL;
using AtomEngine;

namespace OpenglLib
{
    public sealed class Material
    {
        public readonly Shader Shader;
        internal readonly MaterialAsset MaterialAsset;
        internal GL GLContext;
        internal MaterialFactory factory;
        public bool IsValid { get => GLContext != null && Shader != null && Shader.Handle > 0; }

        public Material(GL glContext, Shader shader, MaterialAsset materialAsset)
        {
            GLContext = glContext;
            Shader = shader; 
            MaterialAsset = materialAsset;
        }

        internal void Use() => Shader.Use();
        public void SetUniform(string name, object value)
        {
            if (IsValid)
            {
#if DEBUG
                DebLogger.Error("Called material is not valid");
#endif
                return;
            }
            Shader.SetUniform(name, value);
        }
        public void SetTexture(string uniformName, Texture texture)
        {
            if (IsValid)
            {
#if DEBUG
                DebLogger.Error("Called material is not valid");
#endif
                return;
            }
            Shader.SetTexture(uniformName, texture);
        }


        public Shader Copy() => (Shader)factory.GetShaderFromMaterialAsset(GLContext, MaterialAsset);
        public Material Share() => factory.GetMaterialInstanceFromAsset(GLContext,MaterialAsset);
        internal void Dispose() => Shader.Dispose();

        public static implicit operator uint(Material material)
        {
            if (material == null) return 0;
            if (material.Shader == null) return 0;
            return material.Shader;
        }
        public static implicit operator int(Material material)
        {
            if (material == null) return 0;
            if (material.Shader == null) return 0;
            return material.Shader;
        }
    }


}
