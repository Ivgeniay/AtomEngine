using AtomEngine;
using System.Numerics;

namespace OpenglLib
{
    public class ViewSetterRenderSystem : IRenderSystem
    {
        public IWorld World { get; set; }
        private QueryEntity queryRendererEntities;


        public ViewSetterRenderSystem(IWorld world)
        {
            World = world;

            queryRendererEntities = this.CreateEntityQuery()
                .With<MaterialComponent>()
                .With<MeshComponent>()
                .With<ViewComponent>()
                ;
        }


        public void Render(double deltaTime, object? context)
        {
            Entity[] rendererEntities = queryRendererEntities.Build();
            if (rendererEntities.Length == 0)
                return;

            foreach (var entity in rendererEntities)
            {
                ref var meshComponent = ref this.GetComponent<MeshComponent>(entity);
                ref var materialComponent = ref this.GetComponent<MaterialComponent>(entity);

                if (meshComponent.Mesh == null || materialComponent.Material?.Shader == null)
                    continue;

                ref var viewComponent = ref this.GetComponent<ViewComponent>(entity);

                var shader = materialComponent.Material.Shader;
                var material = materialComponent.Material;

                shader.Use();
                material.SetUniform("model", viewComponent.model);
                material.SetUniform("view", viewComponent.view);
                material.SetUniform("projection", viewComponent.projection);
                meshComponent.Mesh.Draw(materialComponent.Material.Shader);
                //if (shader is IViewRender renderer) { }
            }
        }

        public void Resize(Vector2 size)
        { }

        public void Initialize()
        { }
    }
}
