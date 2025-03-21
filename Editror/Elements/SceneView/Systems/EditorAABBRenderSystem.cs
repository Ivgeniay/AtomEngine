using System.Numerics;
using Silk.NET.OpenGL;
using AtomEngine;
using EngineLib;

namespace Editor
{
    [HideInspectorSearchAttribute]
    public class EditorAABBRenderSystem : IRenderSystem
    { 
        public IWorld World { get;set; }

        private AABBManager _aabbManager;
        private QueryEntity _queryCameras;

        public EditorAABBRenderSystem(IWorld world, GL gl, IEntityComponentInfoProvider componentProvider)
        {
            World = world;
            _queryCameras = world.CreateEntityQuery()
                .With<TransformComponent>()
                .With<CameraComponent>()
                .With<EditorCameraComponent>();

            _aabbManager = new AABBManager(gl, componentProvider);
        }

        public void Initialize()
        {
        }

        public void Render(double deltaTime)
        {
            var cameras = _queryCameras.Build();
            if (cameras.Length == 0) return;

            var cameraEntity = cameras[0];
            ref var cameraTransform = ref World.GetComponent<TransformComponent>(cameraEntity);
            ref var camera = ref World.GetComponent<CameraComponent>(cameraEntity);

            _aabbManager.Render(camera.ViewMatrix, camera.CreateProjectionMatrix());
        }

        public void Resize(Vector2 size)
        {
            var cameras = _queryCameras.Build();
            if (cameras.Length > 0)
            {
                ref var camera = ref World.GetComponent<CameraComponent>(cameras[0]);
                camera.AspectRatio = size.X / size.Y;
            }
        }
    }
}