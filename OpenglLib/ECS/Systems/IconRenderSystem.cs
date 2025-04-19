using Silk.NET.OpenGL;
using System.Numerics;
using AtomEngine;
using EngineLib;
using System.Text;

namespace OpenglLib
{
    public class IconRenderSystem : IRenderSystem
    {
        public IWorld World { get; set; }

        private QueryEntity queryIconEntities;

        public IconRenderSystem(IWorld world)
        {
            World = world;

            queryIconEntities = this.CreateEntityQuery()
                .With<IconComponent>()
                .With<TransformComponent>()
                ;
        }

        public void Initialize()
        {
        }

        public void Render(double deltaTime, object? context)
        {
            if (context == null)
                return;

            Entity[] iconEntities = queryIconEntities.Build();
            if (iconEntities.Length == 0) return;

            GL gl = (GL)context;

            bool depthTestEnabled = gl.IsEnabled(EnableCap.DepthTest);
            bool blendEnabled = gl.IsEnabled(EnableCap.Blend);

            gl.GetInteger(GLEnum.BlendSrc, out int srcFactor);
            gl.GetInteger(GLEnum.BlendDst, out int dstFactor);

            gl.Enable(EnableCap.Blend);
            gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            gl.Disable(EnableCap.DepthTest);


            foreach (var entity in iconEntities)
            {
                ref var iconComponent = ref this.GetComponent<IconComponent>(entity);
                ref var transformComponent = ref this.GetComponent<TransformComponent>(entity);

                if (iconComponent.Material == null || iconComponent.Mesh == null || iconComponent.Material.Shader == null)
                    continue;

                iconComponent.Material.Use();
                iconComponent.Material.SetUniform("iconSize", iconComponent.IconSize);
                iconComponent.Material.SetUniform("modelPosition", transformComponent.Position.ToSilk());

                iconComponent.Mesh.Draw();
            }

            if (depthTestEnabled) gl.Enable(EnableCap.DepthTest);
            else gl.Disable(EnableCap.DepthTest);
            if (blendEnabled) gl.Enable(EnableCap.Blend);
            else gl.Disable(EnableCap.Blend);
            gl.BlendFunc((BlendingFactor)srcFactor, (BlendingFactor)dstFactor);
        }

        public void Resize(Vector2 size)
        {
           
        }
    }
}
