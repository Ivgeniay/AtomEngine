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
                .With<MaterialComponent>()
                .With<MeshComponent>()
                ;
        }


        public void Render(double deltaTime, object? context)
        {
            Entity[] rendererEntities = queryRendererEntities.Build();
            if (rendererEntities.Length == 0) return;

            foreach (var entity in rendererEntities)
            {
                ref var transform = ref this.GetComponent<TransformComponent>(entity);
                ref var meshComponent = ref this.GetComponent<MeshComponent>(entity);
                ref var materialComponent = ref this.GetComponent<MaterialComponent>(entity);

                if (meshComponent.Mesh == null || materialComponent.Material?.Shader == null)
                    continue;

                var shader = materialComponent.Material.Shader;
                var material = materialComponent.Material;

                /*
                 uniform vec3 modelPosition;
uniform vec3 modelRotation;
uniform vec3 modelScale;
                 */

                material.Use();
                material.SetUniform("modelPosition", transform.Position.ToSilk());
                material.SetUniform("modelRotation", transform.Rotation.ToSilk());
                material.SetUniform("modelScale", transform.Scale.ToSilk());

                meshComponent.Mesh.Draw(shader);
            }
        }

        public void Resize(Vector2 size)
        { }

        public void Initialize()
        { }
    }


    //public class ViewRenderSystem : IRenderSystem
    //{
    //    public IWorld World { get; set; }
    //    private QueryEntity queryRendererEntities;
    //    private QueryEntity cameraEntitiesQuery;

    //    public ViewRenderSystem(IWorld world)
    //    {
    //        World = world;

    //        queryRendererEntities = this.CreateEntityQuery()
    //            .With<TransformComponent>()
    //            .With<MaterialComponent>()
    //            .With<MeshComponent>()
    //            ;

    //        cameraEntitiesQuery = this.CreateEntityQuery()
    //            .With<TransformComponent>()
    //            .With<CameraComponent>();
    //    }


    //    public void Render(double deltaTime, object? context)
    //    {
    //        Entity[] rendererEntities = queryRendererEntities.Build();
    //        if (rendererEntities.Length == 0) return;

    //        var queryCameraEntity = cameraEntitiesQuery.Build();
    //        if (queryCameraEntity.Length == 0) return;

    //        Entity cameraEntity = Entity.Null;
    //        foreach (Entity e in queryCameraEntity)
    //        {
    //            ref var cameraComp = ref this.GetComponent<CameraComponent>(e);
    //            if (cameraComp.IsActive)
    //            {
    //                cameraEntity = e;
    //                break;
    //            }
    //        }
    //        if (cameraEntity == Entity.Null) return;

    //        //ref var cameraTransform = ref this.GetComponent<TransformComponent>(cameraEntity);
    //        ref var cameraComponent = ref this.GetComponent<CameraComponent>(cameraEntity);

    //        foreach (var entity in rendererEntities)
    //        {
    //            ref var transform = ref this.GetComponent<TransformComponent>(entity);
    //            ref var meshComponent = ref this.GetComponent<MeshComponent>(entity);
    //            ref var materialComponent = ref this.GetComponent<MaterialComponent>(entity);

    //            if (meshComponent.Mesh == null || materialComponent.Material?.Shader == null)
    //                continue;

    //            var shader = materialComponent.Material.Shader;
    //            var material = materialComponent.Material;

    //            material.Use();
    //            material.SetUniform("view", cameraComponent.ViewMatrix.ToSilk());
    //            material.SetUniform("model", transform.GetModelMatrix().ToSilk());
    //            material.SetUniform("projection", cameraComponent.CreateProjectionMatrix().ToSilk());

    //            meshComponent.Mesh.Draw(shader);
    //        }
    //    }

    //    public void Resize(Vector2 size)
    //    { }

    //    public void Initialize()
    //    { }
    //}

    //public class ViewGetterRenderSystem : IRenderSystem
    //{
    //    public IWorld World { get; set; }
    //    public QueryEntity shaderEntityQuery;
    //    public QueryEntity cameraEntitiesQuery;
    //    public ViewGetterRenderSystem(IWorld world)
    //    {
    //        World = world;
    //        shaderEntityQuery = this.CreateEntityQuery()
    //            .With<TransformComponent>()
    //            .With<ViewComponent>()
    //            ;
    //        cameraEntitiesQuery = this.CreateEntityQuery()
    //            .With<TransformComponent>()
    //            .With<CameraComponent>();
    //    }

    //    public void Initialize() { }

    //    public void Render(double deltaTime, object? context)
    //    {
    //        var queryRenderersEntity = shaderEntityQuery.Build();
    //        if (queryRenderersEntity.Count() == 0) return;

    //        var queryCameraEntity = cameraEntitiesQuery.Build();
    //        if (queryCameraEntity.Count() == 0) return;

    //        Entity cameraEntity = Entity.Null;
    //        foreach (Entity e in queryCameraEntity)
    //        {
    //            ref var cameraComp = ref this.GetComponent<CameraComponent>(e);
    //            if (cameraComp.IsActive)
    //            {
    //                cameraEntity = e;
    //                break;
    //            }
    //        }
    //        if (cameraEntity == Entity.Null) return;

    //        ref var cameraTransform = ref this.GetComponent<TransformComponent>(cameraEntity);
    //        ref var cameraComponent = ref this.GetComponent<CameraComponent>(cameraEntity);

    //        foreach (var entity in queryRenderersEntity)
    //        {
    //            ref var transform = ref this.GetComponent<TransformComponent>(entity);
    //            ref var viewRenderComponent = ref this.GetComponent<ViewComponent>(entity);

    //            viewRenderComponent.view = cameraComponent.ViewMatrix.ToSilk();
    //            viewRenderComponent.model = transform.GetModelMatrix().ToSilk();
    //            viewRenderComponent.projection = cameraComponent.CreateProjectionMatrix().ToSilk();
    //        }
    //    }

    //    public void Resize(Vector2 size)
    //    {

    //    }
    //}

}
