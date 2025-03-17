using System.Collections.Generic;
using System.Numerics;
using Silk.NET.OpenGL;
using AtomEngine;
using System;

namespace Editor
{
    internal class CameraFrustumManager : IDisposable, ICacheble
    {
        private readonly Dictionary<uint, Vector4> _cameraFrustums = new Dictionary<uint, Vector4>();
        private readonly IEntityComponentInfoProvider _componentProvider;
        private readonly GL _gl;
        private CameraFrustumShader _shader;
        private bool _isInitialized = false;
        private bool _isVisible = true;
        private Vector4 _defaultColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);

        public CameraFrustumManager(GL gl, IEntityComponentInfoProvider componentProvider)
        {
            _gl = gl;
            _componentProvider = componentProvider;
            InitializeShader();
        }

        private void InitializeShader()
        {
            try
            {
                _shader = new CameraFrustumShader(_gl);
                _shader.SetColor(_defaultColor);
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка инициализации CameraFrustumShader: {ex.Message}");
                _isInitialized = false;
            }
        }

        public void AddCamera(uint entityId)
        {
            if (!_componentProvider.HasComponent<CameraComponent>(entityId) ||
                !_componentProvider.HasComponent<TransformComponent>(entityId))
                return;

            _cameraFrustums[entityId] = _defaultColor;
        }

        public void RemoveCamera(uint entityId)
        {
            if (_cameraFrustums.ContainsKey(entityId))
                _cameraFrustums.Remove(entityId);
        }

        public void SetCameraColor(uint entityId, Vector4 color)
        {
            if (_cameraFrustums.ContainsKey(entityId))
            {
                _cameraFrustums[entityId] = color;
            }
        }

        public void SetVisibility(bool isVisible)
        {
            _isVisible = isVisible;
        }

        public void Render(Matrix4x4 view, Matrix4x4 projection)
        {
            if (!_isInitialized || !_isVisible)
                return;

            if (_cameraFrustums.Count == 0)
                return;


            GLEnum blendingEnabled = _gl.GetBoolean(GetPName.Blend) ? GLEnum.True : GLEnum.False;
            GLEnum depthTestEnabled = _gl.GetBoolean(GetPName.DepthTest) ? GLEnum.True : GLEnum.False;
            GLEnum cullFaceEnabled = _gl.GetBoolean(GetPName.CullFace) ? GLEnum.True : GLEnum.False;

            _gl.Enable(EnableCap.Blend);
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            _gl.Enable(EnableCap.DepthTest);
            _gl.DepthFunc(DepthFunction.Lequal);
            _gl.Disable(EnableCap.CullFace);

            _shader.Use();

            foreach (var kvp in _cameraFrustums)
            {
                uint entityId = kvp.Key;
                Vector4 color = kvp.Value;

                if (!_componentProvider.HasComponent<CameraComponent>(entityId) ||
                    !_componentProvider.HasComponent<TransformComponent>(entityId))
                    continue;

                var cameraComponent = _componentProvider.GetComponent<CameraComponent>(entityId);
                var transformComponent = _componentProvider.GetComponent<TransformComponent>(entityId);

                Vector3[] frustumCorners = CalculateFrustumCorners(transformComponent, cameraComponent);

                _shader.UpdateFrustumVertices(frustumCorners);

                _shader.SetMVP(Matrix4x4.Identity, view, projection);
                _shader.SetColor(color);

                _shader.Draw();
            }

            if (blendingEnabled == GLEnum.False) _gl.Disable(EnableCap.Blend);
            else _gl.Enable(EnableCap.Blend);

            if (depthTestEnabled == GLEnum.False) _gl.Disable(EnableCap.DepthTest);
            else _gl.Enable(EnableCap.DepthTest);

            if (cullFaceEnabled == GLEnum.True) _gl.Enable(EnableCap.CullFace);
            else _gl.Disable(EnableCap.CullFace);
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

        public void FreeCache()
        {
            _cameraFrustums.Clear();
        }

        public void Dispose()
        {
            _shader?.Dispose();
            _cameraFrustums.Clear();
        }
    }
}