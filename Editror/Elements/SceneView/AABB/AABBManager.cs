using System.Collections.Generic;
using AtomEngine.RenderEntity;
using Silk.NET.OpenGL;
using System.Numerics;
using AtomEngine;
using System;

namespace Editor
{
    internal class AABBManager : IDisposable
    {
        private readonly Dictionary<uint, AABBInfo> _aabbInfos = new Dictionary<uint, AABBInfo>();
        private readonly IEntityComponentInfoProvider _componentProvider;
        private readonly GL _gl;
        private AABBShader _shader;
        private bool _isInitialized = false;
        private bool _isVisible = true;
        private Vector4 _defaultColor = new Vector4(0.0f, 1.0f, 0.0f, 0.5f);

        private class AABBInfo
        {
            public MeshBase Mesh { get; set; }
            public Vector4 Color { get; set; }
            public BoundingBox BoundingBox { get; set; }
        }

        public AABBManager(GL gl, IEntityComponentInfoProvider componentProvider)
        {
            _gl = gl;
            _componentProvider = componentProvider;
            InitializeShader();
        }

        private void InitializeShader()
        {
            try
            {
                _shader = new AABBShader(_gl);
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка инициализации AABBShader: {ex.Message}");
                _isInitialized = false;
            }
        }

        public void AddEntity(uint entityId, MeshBase mesh)
        {
            if (mesh?.BoundingVolume == null)
                return;

            if (_componentProvider.HasComponent<TransformComponent>(entityId))
            {
                var transformComponent = _componentProvider.GetComponent<TransformComponent>(entityId);
                Matrix4x4 modelMatrix = CreateModelMatrix(transformComponent);

                if (mesh.BoundingVolume.Transform(modelMatrix) is BoundingBox worldBounds)
                {
                    _aabbInfos[entityId] = new AABBInfo
                    {
                        Mesh = mesh,
                        BoundingBox = worldBounds,
                        Color = _defaultColor
                    };
                }
            }
        }

        public void RemoveEntity(uint entityId)
        {
            if (!_aabbInfos.TryGetValue(entityId, out var aabbInfo))
            {
                return;
            }
            _aabbInfos.Remove(entityId);
        }

        public void UpdateEntity(uint entityId, MeshBase mesh = null)
        {
            if (!_aabbInfos.TryGetValue(entityId, out var aabbInfo))
            {
                return;
            }

            if (!_componentProvider.HasComponent<TransformComponent>(entityId))
            {
                RemoveEntity(entityId);
                return;
            }

            if (_aabbInfos.ContainsKey(entityId))
            {
                var info = _aabbInfos[entityId];
                MeshBase meshToUse = mesh ?? info.Mesh;

                if (meshToUse?.BoundingVolume == null)
                {
                    RemoveEntity(entityId);
                    return;
                }

                var transformComponent = _componentProvider.GetComponent<TransformComponent>(entityId);
                Matrix4x4 modelMatrix = CreateModelMatrix(transformComponent);

                if (meshToUse.BoundingVolume.Transform(modelMatrix) is BoundingBox worldBounds)
                {
                    info.BoundingBox = worldBounds;
                    info.Mesh = meshToUse;
                }
            }
            else if (mesh != null)
            {
                AddEntity(entityId, mesh);
            }
        }

        public void SetEntityColor(uint entityId, Vector4 color)
        {
            if (_aabbInfos.ContainsKey(entityId))
            {
                _aabbInfos[entityId].Color = color;
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

            if (_aabbInfos.Count == 0)
            {
                return;
            }
            GLEnum blendingEnabled = _gl.GetBoolean(GetPName.Blend) ? GLEnum.True : GLEnum.False;
            GLEnum depthTestEnabled = _gl.GetBoolean(GetPName.DepthTest) ? GLEnum.True : GLEnum.False;
            GLEnum cullFaceEnabled = _gl.GetBoolean(GetPName.CullFace) ? GLEnum.True : GLEnum.False;

            _gl.Enable(EnableCap.Blend);
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            _gl.Enable(EnableCap.DepthTest);
            _gl.DepthFunc(DepthFunction.Lequal);

            _gl.Disable(EnableCap.CullFace);

            foreach (var kvp in _aabbInfos)
            {
                uint entityId = kvp.Key;
                AABBInfo info = kvp.Value;

                if (!_componentProvider.HasComponent<TransformComponent>(entityId))
                    continue;

                Matrix4x4 modelMatrix = Matrix4x4.Identity;
                _shader.DrawAABB(modelMatrix, info.BoundingBox.Min, info.BoundingBox.Max, info.Color, view, projection);
            }

            if (blendingEnabled == GLEnum.False) _gl.Disable(EnableCap.Blend);
            else _gl.Enable(EnableCap.Blend);

            if (depthTestEnabled == GLEnum.False) _gl.Disable(EnableCap.DepthTest);
            else _gl.Enable(EnableCap.DepthTest);

            if (cullFaceEnabled == GLEnum.True) _gl.Enable(EnableCap.CullFace);
            else _gl.Disable(EnableCap.CullFace);
        }

        private Matrix4x4 CreateModelMatrix(TransformComponent transform)
        {
            Matrix4x4 result = Matrix4x4.Identity;
            Matrix4x4 translationMatrix = Matrix4x4.CreateTranslation(transform.Position);

            Matrix4x4 rotationMatrix = Matrix4x4.Identity;
            if (transform.Rotation != Vector3.Zero)
            {
                // Конвертируем градусы в радианы
                float yawRad = transform.Rotation.Y * (MathF.PI / 180.0f);
                float pitchRad = transform.Rotation.X * (MathF.PI / 180.0f);
                float rollRad = transform.Rotation.Z * (MathF.PI / 180.0f);

                rotationMatrix = Matrix4x4.CreateFromYawPitchRoll(yawRad, pitchRad, rollRad);
            }

            Matrix4x4 scaleMatrix = Matrix4x4.CreateScale(transform.Scale);

            result *= rotationMatrix;
            result *= translationMatrix;
            result *= scaleMatrix;

            return result;
            //return scaleMatrix * rotationMatrix * translationMatrix;
        }

        internal void FreeCache()
        {
            _aabbInfos.Clear();
        }

        public void Dispose()
        {
            _shader?.Dispose();
            _aabbInfos.Clear();
        }

    }
}