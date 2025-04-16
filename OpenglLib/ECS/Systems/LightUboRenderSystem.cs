using System.Runtime.InteropServices;
using System.Numerics;
using Silk.NET.OpenGL;
using AtomEngine;
using EngineLib;
using OpenglLib.Buffers;

namespace OpenglLib
{
    public class LightUboRenderSystem : IRenderSystem
    {
        public IWorld World { get; set; }

        private QueryEntity queryLightEntities;
        private QueryEntity queryGlobalLightSettings;
        private QueryEntity queryCameraEntities;

        private UboService _uboService;
        private LightsUboData _uboData;
        private bool _isDirty = true;

        private float _shadowNearPlane = 0.1f;
        private float _shadowFarPlane = 50.0f;
        private float _shadowOrthoSize = 10.0f;

        public LightUboRenderSystem(IWorld world)
        {
            World = world;

            queryLightEntities = this.CreateEntityQuery()
                .With<LightComponent>()
                .With<TransformComponent>()
                ;

            queryGlobalLightSettings = this.CreateEntityQuery()
                .With<GlobalLightSettingsComponent>()
                ;

            queryCameraEntities = this.CreateEntityQuery()
                .With<CameraComponent>()
                .With<TransformComponent>()
                ;

            InitializeUboData();
        }

        private void InitializeUboData()
        {
            _uboData = new LightsUboData
            {
                AmbientColor = new Vector3(0.1f, 0.1f, 0.1f),
                AmbientIntensity = 0.1f,
                ShadowBias = 0.005f,
                PcfKernelSize = 3,
                ShadowIntensity = 0.7f,
                NumDirectionalLights = 0,
                NumPointLights = 0
            };
        }

        public void Initialize()
        {
            _uboService = ServiceHub.Get<UboService>();
        }

        public void Render(double deltaTime, object? context)
        {
            if (!_uboService.HasUboByBindingPoint(1))
                return;

            bool globalSettingsChanged = ProcessGlobalLightSettings();

            bool lightsChanged = ProcessLights();

            if (globalSettingsChanged || lightsChanged || _isDirty)
            {
                _uboService.SetUboDataByBindingPoint(1, _uboData);
                _isDirty = false;
            }
        }

        private bool ProcessGlobalLightSettings()
        {
            bool changed = false;

            Entity[] globalSettingsEntities = queryGlobalLightSettings.Build();

            if (globalSettingsEntities.Length > 0)
            {
                ref var globalSettings = ref this.GetComponent<GlobalLightSettingsComponent>(globalSettingsEntities[0]);

                if (globalSettings.IsDirty)
                {
                    _uboData.AmbientColor = globalSettings.AmbientColor;
                    _uboData.AmbientIntensity = globalSettings.AmbientIntensity;
                    _uboData.ShadowBias = globalSettings.ShadowBias;
                    _uboData.PcfKernelSize = globalSettings.PcfKernelSize;
                    _uboData.ShadowIntensity = globalSettings.ShadowIntensity;

                    globalSettings.MakeClean();
                    changed = true;
                }
            }

            return changed;
        }

        private bool ProcessLights()
        {
            bool changed = false;

            int directionalLightCount = 0;
            int pointLightCount = 0;

            Entity[] lightEntities = queryLightEntities.Build();

            foreach (var entity in lightEntities)
            {
                ref var lightComponent = ref this.GetComponent<LightComponent>(entity);
                ref var transformComponent = ref this.GetComponent<TransformComponent>(entity);

                if (lightComponent.Type == LightType.Directional && lightComponent.Enabled > 0.5f && directionalLightCount < LightParams.MAX_DIRECTIONAL_LIGHTS)
                {
                    Matrix4x4 rotationMatrix = transformComponent.GetRotationMatrix();
                    Vector3 direction = Vector3.Transform(new Vector3(0, 0, 1), rotationMatrix);

                    UpdateDirectionalLight(ref lightComponent, ref transformComponent, direction, directionalLightCount);
                    directionalLightCount++;

                    if (lightComponent.IsDirty)
                    {
                        changed = true;
                        lightComponent.MakeClean();
                    }
                }
            }

            foreach (var entity in lightEntities)
            {
                ref var lightComponent = ref this.GetComponent<LightComponent>(entity);
                ref var transformComponent = ref this.GetComponent<TransformComponent>(entity);

                if (lightComponent.Type == LightType.Point && lightComponent.Enabled > 0.5f && pointLightCount < LightParams.MAX_POINT_LIGHTS)
                {
                    UpdatePointLight(ref lightComponent, ref transformComponent, pointLightCount);
                    pointLightCount++;

                    if (lightComponent.IsDirty)
                    {
                        changed = true;
                        lightComponent.MakeClean();
                    }
                }
            }

            if (_uboData.NumDirectionalLights != directionalLightCount)
            {
                _uboData.NumDirectionalLights = directionalLightCount;
                changed = true;
            }

            if (_uboData.NumPointLights != pointLightCount)
            {
                _uboData.NumPointLights = pointLightCount;
                changed = true;
            }

            return changed;
        }

        private void UpdateDirectionalLight(ref LightComponent light, ref TransformComponent transform, Vector3 direction, int index)
        {
            direction = Vector3.Normalize(direction);
            DirectionalLightData directionalLight;

            switch (index)
            {
                case 0: directionalLight = _uboData.DirectionalLight0; break;
                case 1: directionalLight = _uboData.DirectionalLight1; break;
                case 2: directionalLight = _uboData.DirectionalLight2; break;
                case 3: directionalLight = _uboData.DirectionalLight3; break;
                default: return;
            }

            directionalLight.Direction = direction;
            directionalLight.Color = light.Color;
            directionalLight.Intensity = light.Intensity;
            directionalLight.CastShadows = light.CastShadows ? 1.0f : 0.0f;
            directionalLight.LightSpaceMatrix = Matrix4x4.Identity;
            directionalLight.Enabled = light.Enabled;
            directionalLight.LightId = light.LightId;

            if (light.CastShadows)
            {
                CameraComponent activeCamera = default;
                TransformComponent cameraTransform = default;
                bool foundActiveCamera = false;

                Entity[] cameraEntities = queryCameraEntities.Build();
                foreach (var entity in cameraEntities)
                {
                    ref var camera = ref this.GetComponent<CameraComponent>(entity);
                    if (camera.IsActive)
                    {
                        activeCamera = camera;
                        cameraTransform = this.GetComponent<TransformComponent>(entity);
                        foundActiveCamera = true;
                        break;
                    }
                }

                float nearPlane = foundActiveCamera ? activeCamera.NearPlane : _shadowNearPlane;
                float farPlane = foundActiveCamera ? activeCamera.FarPlane : _shadowFarPlane;

                float orthoSize = _shadowOrthoSize;
                if (foundActiveCamera)
                {
                    float distance = Vector3.Distance(cameraTransform.Position, Vector3.Zero);
                    float aspectRatio = activeCamera.AspectRatio;
                    float fovY = activeCamera.FieldOfView * (MathF.PI / 180.0f);
                    float halfHeight = distance * MathF.Tan(fovY / 2.0f);
                    float halfWidth = halfHeight * aspectRatio;

                    orthoSize = MathF.Max(halfWidth, halfHeight) * 2.0f;
                }

                Vector3 up = Vector3.UnitY;
                if (Math.Abs(Vector3.Dot(direction, up)) > 0.99f)
                    up = Vector3.UnitZ;

                Vector3 right = Vector3.Cross(up, direction);
                right = Vector3.Normalize(right);
                up = Vector3.Cross(direction, right);
                up = Vector3.Normalize(up);

                Vector3 targetPos = foundActiveCamera ? cameraTransform.Position : Vector3.Zero;
                Vector3 lightPos = targetPos - direction * farPlane * 0.5f;

                Matrix4x4 lightView = Matrix4x4.CreateLookAt(lightPos, targetPos, up);

                Matrix4x4 lightProjection = Matrix4x4.CreateOrthographicOffCenter(
                    -orthoSize, orthoSize,
                    -orthoSize, orthoSize,
                    nearPlane, farPlane);

                directionalLight.LightSpaceMatrix = lightView * lightProjection;
            }
            light.LightSpaceMatrix = directionalLight.LightSpaceMatrix;


            switch (index)
            {
                case 0: _uboData.DirectionalLight0 = directionalLight; break;
                case 1: _uboData.DirectionalLight1 = directionalLight; break;
                case 2: _uboData.DirectionalLight2 = directionalLight; break;
                case 3: _uboData.DirectionalLight3 = directionalLight; break;
            }
        }

        private void UpdatePointLight(ref LightComponent light, ref TransformComponent transform, int index)
        {
            Vector3 position = transform.Position;
            PointLightData pointLight;

            switch (index)
            {
                case 0: pointLight = _uboData.PointLight0; break;
                case 1: pointLight = _uboData.PointLight1; break;
                case 2: pointLight = _uboData.PointLight2; break;
                case 3: pointLight = _uboData.PointLight3; break;
                case 4: pointLight = _uboData.PointLight4; break;
                case 5: pointLight = _uboData.PointLight5; break;
                case 6: pointLight = _uboData.PointLight6; break;
                case 7: pointLight = _uboData.PointLight7; break;
                default: return;
            }

            pointLight.Position = position;
            pointLight.Color = light.Color;
            pointLight.Intensity = light.Intensity;
            pointLight.Radius = light.Radius;
            pointLight.CastShadows = light.CastShadows ? 1.0f : 0.0f;
            pointLight.FalloffExponent = light.FalloffExponent;
            pointLight.Enabled = light.Enabled;

            switch (index)
            {
                case 0: _uboData.PointLight0 = pointLight; break;
                case 1: _uboData.PointLight1 = pointLight; break;
                case 2: _uboData.PointLight2 = pointLight; break;
                case 3: _uboData.PointLight3 = pointLight; break;
                case 4: _uboData.PointLight4 = pointLight; break;
                case 5: _uboData.PointLight5 = pointLight; break;
                case 6: _uboData.PointLight6 = pointLight; break;
                case 7: _uboData.PointLight7 = pointLight; break;
            }
        }

        public void Resize(Vector2 size)
        {
            
        }
    }
}
