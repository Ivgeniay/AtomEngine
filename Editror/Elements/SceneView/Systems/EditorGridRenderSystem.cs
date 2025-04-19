using System.Numerics;
using Silk.NET.OpenGL;
using AtomEngine;
using EngineLib;

namespace Editor
{
    //[HideInspectorSearch]
    //public class EditorGridRenderSystem : IRenderSystem
    //{
    //    public IWorld World { get; set; }

    //    private GridShader _gridShader;
    //    private GL _gl;
    //    private QueryEntity _queryEditorCameras;

    //    public EditorGridRenderSystem(IWorld world)
    //    {
    //        World = world;
    //        _queryEditorCameras = world.CreateEntityQuery()
    //            .With<TransformComponent>()
    //            .With<CameraComponent>()
    //            .With<EditorCameraComponent>()
    //            ;
    //    }

    //    public void Initialize()
    //    {
    //    }

    //    public void Render(double deltaTime, object? context)
    //    {
    //        if (context == null) return;
    //        if (context is GL) _gl = (GL)context;
    //        if (_gridShader == null) _gridShader = new GridShader(_gl);

    //        var cameras = _queryEditorCameras.Build();
    //        if (cameras.Length == 0) return;

    //        var cameraEntity = cameras[0];
    //        ref var cameraTransform = ref World.GetComponent<TransformComponent>(cameraEntity);
    //        ref var camera = ref World.GetComponent<CameraComponent>(cameraEntity);
    //        ref var editorCamera = ref World.GetComponent<EditorCameraComponent>(cameraEntity);

    //        _gl.Enable(EnableCap.DepthTest);
    //        _gl.DepthFunc(DepthFunction.Lequal);
    //        _gl.Enable(EnableCap.Blend);
    //        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

    //        var view = camera.ViewMatrix;
    //        var projection = editorCamera.IsPerspective
    //            ? camera.CreateProjectionMatrix()
    //            : CreateOrthographicMatrix(camera, cameraTransform, editorCamera);

    //        _gridShader.Use();
    //        _gridShader.SetMVP(Matrix4x4.Identity, view, projection);
    //        _gridShader.Draw();
    //    }

    //    private Matrix4x4 CreateOrthographicMatrix(CameraComponent camera, TransformComponent transform, EditorCameraComponent editorCamera)
    //    {
    //        float size = Vector3.Distance(transform.Position, editorCamera.Target) * 0.1f;
    //        return Matrix4x4.CreateOrthographic(
    //            size * camera.AspectRatio,
    //            size,
    //            camera.NearPlane,
    //            camera.FarPlane);
    //    }

    //    public void Resize(Vector2 size)
    //    {
    //        var cameras = _queryEditorCameras.Build();
    //        if (cameras.Length > 0)
    //        {
    //            ref var camera = ref World.GetComponent<CameraComponent>(cameras[0]);
    //            camera.AspectRatio = size.X / size.Y;
    //        }
    //    }
    //}


    [HideInspectorSearch]
    public class EditorGridRenderSystem : IRenderSystem
    {
        public IWorld World { get; set; }
        private InfiniteGridShader _gridShader;
        private GL _gl;
        private QueryEntity _queryEditorCameras;

        public EditorGridRenderSystem(IWorld world)
        {
            World = world;
            _queryEditorCameras = world.CreateEntityQuery()
                .With<TransformComponent>()
                .With<CameraComponent>()
                .With<EditorCameraComponent>();
        }

        public void Initialize()
        {
        }

        public void Render(double deltaTime, object? context)
        {
            if (context == null) return;
            if (context is GL) _gl = (GL)context;

            if (_gridShader == null)
                _gridShader = new InfiniteGridShader(_gl);

            var cameras = _queryEditorCameras.Build();
            if (cameras.Length == 0) return;

            var cameraEntity = cameras[0];
            ref var cameraTransform = ref World.GetComponent<TransformComponent>(cameraEntity);
            ref var camera = ref World.GetComponent<CameraComponent>(cameraEntity);
            ref var editorCamera = ref World.GetComponent<EditorCameraComponent>(cameraEntity);

            _gl.GetBoolean(GetPName.DepthTest, out var depthTestEnabled);
            _gl.GetInteger(GetPName.DepthFunc, out var depthFunc);
            _gl.GetBoolean(GetPName.Blend, out var blendEnabled);
            _gl.GetInteger(GetPName.BlendSrc, out var blendSrc);
            _gl.GetInteger(GetPName.BlendDst, out var blendDst);

            _gl.Enable(EnableCap.DepthTest);
            _gl.DepthFunc(DepthFunction.Lequal);
            _gl.Enable(EnableCap.Blend);
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            var view = camera.ViewMatrix;
            var projection = editorCamera.IsPerspective
                ? camera.CreateProjectionMatrix()
                : CreateOrthographicMatrix(camera, cameraTransform, editorCamera);

            _gridShader.SetViewProjection(view, projection, cameraTransform.Position);
            _gridShader.Draw();

            if (depthTestEnabled)
                _gl.Enable(EnableCap.DepthTest);
            else
                _gl.Disable(EnableCap.DepthTest);

            _gl.DepthFunc((DepthFunction)depthFunc);

            if (blendEnabled)
                _gl.Enable(EnableCap.Blend);
            else
                _gl.Disable(EnableCap.Blend);

            _gl.BlendFunc((BlendingFactor)blendSrc, (BlendingFactor)blendDst);
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
        }
    }

}