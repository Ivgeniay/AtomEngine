using System.Numerics;
using Silk.NET.OpenGL;
using AtomEngine;

namespace Editor
{
    public class EditorGridRenderSystem : IRenderSystem
    {
        private IWorld _world;
        public IWorld World => _world;

        private GridShader _gridShader;
        private GL _gl;
        private Entity _gridEntity;
        private QueryEntity _queryEditorCameras;

        public EditorGridRenderSystem(IWorld world, GL gl)
        {
            _world = world;
            _gl = gl;
            _queryEditorCameras = world.CreateEntityQuery()
                .With<TransformComponent>()
                .With<CameraComponent>()
                .With<EditorCameraComponent>();

            _gridShader = new GridShader(_gl);
        }

        public void Initialize()
        {
            _gridEntity = _world.CreateEntity();
        }

        public void Render(double deltaTime)
        {
            var cameras = _queryEditorCameras.Build();
            if (cameras.Length == 0) return;

            var cameraEntity = cameras[0];
            ref var cameraTransform = ref _world.GetComponent<TransformComponent>(cameraEntity);
            ref var camera = ref _world.GetComponent<CameraComponent>(cameraEntity);
            ref var editorCamera = ref _world.GetComponent<EditorCameraComponent>(cameraEntity);

            _gl.Enable(EnableCap.DepthTest);
            _gl.DepthFunc(DepthFunction.Lequal);
            _gl.Enable(EnableCap.Blend);
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            var view = camera.ViewMatrix;
            var projection = editorCamera.IsPerspective
                ? camera.CreateProjectionMatrix()
                : CreateOrthographicMatrix(camera, cameraTransform, editorCamera);

            _gridShader.Use();
            _gridShader.SetMVP(Matrix4x4.Identity, view, projection);
            _gridShader.Draw();
        }

        private Matrix4x4 CreateOrthographicMatrix(CameraComponent camera, TransformComponent transform, EditorCameraComponent editorCamera)
        {
            float size = Vector3.Distance(transform.Position, editorCamera.Target) * 0.1f;
            return Matrix4x4.CreateOrthographic(
                size * camera.AspectRatio,
                size,
                camera.NearPlane,
                camera.FarPlane);
        }

        public void Resize(Vector2 size)
        {
            var cameras = _queryEditorCameras.Build();
            if (cameras.Length > 0)
            {
                ref var camera = ref _world.GetComponent<CameraComponent>(cameras[0]);
                camera.AspectRatio = size.X / size.Y;
            }
        }
    }
}