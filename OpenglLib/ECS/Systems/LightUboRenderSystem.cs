using System.Numerics;
using AtomEngine;
using EngineLib;

namespace OpenglLib
{
    public class LightUboRenderSystem : IRenderSystem
    {
        const uint UBO_BINDING_POINT = 1;

        const string UBO_NAME = "LightsUBO";

        private readonly string[] DIRECTIONAL_LIGHT_DOMAINS = new string[LightParams.MAX_DIRECTIONAL_LIGHTS];

        const string DIRECTION_SUBDOMAIN = "direction";
        const string COLOR_SUBDOMAIN = "color";
        const string INTENSITY_SUBDOMAIN = "intensity";
        const string CAST_SHADOWS_SUBDOMAIN = "castShadows";
        const string ENABLED_SUBDOMAIN = "enabled";
        const string LIGHT_ID_SUBDOMAIN = "lightId";

        const string NUM_CASCADES_SUBDOMAIN = "numCascades";
        private readonly string[][] CASCADE_DOMAINS = new string[LightParams.MAX_DIRECTIONAL_LIGHTS][];

        const string LIGHT_SPACE_MATRIX_SUBDOMAIN = "lightSpaceMatrix";
        const string SPLIT_DEPTH_SUBDOMAIN = "splitDepth";

        private readonly string[] POINT_LIGHT_DOMAINS = new string[LightParams.MAX_POINT_LIGHTS];

        const string POSITION_SUBDOMAIN = "position";
        const string RADIUS_SUBDOMAIN = "radius";
        const string FALLOFF_EXPONENT_SUBDOMAIN = "falloffExponent";

        const string AMBIENT_COLOR_DOMAIN = "ambientColor";
        const string AMBIENT_INTENSITY_DOMAIN = "ambientIntensity";
        const string NUM_DIRECTIONAL_LIGHTS_DOMAIN = "numDirectionalLights";
        const string NUM_POINT_LIGHTS_DOMAIN = "numPointLights";
        const string SHADOW_BIAS_DOMAIN = "shadowBias";
        const string PCF_KERNEL_SIZE_DOMAIN = "pcfKernelSize";
        const string SHADOW_INTENSITY_DOMAIN = "shadowIntensity";

        public IWorld World { get; set; }

        private QueryEntity queryLightEntities;
        private QueryEntity queryGlobalLightSettings;
        private QueryEntity queryCameraEntities;

        private UboService _uboService;
        private Dictionary<string, object> valuePairs = new Dictionary<string, object>();
        private bool _isDirty = true;

        Vector4[] ndcCorners = new Vector4[8]
        {
            new Vector4(-1, 1, 0, 1),
            new Vector4(1, 1, 0, 1),
            new Vector4(-1, -1, 0, 1),
            new Vector4(1, -1, 0, 1),
            new Vector4(-1, 1, 1, 1),
            new Vector4(1, 1, 1, 1),
            new Vector4(-1, -1, 1, 1),
            new Vector4(1, -1, 1, 1)
        };

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

            for (int i = 0; i < LightParams.MAX_DIRECTIONAL_LIGHTS; i++)
            {
                DIRECTIONAL_LIGHT_DOMAINS[i] = $"{UBO_NAME}.directionalLights[{i}]";
                CASCADE_DOMAINS[i] = new string[LightParams.MAX_CASCADES];

                for (int j = 0; j < LightParams.MAX_CASCADES; j++)
                {
                    CASCADE_DOMAINS[i][j] = $"{DIRECTIONAL_LIGHT_DOMAINS[i]}.cascades[{j}]";
                }
            }

            for (int i = 0; i < LightParams.MAX_POINT_LIGHTS; i++)
            {
                POINT_LIGHT_DOMAINS[i] = $"{UBO_NAME}.pointLights[{i}]";
            }

            InitializeUboData();
        }

        private void InitializeUboData()
        {
            valuePairs[$"{UBO_NAME}.{AMBIENT_COLOR_DOMAIN}"] = new Vector3(0.1f, 0.1f, 0.1f);
            valuePairs[$"{UBO_NAME}.{AMBIENT_INTENSITY_DOMAIN}"] = 0.1f;
            valuePairs[$"{UBO_NAME}.{SHADOW_BIAS_DOMAIN}"] = 0.005f;
            valuePairs[$"{UBO_NAME}.{PCF_KERNEL_SIZE_DOMAIN}"] = 3;
            valuePairs[$"{UBO_NAME}.{SHADOW_INTENSITY_DOMAIN}"] = 0.7f;
            valuePairs[$"{UBO_NAME}.{NUM_DIRECTIONAL_LIGHTS_DOMAIN}"] = 0;
            valuePairs[$"{UBO_NAME}.{NUM_POINT_LIGHTS_DOMAIN}"] = 0;

            for (int i = 0; i < LightParams.MAX_DIRECTIONAL_LIGHTS; i++)
            {
                string lightRoot = DIRECTIONAL_LIGHT_DOMAINS[i];

                valuePairs[$"{lightRoot}.{DIRECTION_SUBDOMAIN}"] = new Vector3(0, -1, 0);
                valuePairs[$"{lightRoot}.{COLOR_SUBDOMAIN}"] = new Vector3(1, 1, 1);
                valuePairs[$"{lightRoot}.{INTENSITY_SUBDOMAIN}"] = 0.0f;
                valuePairs[$"{lightRoot}.{CAST_SHADOWS_SUBDOMAIN}"] = 0.0f;
                valuePairs[$"{lightRoot}.{ENABLED_SUBDOMAIN}"] = 0.0f;
                valuePairs[$"{lightRoot}.{LIGHT_ID_SUBDOMAIN}"] = i;
                valuePairs[$"{lightRoot}.{NUM_CASCADES_SUBDOMAIN}"] = 0;

                for (int j = 0; j < LightParams.MAX_CASCADES; j++)
                {
                    string cascadeRoot = CASCADE_DOMAINS[i][j];

                    valuePairs[$"{cascadeRoot}.{LIGHT_SPACE_MATRIX_SUBDOMAIN}"] = Matrix4x4.Identity;
                    valuePairs[$"{cascadeRoot}.{SPLIT_DEPTH_SUBDOMAIN}"] = 0.0f;
                }
            }

            for (int i = 0; i < LightParams.MAX_POINT_LIGHTS; i++)
            {
                string lightRoot = POINT_LIGHT_DOMAINS[i];

                valuePairs[$"{lightRoot}.{POSITION_SUBDOMAIN}"] = Vector3.Zero;
                valuePairs[$"{lightRoot}.{COLOR_SUBDOMAIN}"] = new Vector3(1, 1, 1);
                valuePairs[$"{lightRoot}.{INTENSITY_SUBDOMAIN}"] = 0.0f;
                valuePairs[$"{lightRoot}.{RADIUS_SUBDOMAIN}"] = 10.0f;
                valuePairs[$"{lightRoot}.{CAST_SHADOWS_SUBDOMAIN}"] = 0.0f;
                valuePairs[$"{lightRoot}.{FALLOFF_EXPONENT_SUBDOMAIN}"] = 1.0f;
                valuePairs[$"{lightRoot}.{ENABLED_SUBDOMAIN}"] = 0.0f;
                valuePairs[$"{lightRoot}.{LIGHT_ID_SUBDOMAIN}"] = i;
            }

        }

        public void Initialize()
        {
            _uboService = ServiceHub.Get<UboService>();
            _uboService.SetUboDataByBindingPoint(UBO_BINDING_POINT, valuePairs);
            _uboService.Update(UBO_BINDING_POINT);
        }

        public void Render(double deltaTime, object? context)
        {
            if (!_uboService.HasUboByBindingPoint(UBO_BINDING_POINT))
                return;

            bool globalSettingsChanged = ProcessGlobalLightSettings();
            bool lightsChanged = ProcessLights();

            if (globalSettingsChanged || lightsChanged || _isDirty)
            {
                _uboService.SetUboDataByBindingPoint(UBO_BINDING_POINT, valuePairs);
                _uboService.Update(UBO_BINDING_POINT);
                _isDirty = false;
            }
        }

        private Vector3 GetLightDirectionFromRotation(Vector3 rotation)
        {
            float pitchRad = rotation.X.DegreesToRadians();
            float yawRad = rotation.Y.DegreesToRadians();

            Vector3 direction = new Vector3(
                MathF.Sin(yawRad) * MathF.Cos(pitchRad),
                -MathF.Sin(pitchRad),
                MathF.Cos(yawRad) * MathF.Cos(pitchRad)
            );

            return Vector3.Normalize(direction);
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
                    valuePairs[$"{UBO_NAME}.{AMBIENT_COLOR_DOMAIN}"] = globalSettings.AmbientColor;
                    valuePairs[$"{UBO_NAME}.{AMBIENT_INTENSITY_DOMAIN}"] = globalSettings.AmbientIntensity;
                    valuePairs[$"{UBO_NAME}.{SHADOW_BIAS_DOMAIN}"] = globalSettings.ShadowBias;
                    valuePairs[$"{UBO_NAME}.{PCF_KERNEL_SIZE_DOMAIN}"] = globalSettings.PcfKernelSize;
                    valuePairs[$"{UBO_NAME}.{SHADOW_INTENSITY_DOMAIN}"] = globalSettings.ShadowIntensity;

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

            for (int i = 0; i < LightParams.MAX_DIRECTIONAL_LIGHTS; i++)
            {
                string lightRoot = DIRECTIONAL_LIGHT_DOMAINS[i];
                valuePairs[$"{lightRoot}.{ENABLED_SUBDOMAIN}"] = 0.0f;
            }

            for (int i = 0; i < LightParams.MAX_POINT_LIGHTS; i++)
            {
                string lightRoot = POINT_LIGHT_DOMAINS[i];
                valuePairs[$"{lightRoot}.{ENABLED_SUBDOMAIN}"] = 0.0f;
            }

            foreach (var entity in lightEntities)
            {
                ref var lightComponent = ref this.GetComponent<LightComponent>(entity);
                ref var transformComponent = ref this.GetComponent<TransformComponent>(entity);

                if (lightComponent.Type == LightType.Directional &&
                    lightComponent.Enabled > 0.5f &&
                    directionalLightCount < LightParams.MAX_DIRECTIONAL_LIGHTS)
                {
                    Vector3 direction = GetLightDirectionFromRotation(transformComponent.Rotation);
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

                if (lightComponent.Type == LightType.Point &&
                    lightComponent.Enabled > 0.5f &&
                    pointLightCount < LightParams.MAX_POINT_LIGHTS)
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

            if (!valuePairs[$"{UBO_NAME}.{NUM_DIRECTIONAL_LIGHTS_DOMAIN}"].Equals(directionalLightCount))
            {
                valuePairs[$"{UBO_NAME}.{NUM_DIRECTIONAL_LIGHTS_DOMAIN}"] = directionalLightCount;
                changed = true;
            }

            if (!valuePairs[$"{UBO_NAME}.{NUM_POINT_LIGHTS_DOMAIN}"].Equals(pointLightCount))
            {
                valuePairs[$"{UBO_NAME}.{NUM_POINT_LIGHTS_DOMAIN}"] = pointLightCount;
                changed = true;
            }

            return changed;
        }

        private void UpdateDirectionalLight(ref LightComponent light, ref TransformComponent transform, Vector3 direction, int index)
        {
            direction = Vector3.Normalize(direction);
            string lightRoot = DIRECTIONAL_LIGHT_DOMAINS[index];

            valuePairs[$"{lightRoot}.{DIRECTION_SUBDOMAIN}"] = direction;
            valuePairs[$"{lightRoot}.{COLOR_SUBDOMAIN}"] = light.Color;
            valuePairs[$"{lightRoot}.{INTENSITY_SUBDOMAIN}"] = light.Intensity;
            valuePairs[$"{lightRoot}.{CAST_SHADOWS_SUBDOMAIN}"] = light.CastShadows ? 1.0f : 0.0f;
            valuePairs[$"{lightRoot}.{ENABLED_SUBDOMAIN}"] = light.Enabled;
            valuePairs[$"{lightRoot}.{LIGHT_ID_SUBDOMAIN}"] = light.LightId;

            valuePairs[$"{lightRoot}.{NUM_CASCADES_SUBDOMAIN}"] = 0;

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

                if (foundActiveCamera)
                {
                    Vector3[] frustumCorners = CalculateFrustumCorners(activeCamera, cameraTransform);

                    float[] cascadeSplits = CascadedShadowCalculator.CalculateCascadeSplits(
                        activeCamera.NearPlane, activeCamera.FarPlane);

                    int numCascades = LightParams.MAX_CASCADES;
                    valuePairs[$"{lightRoot}.{NUM_CASCADES_SUBDOMAIN}"] = numCascades;

                    for (int cascadeIdx = 0; cascadeIdx < numCascades; cascadeIdx++)
                    {
                        float startSplit = cascadeIdx == 0 ? 0.0f : cascadeSplits[cascadeIdx - 1];
                        float endSplit = cascadeSplits[cascadeIdx];

                        Vector3[] cascadeFrustum = CascadedShadowCalculator.GetCascadeFrustumCorners(
                            frustumCorners, startSplit, endSplit);

                        Matrix4x4 lightSpaceMatrix = CascadedShadowCalculator.CalculateLightSpaceMatrix(
                            cascadeFrustum, direction, transform.Position);

                        string _cascadeRoot = CASCADE_DOMAINS[index][cascadeIdx];
                        valuePairs[$"{_cascadeRoot}.{LIGHT_SPACE_MATRIX_SUBDOMAIN}"] = lightSpaceMatrix;
                        valuePairs[$"{_cascadeRoot}.{SPLIT_DEPTH_SUBDOMAIN}"] = endSplit;
                    }
                }
                else
                {
                    Vector3 sceneCenter = Vector3.Zero;
                    float sceneRadius = 20.0f;

                    Vector3 up = Vector3.UnitY;
                    if (Math.Abs(Vector3.Dot(direction, up)) > 0.99f)
                        up = Vector3.UnitZ;

                    Matrix4x4 lightView = Matrix4x4.CreateLookAt(
                        sceneCenter - direction * sceneRadius,
                        sceneCenter,
                        up
                    );

                    Matrix4x4 lightProjection = Matrix4x4.CreateOrthographicOffCenter(
                        -sceneRadius, sceneRadius,
                        -sceneRadius, sceneRadius,
                        0.1f, sceneRadius * 2.0f
                    );

                    Matrix4x4 lightSpaceMatrix = lightView * lightProjection;

                    string _cascadeRoot = CASCADE_DOMAINS[index][0];
                    valuePairs[$"{_cascadeRoot}.{LIGHT_SPACE_MATRIX_SUBDOMAIN}"] = lightSpaceMatrix;
                    valuePairs[$"{_cascadeRoot}.{SPLIT_DEPTH_SUBDOMAIN}"] = 1.0f;

                    valuePairs[$"{lightRoot}.{NUM_CASCADES_SUBDOMAIN}"] = 1;
                }

                string cascadeRoot = CASCADE_DOMAINS[index][0];
            }
        }

        private void UpdatePointLight(ref LightComponent light, ref TransformComponent transform, int index)
        {
            string lightRoot = POINT_LIGHT_DOMAINS[index];

            valuePairs[$"{lightRoot}.{POSITION_SUBDOMAIN}"] = transform.Position;
            valuePairs[$"{lightRoot}.{COLOR_SUBDOMAIN}"] = light.Color;
            valuePairs[$"{lightRoot}.{INTENSITY_SUBDOMAIN}"] = light.Intensity;
            valuePairs[$"{lightRoot}.{RADIUS_SUBDOMAIN}"] = light.Radius;
            valuePairs[$"{lightRoot}.{CAST_SHADOWS_SUBDOMAIN}"] = light.CastShadows ? 1.0f : 0.0f;
            valuePairs[$"{lightRoot}.{FALLOFF_EXPONENT_SUBDOMAIN}"] = light.FalloffExponent;
            valuePairs[$"{lightRoot}.{ENABLED_SUBDOMAIN}"] = light.Enabled;
            valuePairs[$"{lightRoot}.{LIGHT_ID_SUBDOMAIN}"] = light.LightId;
        }

        public Vector3[] CalculateFrustumCorners(CameraComponent camera, TransformComponent cameraTrans)
        {
            Matrix4x4 view = Matrix4x4.CreateLookAt(
                cameraTrans.Position,
                cameraTrans.Position + Vector3.Transform(Vector3.UnitZ, cameraTrans.GetRotationMatrix()),
                camera.CameraUp
            );

            Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(
                camera.FieldOfView * (MathF.PI / 180.0f),
                camera.AspectRatio,
                camera.NearPlane,
                camera.FarPlane
            );

            Matrix4x4.Invert(view * projection, out Matrix4x4 invViewProj);
            Vector3[] corners = new Vector3[8];

            for (int i = 0; i < 8; i++)
            {
                Vector4 worldPos = Vector4.Transform(ndcCorners[i], invViewProj);
                corners[i] = new Vector3(
                    worldPos.X / worldPos.W,
                    worldPos.Y / worldPos.W,
                    worldPos.Z / worldPos.W
                );
            }

            return corners;
        }

        public void Resize(Vector2 size)
        {
        }
    }


    //public class LightUboRenderSystem : IRenderSystem
    //{
    //    public IWorld World { get; set; }

    //    private QueryEntity queryLightEntities;
    //    private QueryEntity queryGlobalLightSettings;
    //    private QueryEntity queryCameraEntities;

    //    private UboService _uboService;
    //    private LightsUboData _uboData;
    //    private bool _isDirty = true;

    //    private float _shadowNearPlane = 0.1f;
    //    private float _shadowFarPlane = 50.0f;
    //    private float _shadowOrthoSize = 10.0f;

    //    public LightUboRenderSystem(IWorld world)
    //    {
    //        World = world;

    //        queryLightEntities = this.CreateEntityQuery()
    //            .With<LightComponent>()
    //            .With<TransformComponent>()
    //            ;

    //        queryGlobalLightSettings = this.CreateEntityQuery()
    //            .With<GlobalLightSettingsComponent>()
    //            ;

    //        queryCameraEntities = this.CreateEntityQuery()
    //            .With<CameraComponent>()
    //            .With<TransformComponent>()
    //            ;

    //        InitializeUboData();
    //    }

    //    private void InitializeUboData()
    //    {
    //        _uboData = new LightsUboData
    //        {
    //            AmbientColor = new Vector3(0.1f, 0.1f, 0.1f),
    //            AmbientIntensity = 0.1f,
    //            ShadowBias = 0.005f,
    //            PcfKernelSize = 3,
    //            ShadowIntensity = 0.7f,
    //            NumDirectionalLights = 0,
    //            NumPointLights = 0
    //        };
    //    }

    //    public void Initialize()
    //    {
    //        _uboService = ServiceHub.Get<UboService>();
    //    }

    //    public void Render(double deltaTime, object? context)
    //    {
    //        if (!_uboService.HasUboByBindingPoint(1))
    //            return;

    //        bool globalSettingsChanged = ProcessGlobalLightSettings();

    //        bool lightsChanged = ProcessLights();

    //        if (globalSettingsChanged || lightsChanged || _isDirty)
    //        {
    //            _uboService.SetUboDataByBindingPoint(1, _uboData);
    //            _isDirty = false;
    //        }
    //    }
    //    private Vector3 GetLightDirectionFromRotation(Vector3 rotation)
    //    {
    //        float pitchRad = rotation.X.DegreesToRadians();
    //        float yawRad = rotation.Y.DegreesToRadians();

    //        Vector3 direction = new Vector3(
    //            MathF.Sin(yawRad) * MathF.Cos(pitchRad),
    //            -MathF.Sin(pitchRad),
    //            MathF.Cos(yawRad) * MathF.Cos(pitchRad)
    //        );

    //        return Vector3.Normalize(direction);
    //    }
    //    private bool ProcessGlobalLightSettings()
    //    {
    //        bool changed = false;

    //        Entity[] globalSettingsEntities = queryGlobalLightSettings.Build();

    //        if (globalSettingsEntities.Length > 0)
    //        {
    //            ref var globalSettings = ref this.GetComponent<GlobalLightSettingsComponent>(globalSettingsEntities[0]);

    //            if (globalSettings.IsDirty)
    //            {
    //                _uboData.AmbientColor = globalSettings.AmbientColor;
    //                _uboData.AmbientIntensity = globalSettings.AmbientIntensity;
    //                _uboData.ShadowBias = globalSettings.ShadowBias;
    //                _uboData.PcfKernelSize = globalSettings.PcfKernelSize;
    //                _uboData.ShadowIntensity = globalSettings.ShadowIntensity;

    //                globalSettings.MakeClean();
    //                changed = true;
    //            }
    //        }

    //        return changed;
    //    }

    //    private bool ProcessLights()
    //    {
    //        bool changed = false;

    //        int directionalLightCount = 0;
    //        int pointLightCount = 0;

    //        Entity[] lightEntities = queryLightEntities.Build();

    //        foreach (var entity in lightEntities)
    //        {
    //            ref var lightComponent = ref this.GetComponent<LightComponent>(entity);
    //            ref var transformComponent = ref this.GetComponent<TransformComponent>(entity);

    //            if (lightComponent.Type == LightType.Directional && lightComponent.Enabled > 0.5f && directionalLightCount < LightParams.MAX_DIRECTIONAL_LIGHTS)
    //            {
    //                Vector3 direction = GetLightDirectionFromRotation(transformComponent.Rotation);

    //                UpdateDirectionalLight(ref lightComponent, ref transformComponent, direction, directionalLightCount);
    //                directionalLightCount++;

    //                if (lightComponent.IsDirty)
    //                {
    //                    changed = true;
    //                    lightComponent.MakeClean();
    //                }
    //            }
    //        }

    //        foreach (var entity in lightEntities)
    //        {
    //            ref var lightComponent = ref this.GetComponent<LightComponent>(entity);
    //            ref var transformComponent = ref this.GetComponent<TransformComponent>(entity);

    //            if (lightComponent.Type == LightType.Point && lightComponent.Enabled > 0.5f && pointLightCount < LightParams.MAX_POINT_LIGHTS)
    //            {
    //                UpdatePointLight(ref lightComponent, ref transformComponent, pointLightCount);
    //                pointLightCount++;

    //                if (lightComponent.IsDirty)
    //                {
    //                    changed = true;
    //                    lightComponent.MakeClean();
    //                }
    //            }
    //        }

    //        if (_uboData.NumDirectionalLights != directionalLightCount)
    //        {
    //            _uboData.NumDirectionalLights = directionalLightCount;
    //            changed = true;
    //        }

    //        if (_uboData.NumPointLights != pointLightCount)
    //        {
    //            _uboData.NumPointLights = pointLightCount;
    //            changed = true;
    //        }

    //        return changed;
    //    }

    //    private void UpdateDirectionalLight(ref LightComponent light, ref TransformComponent transform, Vector3 direction, int index)
    //    {
    //        direction = Vector3.Normalize(direction);
    //        DirectionalLightData directionalLight;

    //        switch (index)
    //        {
    //            case 0: directionalLight = _uboData.DirectionalLight0; break;
    //            case 1: directionalLight = _uboData.DirectionalLight1; break;
    //            case 2: directionalLight = _uboData.DirectionalLight2; break;
    //            case 3: directionalLight = _uboData.DirectionalLight3; break;
    //            default: return;
    //        }

    //        directionalLight.Direction = direction;
    //        directionalLight.Color = light.Color;
    //        directionalLight.Intensity = light.Intensity;
    //        directionalLight.CastShadows = light.CastShadows ? 1.0f : 0.0f;
    //        directionalLight.LightSpaceMatrix = Matrix4x4.Identity;
    //        directionalLight.Enabled = light.Enabled;
    //        directionalLight.LightId = light.LightId;


    //        if (light.CastShadows)
    //        {
    //            CameraComponent activeCamera = default;
    //            TransformComponent cameraTransform = default;
    //            bool foundActiveCamera = false;

    //            Entity[] cameraEntities = queryCameraEntities.Build();
    //            foreach (var entity in cameraEntities)
    //            {
    //                ref var camera = ref this.GetComponent<CameraComponent>(entity);
    //                if (camera.IsActive)
    //                {
    //                    activeCamera = camera;
    //                    cameraTransform = this.GetComponent<TransformComponent>(entity);
    //                    foundActiveCamera = true;
    //                    break;
    //                }
    //            }

    //            if (foundActiveCamera)
    //            {
    //                Vector3[] frustumCorners = CalculateFrustumCorners(activeCamera, cameraTransform);

    //                //string k = string.Empty;
    //                //k += $"CameraPosition: {cameraTransform.Position}";
    //                //k += "\n";
    //                //k += $"CameraRotation: {cameraTransform.Rotation}";
    //                //k += "\n";
    //                //k += $"NearPlane: {activeCamera.NearPlane}";
    //                //k += "\n";
    //                //k += $"FarPlane: {activeCamera.FarPlane}";
    //                //k += "\n";
    //                //k += $"CameraUp: {activeCamera.CameraUp}";
    //                //k += "\n";
    //                //k += $"AspectRatio: {activeCamera.AspectRatio}";
    //                //k += "\n";
    //                //k += $"FieldOfView: {activeCamera.FieldOfView}";
    //                //k += "\n";

    //                //for (int i = 0; i < frustumCorners.Length; i++)
    //                //{
    //                //    k = k + $"frustumCorners[{i}]: " + frustumCorners[i].ToString() + "\n";
    //                //}

    //                float maxShadowDistance = 100.0f;
    //                float shadowRatio = maxShadowDistance / activeCamera.FarPlane;

    //                Vector3[] shadowFrustumCorners = new Vector3[8];
    //                for (int i = 0; i < 4; i++)
    //                {
    //                    shadowFrustumCorners[i] = frustumCorners[i];
    //                    shadowFrustumCorners[i + 4] = Vector3.Lerp(
    //                        frustumCorners[i],
    //                        frustumCorners[i + 4],
    //                        shadowRatio
    //                    );
    //                }

    //                Vector3 frustumCenter = Vector3.Zero;
    //                foreach (var corner in shadowFrustumCorners)
    //                {
    //                    frustumCenter += corner;
    //                }
    //                frustumCenter /= 8.0f;
    //                //k = k + $"frustumCenter: {frustumCenter}" + frustumCenter.ToString() + "\n";

    //                Vector3 up = Vector3.UnitY;
    //                if (Math.Abs(Vector3.Dot(direction, up)) > 0.99f)
    //                    up = Vector3.UnitZ;

    //                Matrix4x4 lightView = Matrix4x4.CreateLookAt(
    //                    frustumCenter - direction * maxShadowDistance * 0.5f,
    //                    frustumCenter,
    //                    up
    //                );
    //                //k += $"lightView: {lightView}";
    //                //k += "\n";

    //                Vector3 min = Vector3.Transform(shadowFrustumCorners[0], lightView);
    //                Vector3 max = min;

    //                for (int i = 1; i < 8; i++)
    //                {
    //                    Vector3 transformedCorner = Vector3.Transform(shadowFrustumCorners[i], lightView);
    //                    min = Vector3.Min(min, transformedCorner);
    //                    max = Vector3.Max(max, transformedCorner);
    //                }

    //                float padding = 5.0f;
    //                min.X -= padding;
    //                min.Y -= padding;
    //                max.X += padding;
    //                max.Y += padding;
    //                float zNear = 1.0f;
    //                float zFar = maxShadowDistance + padding * 2;

    //                //k += $"MinZ: {min.Z} \n";
    //                //k += $"MaxZ: {max.Z} \n";
    //                //k += $"zNear: {zNear} \n";
    //                //k += $"zFar: {zFar} \n";

    //                Matrix4x4 lightProjection = Matrix4x4.CreateOrthographicOffCenter(
    //                    min.X, max.X,
    //                    min.Y, max.Y,
    //                    zNear, zFar
    //                );

    //                //k += $"lightProjection: {lightProjection}";

    //                directionalLight.LightSpaceMatrix = lightView * lightProjection;
    //            }
    //            //else
    //            //{
    //            //    Vector3 sceneCenter = Vector3.Zero;
    //            //    float sceneRadius = 20.0f;

    //            //    Vector3 up = Vector3.UnitY;
    //            //    if (Math.Abs(Vector3.Dot(direction, up)) > 0.99f)
    //            //        up = Vector3.UnitZ;

    //            //    Matrix4x4 lightView = Matrix4x4.CreateLookAt(
    //            //        sceneCenter - direction * sceneRadius,
    //            //        sceneCenter,
    //            //        up
    //            //    );

    //            //    Matrix4x4 lightProjection = Matrix4x4.CreateOrthographicOffCenter(
    //            //        -sceneRadius, sceneRadius,
    //            //        -sceneRadius, sceneRadius,
    //            //        0.1f, sceneRadius * 2.0f
    //            //    );

    //            //    directionalLight.LightSpaceMatrix = lightView * lightProjection;
    //            //}
    //        }

    //        light.LightSpaceMatrix = directionalLight.LightSpaceMatrix;

    //        switch (index)
    //        {
    //            case 0: _uboData.DirectionalLight0 = directionalLight; break;
    //            case 1: _uboData.DirectionalLight1 = directionalLight; break;
    //            case 2: _uboData.DirectionalLight2 = directionalLight; break;
    //            case 3: _uboData.DirectionalLight3 = directionalLight; break;
    //        }
    //    }

    //    private void UpdatePointLight(ref LightComponent light, ref TransformComponent transform, int index)
    //    {
    //        Vector3 position = transform.Position;
    //        PointLightData pointLight;

    //        switch (index)
    //        {
    //            case 0: pointLight = _uboData.PointLight0; break;
    //            case 1: pointLight = _uboData.PointLight1; break;
    //            case 2: pointLight = _uboData.PointLight2; break;
    //            case 3: pointLight = _uboData.PointLight3; break;
    //            case 4: pointLight = _uboData.PointLight4; break;
    //            case 5: pointLight = _uboData.PointLight5; break;
    //            case 6: pointLight = _uboData.PointLight6; break;
    //            case 7: pointLight = _uboData.PointLight7; break;
    //            default: return;
    //        }

    //        pointLight.Position = position;
    //        pointLight.Color = light.Color;
    //        pointLight.Intensity = light.Intensity;
    //        pointLight.Radius = light.Radius;
    //        pointLight.CastShadows = light.CastShadows ? 1.0f : 0.0f;
    //        pointLight.FalloffExponent = light.FalloffExponent;
    //        pointLight.Enabled = light.Enabled;

    //        switch (index)
    //        {
    //            case 0: _uboData.PointLight0 = pointLight; break;
    //            case 1: _uboData.PointLight1 = pointLight; break;
    //            case 2: _uboData.PointLight2 = pointLight; break;
    //            case 3: _uboData.PointLight3 = pointLight; break;
    //            case 4: _uboData.PointLight4 = pointLight; break;
    //            case 5: _uboData.PointLight5 = pointLight; break;
    //            case 6: _uboData.PointLight6 = pointLight; break;
    //            case 7: _uboData.PointLight7 = pointLight; break;
    //        }
    //    }

    //    public Vector3[] CalculateFrustumCorners(CameraComponent camera, TransformComponent cameraTrans)
    //    {
    //        Matrix4x4 view = Matrix4x4.CreateLookAt(
    //            cameraTrans.Position,
    //            cameraTrans.Position + Vector3.Transform(Vector3.UnitZ, cameraTrans.GetRotationMatrix()),
    //            camera.CameraUp
    //        );

    //        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(
    //            camera.FieldOfView * (MathF.PI / 180.0f),
    //            camera.AspectRatio,
    //            camera.NearPlane,
    //            camera.FarPlane
    //        );

    //        Matrix4x4.Invert(view * projection, out Matrix4x4 invViewProj);

    //        Vector3[] corners = new Vector3[8];

    //        Vector4[] ndcCorners = new Vector4[8]
    //        {
    //            new Vector4(-1, 1, 0, 1), 
    //            new Vector4(1, 1, 0, 1),  
    //            new Vector4(-1, -1, 0, 1), 
    //            new Vector4(1, -1, 0, 1),  
    //            new Vector4(-1, 1, 1, 1),  
    //            new Vector4(1, 1, 1, 1),   
    //            new Vector4(-1, -1, 1, 1), 
    //            new Vector4(1, -1, 1, 1)   
    //        };

    //        for (int i = 0; i < 8; i++)
    //        {
    //            Vector4 worldPos = Vector4.Transform(ndcCorners[i], invViewProj);
    //            corners[i] = new Vector3(
    //                worldPos.X / worldPos.W,
    //                worldPos.Y / worldPos.W,
    //                worldPos.Z / worldPos.W
    //            );
    //        }

    //        return corners;
    //    }

    //    public void Resize(Vector2 size)
    //    {

    //    }
    //}
}
