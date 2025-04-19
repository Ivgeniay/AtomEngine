using AtomEngine;
using Silk.NET.OpenGL;
using System.Numerics;

namespace OpenglLib
{
    public class ShadowMapBindingSystem : IRenderSystem
    {
        public IWorld World { get; set; }

        private QueryEntity queryRendererEntities;
        private QueryEntity queryShadowMapEntities;

        private const int SHADOW_TEXTURE_UNIT = 10;

        public ShadowMapBindingSystem(IWorld world)
        {
            World = world;

            queryRendererEntities = this.CreateEntityQuery()
                .With<TransformComponent>()
                .With<PBRComponent>()
                ;

            queryShadowMapEntities = this.CreateEntityQuery()
                .With<ShadowMapComponent>()
                ;
        }

        public void Initialize()
        {
        }

        public void Render(double deltaTime, object? context)
        {
            if (context == null) return;
            GL gl = null;
            if (context is GL) gl = (GL)context;
            if (gl == null) return;

            Entity[] shadowMapEntities = queryShadowMapEntities.Build();
            if (shadowMapEntities.Length == 0) return;

            Entity[] rendererEntities = queryRendererEntities.Build();
            if (rendererEntities.Length == 0) return;

            ref var shadowMapComponent = ref this.GetComponent<ShadowMapComponent>(shadowMapEntities[0]);
            if (shadowMapComponent.ShadowMapArrayTextureId == 0) return;

            gl.ActiveTexture(TextureUnit.Texture0 + SHADOW_TEXTURE_UNIT);
            gl.BindTexture(TextureTarget.Texture2DArray, shadowMapComponent.ShadowMapArrayTextureId);

            foreach (var entity in rendererEntities)
            {
                ref var pbrComponent = ref this.GetComponent<PBRComponent>(entity);
                if (pbrComponent.Material == null || pbrComponent.Material?.Shader == null)
                    continue;

                Material material = pbrComponent.Material;
                material.Use();
                material.SetUniform("shadowMapsArray", SHADOW_TEXTURE_UNIT);
            }
        }

        public void Resize(Vector2 size)
        {
        }
    }


}
