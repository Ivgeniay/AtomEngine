using System.Numerics;
using Silk.NET.OpenGL;
using AtomEngine;
using EngineLib;

namespace Editor
{
    [HideInspectorSearch]
    public class EditorGridRenderSystem : IRenderSystem
    {
        public IWorld World { get; set; }

        private GridShader _gridShader;
        private GL _gl;
        private Entity _gridEntity;
        private QueryEntity _queryEditorCameras;

        public EditorGridRenderSystem(IWorld world, GL gl)
        {
            World = world;
            _gl = gl;
            _queryEditorCameras = world.CreateEntityQuery()
                .With<TransformComponent>()
                .With<CameraComponent>()
                .With<EditorCameraComponent>();

            _gridShader = new GridShader(_gl);
        }

        public void Initialize()
        {
            _gridEntity = World.CreateEntity();
        }

        public void Render(double deltaTime, object? context)
        {
            var cameras = _queryEditorCameras.Build();
            if (cameras.Length == 0) return;

            var cameraEntity = cameras[0];
            ref var cameraTransform = ref World.GetComponent<TransformComponent>(cameraEntity);
            ref var camera = ref World.GetComponent<CameraComponent>(cameraEntity);
            ref var editorCamera = ref World.GetComponent<EditorCameraComponent>(cameraEntity);

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
                ref var camera = ref World.GetComponent<CameraComponent>(cameras[0]);
                camera.AspectRatio = size.X / size.Y;
            }
        }
    }
}