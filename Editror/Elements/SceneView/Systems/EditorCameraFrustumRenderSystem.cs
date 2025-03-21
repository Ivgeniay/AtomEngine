using System.Numerics;
using Silk.NET.OpenGL;
using AtomEngine;
using System;
using EngineLib;

namespace Editor
{
    [HideInspectorSearchAttribute]
    public class EditorCameraFrustumRenderSystem : IRenderSystem
    {
        public IWorld World { get; set; }

        private readonly IEntityComponentInfoProvider _componentProvider;
        private readonly GL _gl;
        private CameraFrustumShader _shader;
        private Vector4 _defaultColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);

        private QueryEntity _queryCameras;
        private QueryEntity _queryEditorCamera;

        public EditorCameraFrustumRenderSystem(IWorld world, GL gl)
        {
            World = world;
            _gl = gl;

            _queryCameras = world.CreateEntityQuery()
                .Without<EditorCameraComponent>()
                .With<TransformComponent>()
                .With<CameraComponent>();

            _queryEditorCamera = world.CreateEntityQuery()
                .With<TransformComponent>()
                .With<CameraComponent>()
                .With<EditorCameraComponent>();

            try
            {
                _shader = new CameraFrustumShader(_gl);
                _shader.SetColor(_defaultColor);
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка инициализации CameraFrustumShader: {ex.Message}");
            }
        }

        public void Initialize() { }

        public void Render(double deltaTime)
        {
            if (_shader == null)
                return;

            var editorCameras = _queryEditorCamera.Build();
            if (editorCameras.Length == 0) return;

            var cameras = _queryCameras.Build();
            if (cameras.Length == 0)
                return;

            var editorCameraEntity = editorCameras[0];
            ref var editorTransform = ref World.GetComponent<TransformComponent>(editorCameraEntity);
            ref var editorCamera = ref World.GetComponent<CameraComponent>(editorCameraEntity);
            ref var editorCameraExt = ref World.GetComponent<EditorCameraComponent>(editorCameraEntity);

            Matrix4x4 view = editorCamera.ViewMatrix;
            Matrix4x4 projection = editorCameraExt.IsPerspective
                ? editorCamera.CreateProjectionMatrix()
                : CreateOrthographicMatrix(editorCamera, editorTransform, editorCameraExt);

            //GLEnum blendingEnabled = _gl.GetBoolean(GetPName.Blend) ? GLEnum.True : GLEnum.False;
            //GLEnum depthTestEnabled = _gl.GetBoolean(GetPName.DepthTest) ? GLEnum.True : GLEnum.False;
            //GLEnum cullFaceEnabled = _gl.GetBoolean(GetPName.CullFace) ? GLEnum.True : GLEnum.False;

            //_gl.Enable(EnableCap.Blend);
            //_gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            //_gl.Enable(EnableCap.DepthTest);
            //_gl.DepthFunc(DepthFunction.Lequal);
            //_gl.Disable(EnableCap.CullFace);

            _shader.Use();

            foreach (var cameraEntity in cameras)
            {
                ref var transformComponent = ref World.GetComponent<TransformComponent>(cameraEntity);
                ref var cameraComponent = ref World.GetComponent<CameraComponent>(cameraEntity);

                Vector3[] frustumCorners = CalculateFrustumCorners(transformComponent, cameraComponent);
                _shader.UpdateFrustumVertices(frustumCorners);
                _shader.SetMVP(Matrix4x4.Identity, view, projection);
                _shader.SetColor(_defaultColor);
                _shader.Draw();
            }

            //if (blendingEnabled == GLEnum.False) _gl.Disable(EnableCap.Blend);
            //else _gl.Enable(EnableCap.Blend);

            //if (depthTestEnabled == GLEnum.False) _gl.Disable(EnableCap.DepthTest);
            //else _gl.Enable(EnableCap.DepthTest);

            //if (cullFaceEnabled == GLEnum.True) _gl.Enable(EnableCap.CullFace);
            //else _gl.Disable(EnableCap.CullFace);
        }

        private Vector3[] CalculateFrustumCorners(TransformComponent transform, CameraComponent camera)
        {
            Vector3[] frustumCornersLocal = new Vector3[8];

            float nearPlane = camera.NearPlane;
            float farPlane = camera.FarPlane;
            float fovY = camera.FieldOfView * (MathF.PI / 180f);
            float aspect = camera.AspectRatio;

            float nearHeight = 2.0f * MathF.Tan(fovY / 2.0f) * nearPlane;
            float nearWidth = nearHeight * aspect;
            float farHeight = 2.0f * MathF.Tan(fovY / 2.0f) * farPlane;
            float farWidth = farHeight * aspect;

            frustumCornersLocal[0] = new Vector3(-nearWidth / 2, -nearHeight / 2, -nearPlane);  // левый нижний
            frustumCornersLocal[1] = new Vector3(nearWidth / 2, -nearHeight / 2, -nearPlane);   // правый нижний
            frustumCornersLocal[2] = new Vector3(nearWidth / 2, nearHeight / 2, -nearPlane);    // правый верхний
            frustumCornersLocal[3] = new Vector3(-nearWidth / 2, nearHeight / 2, -nearPlane);   // левый верхний

            float visualFarPlane = Math.Min(farPlane, 20.0f);
            float visualFarHeight = 2.0f * MathF.Tan(fovY / 2.0f) * visualFarPlane;
            float visualFarWidth = visualFarHeight * aspect;

            frustumCornersLocal[4] = new Vector3(-visualFarWidth / 2, -visualFarHeight / 2, -visualFarPlane);  // левый нижний
            frustumCornersLocal[5] = new Vector3(visualFarWidth / 2, -visualFarHeight / 2, -visualFarPlane);   // правый нижний
            frustumCornersLocal[6] = new Vector3(visualFarWidth / 2, visualFarHeight / 2, -visualFarPlane);    // правый верхний
            frustumCornersLocal[7] = new Vector3(-visualFarWidth / 2, visualFarHeight / 2, -visualFarPlane);   // левый верхний

            Vector3[] frustumCornersWorld = new Vector3[8];
            Matrix4x4 cameraTransform = CreateModelMatrix(transform);

            for (int i = 0; i < 8; i++)
            {
                frustumCornersWorld[i] = Vector3.Transform(frustumCornersLocal[i], cameraTransform);
            }

            return frustumCornersWorld;
        }

        private Matrix4x4 CreateModelMatrix(TransformComponent transform)
        {
            Matrix4x4 rotationMatrix = Matrix4x4.CreateFromQuaternion(transform.Rotation.ToQuaternion());
            Matrix4x4 translationMatrix = Matrix4x4.CreateTranslation(transform.Position);
            Matrix4x4 scaleMatrix = Matrix4x4.CreateScale(transform.Scale);

            Matrix4x4 result = Matrix4x4.Identity;
            result *= rotationMatrix;
            result *= translationMatrix;

            return result;
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
            var cameras = _queryEditorCamera.Build();
            if (cameras.Length > 0)
            {
                ref var camera = ref World.GetComponent<CameraComponent>(cameras[0]);
                camera.AspectRatio = size.X / size.Y;
            }
        }
    }
}