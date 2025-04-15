using AtomEngine;
using System.Numerics;

namespace OpenglLib
{
    public class ViewGetterRenderSystem : IRenderSystem
    {
        public IWorld World { get; set; }
        public QueryEntity shaderEntityQuery;
        public QueryEntity cameraEntitiesQuery;
        public ViewGetterRenderSystem(IWorld world)
        {
            World = world;
            shaderEntityQuery = this.CreateEntityQuery()
                .With<TransformComponent>()
                .With<ViewComponent>()
                ;
            cameraEntitiesQuery = this.CreateEntityQuery()
                .With<TransformComponent>()
                .With<CameraComponent>();
        }

        public void Initialize() { }

        public void Render(double deltaTime, object? context)
        {
            var queryRenderersEntity = shaderEntityQuery.Build();
            if (queryRenderersEntity.Count() == 0) return;

            var queryCameraEntity = cameraEntitiesQuery.Build();
            if (queryCameraEntity.Count() == 0) return;

            Entity cameraEntity = Entity.Null;
            foreach (Entity e in queryCameraEntity)
            {
                ref var cameraComp = ref this.GetComponent<CameraComponent>(e);
                if (cameraComp.IsActive)
                {
                    cameraEntity = e;
                    break;
                }
            }
            if (cameraEntity == Entity.Null) return;

            ref var cameraTransform = ref this.GetComponent<TransformComponent>(cameraEntity);
            ref var cameraComponent = ref this.GetComponent<CameraComponent>(cameraEntity);

            foreach (var entity in queryRenderersEntity)
            {
                ref var transform = ref this.GetComponent<TransformComponent>(entity);
                ref var viewRenderComponent = ref this.GetComponent<ViewComponent>(entity);

                viewRenderComponent.view = cameraComponent.ViewMatrix.ToSilk();
                viewRenderComponent.model = transform.GetModelMatrix().ToSilk();
                viewRenderComponent.projection = cameraComponent.CreateProjectionMatrix().ToSilk();
            }
        }

        public void Resize(Vector2 size)
        {

        }
    }
}
