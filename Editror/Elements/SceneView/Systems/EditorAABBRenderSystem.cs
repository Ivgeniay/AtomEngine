using System.Numerics;
using Silk.NET.OpenGL;
using AtomEngine;

namespace Editor
{
    public class EditorAABBRenderSystem : IRenderSystem
    {
        private IWorld _world;
        public IWorld World => _world;

        private AABBManager _aabbManager;
        private QueryEntity _queryCameras;

        public EditorAABBRenderSystem(IWorld world, GL gl, IEntityComponentInfoProvider componentProvider)
        {
            _world = world;
            _queryCameras = world.CreateEntityQuery()
                .With<TransformComponent>()
                .With<CameraComponent>()
                .With<EditorCameraComponent>();

            // Инициализируем менеджер AABB
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
            ref var cameraTransform = ref _world.GetComponent<TransformComponent>(cameraEntity);
            ref var camera = ref _world.GetComponent<CameraComponent>(cameraEntity);

            _aabbManager.Render(camera.ViewMatrix, camera.CreateProjectionMatrix());
        }

        public void Resize(Vector2 size)
        {
            var cameras = _queryCameras.Build();
            if (cameras.Length > 0)
            {
                ref var camera = ref _world.GetComponent<CameraComponent>(cameras[0]);
                camera.AspectRatio = size.X / size.Y;
            }
        }
    }
}