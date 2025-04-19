using System.Numerics;
using AtomEngine;

namespace OpenglLib
{
    public class ViewRenderSystem : IRenderSystem
    {
        public IWorld World { get; set; }
        private QueryEntity queryRendererEntities;

        public ViewRenderSystem(IWorld world)
        {
            World = world;
            queryRendererEntities = this.CreateEntityQuery()
                .With<TransformComponent>()
                .With<PBRComponent>()
                ;
        }


        public void Render(double deltaTime, object? context)
        {
            Entity[] rendererEntities = queryRendererEntities.Build();
            if (rendererEntities.Length == 0) return;

            foreach (var entity in rendererEntities)
            {
                ref var transform = ref this.GetComponent<TransformComponent>(entity);
                ref var materialComponent = ref this.GetComponent<PBRComponent>(entity);

                if (materialComponent.Mesh == null || materialComponent.Material == null || materialComponent.Material?.Shader == null)
                    continue;

                var shader = materialComponent.Material.Shader;
                var material = materialComponent.Material;

                material.Use();
                material.SetUniform("modelPosition", transform.Position.ToSilk());
                material.SetUniform("modelRotation", transform.Rotation.ToSilk());
                material.SetUniform("modelScale", transform.Scale.ToSilk());

                materialComponent.Mesh.Draw(shader);
            }
        }


        public void Resize(Vector2 size)
        { }

        public void Initialize()
        { }
    }
}
