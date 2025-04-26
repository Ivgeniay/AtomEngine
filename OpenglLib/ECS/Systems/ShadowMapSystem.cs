using OpenglLib.ECS.Components;
using OpenglLib.Buffers;
using Silk.NET.OpenGL;
using System.Numerics;
using AtomEngine;
using EngineLib;

namespace OpenglLib
{
    public class ShadowMapSystem : IRenderSystem, IDisposable
    {
        public IWorld World { get; set; }

        private QueryEntity queryLightEntities;
        private QueryEntity queryRendererEntities;
        private QueryEntity queryShadowMapEntities;

        private FBOService _fboService;
        private UboService _uboService;
        private GL _gl;
        private FramebufferObjectDepth _fbo;
        private int _shadowMapSize = 2048;
        private bool _initialized = false;

        private const string UBO_NAME = "LightsUBO";
        private const string DIRECTION_SUBDOMAIN = "direction";
        private const string COLOR_SUBDOMAIN = "color";
        private const string INTENSITY_SUBDOMAIN = "intensity";
        private const string CAST_SHADOWS_SUBDOMAIN = "castShadows";
        private const string ENABLED_SUBDOMAIN = "enabled";
        private const string LIGHT_ID_SUBDOMAIN = "lightId";
        private const string NUM_CASCADES_SUBDOMAIN = "numCascades";
        private const string LIGHT_SPACE_MATRIX_SUBDOMAIN = "lightSpaceMatrix";
        private const string SPLIT_DEPTH_SUBDOMAIN = "splitDepth";
        private const uint UBO_BINDING_POINT = 1;

        public ShadowMapSystem(IWorld world)
        {
            World = world;

            queryLightEntities = this.CreateEntityQuery()
                .With<LightComponent>()
                .With<TransformComponent>()
                .With<ShadowMaterialComponent>();

            queryRendererEntities = this.CreateEntityQuery()
                .With<TransformComponent>()
                .With<PBRComponent>();

            queryShadowMapEntities = this.CreateEntityQuery()
                .With<ShadowMapComponent>();
        }

        public void Initialize()
        {
            _fboService = ServiceHub.Get<FBOService>();
            _uboService = ServiceHub.Get<UboService>();
        }

        private void InitializeShadowMapArray(GL gl)
        {
            _gl = gl;

            int maxShadowMaps = (LightParams.MAX_DIRECTIONAL_LIGHTS * LightParams.MAX_CASCADES) +
                                LightParams.MAX_POINT_LIGHTS +
                                LightParams.MAX_SPOT_LIGHTS;

            try
            {
                _fbo = new FramebufferObjectDepth(gl, _shadowMapSize, _shadowMapSize, maxShadowMaps);
                _initialized = true;
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Failed to initialize shadow map array: {ex.Message}");
                _initialized = false;
            }
        }

        private void CleanupShadowMapArray()
        {
            if (_initialized && _fbo != null)
            {
                _fbo.Dispose();
                _fbo = null;
                _initialized = false;
            }
        }

        private int RegisterCascadeLayer(ref ShadowMapComponent component, int lightId, LightType lightType, int cascadeIndex)
        {
            int existingIndex = -1;

            if (lightType == LightType.Directional)
            {
                existingIndex = component.GetDirectionalLightCascadeIndex(lightId, cascadeIndex);
            }
            else if (lightType == LightType.Point)
            {
                existingIndex = component.GetPointLightLayerIndex(lightId);
            }
            else if (lightType == LightType.Spot)
            {
                existingIndex = component.GetSpotLightLayerIndex(lightId);
            }

            if (existingIndex >= 0)
            {
                return existingIndex;
            }

            for (int i = 0; i < ShadowMapComponent.MAX_SHADOW_MAPS; i++)
            {
                if (component.LightIds[i] == -1)
                {
                    component.LightIds[i] = lightId;
                    component.LightTypes[i] = lightType;
                    component.CascadeIndices[i] = cascadeIndex;
                    return i;
                }
            }

            return -1; 
        }

        private void ClearLayer(ref ShadowMapComponent component, int layerIndex)
        {
            if (layerIndex >= 0 && layerIndex < ShadowMapComponent.MAX_SHADOW_MAPS)
            {
                component.LightIds[layerIndex] = -1;
                component.LightTypes[layerIndex] = (LightType)(-1);
                component.CascadeIndices[layerIndex] = -1;
            }
        }

        private void ClearLightLayers(ref ShadowMapComponent component, int lightId)
        {
            for (int i = 0; i < ShadowMapComponent.MAX_SHADOW_MAPS; i++)
            {
                if (component.LightIds[i] == lightId)
                {
                    ClearLayer(ref component, i);
                }
            }
        }

        private int GetLightIndexInUBO(int lightId, LightType lightType)
        {
            string arrayName = lightType == LightType.Directional ?
                "directionalLights" : (lightType == LightType.Point ? "pointLights" : "spotLights");

            int maxLights = lightType == LightType.Directional ?
                LightParams.MAX_DIRECTIONAL_LIGHTS :
                (lightType == LightType.Point ? LightParams.MAX_POINT_LIGHTS : LightParams.MAX_SPOT_LIGHTS);

            for (int i = 0; i < maxLights; i++)
            {
                string lightIdPath = $"{UBO_NAME}.{arrayName}[{i}].{LIGHT_ID_SUBDOMAIN}";

                if (_uboService.TryGetValue(UBO_BINDING_POINT, lightIdPath, out object lightIdObj) &&
                    lightIdObj is int id && id == lightId)
                {
                    return i;
                }
            }

            return -1;
        }

        public void Render(double deltaTime, object? context)
        {
            if (context == null) return;
            GL gl = null;
            if (context is GL) gl = (GL)context;
            if (gl == null) return;

            Entity[] allLightEntities = queryLightEntities.Build();
            if (allLightEntities.Length == 0)
            {
                CleanupShadowMapArray();
                return;
            }

            Entity[] rendererEntities = queryRendererEntities.Build();
            if (rendererEntities.Length == 0)
            {
                return;
            }

            List<Entity> activeLightEntities = new List<Entity>();
            foreach (var entity in allLightEntities)
            {
                ref var lightComponent = ref this.GetComponent<LightComponent>(entity);
                ref var shadowMaterialComponent = ref this.GetComponent<ShadowMaterialComponent>(entity);

                if (lightComponent.CastShadows && lightComponent.Enabled > 0.5f &&
                    shadowMaterialComponent.Material != null)
                {
                    activeLightEntities.Add(entity);
                }
            }

            if (activeLightEntities.Count == 0)
            {
                CleanupShadowMapArray();
                return;
            }

            Entity[] mapEntities = queryShadowMapEntities.Build();
            if (mapEntities.Length == 0)
            {
                return;
            }

            ref var shadowMapComponent = ref this.GetComponent<ShadowMapComponent>(mapEntities[0]);

            if (!_initialized)
            {
                InitializeShadowMapArray(gl);
                shadowMapComponent.ShadowMapArrayTextureId = _fbo.DepthTextureArray;
            }

            HashSet<int> activeLightIds = new HashSet<int>();
            foreach (var entity in activeLightEntities)
            {
                ref var lightComponent = ref this.GetComponent<LightComponent>(entity);
                activeLightIds.Add(lightComponent.LightId);
            }

            for (int i = 0; i < ShadowMapComponent.MAX_SHADOW_MAPS; i++)
            {
                if (shadowMapComponent.LightIds[i] != -1 &&
                    !activeLightIds.Contains(shadowMapComponent.LightIds[i]))
                {
                    ClearLayer(ref shadowMapComponent, i);
                }
            }

            Span<int> viewport = stackalloc int[4];
            gl.GetInteger(GLEnum.Viewport, viewport);
            int prevFBO = gl.GetInteger(GLEnum.FramebufferBinding);

            bool depthTestEnabled = gl.IsEnabled(EnableCap.DepthTest);
            bool cullFaceEnabled = gl.IsEnabled(EnableCap.CullFace);
            int originalCullFaceMode = gl.GetInteger(GLEnum.CullFaceMode);

            gl.Enable(EnableCap.DepthTest);
            gl.Enable(EnableCap.CullFace);
            gl.CullFace(GLEnum.Front);
            gl.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);

            foreach (var lightEntity in activeLightEntities)
            {
                ref var lightComponent = ref this.GetComponent<LightComponent>(lightEntity);
                ref var shadowMaterialComponent = ref this.GetComponent<ShadowMaterialComponent>(lightEntity);

                if (lightComponent.Type == LightType.Directional)
                {
                    int lightIndex = GetLightIndexInUBO(lightComponent.LightId, LightType.Directional);
                    if (lightIndex == -1) continue;

                    string numCascadesPath = $"{UBO_NAME}.directionalLights[{lightIndex}].{NUM_CASCADES_SUBDOMAIN}";
                    int numCascades = 1;

                    if (_uboService.TryGetValue(UBO_BINDING_POINT, numCascadesPath, out object numCascadesObj) &&
                        numCascadesObj is int numCascadesValue)
                    {
                        numCascades = numCascadesValue;
                    }

                    for (int cascadeIdx = 0; cascadeIdx < numCascades; cascadeIdx++)
                    {
                        int layerIndex = RegisterCascadeLayer(
                            ref shadowMapComponent,
                            lightComponent.LightId,
                            LightType.Directional,
                            cascadeIdx);

                        if (layerIndex == -1) continue;

                        try
                        {
                            _fbo.BindLayer(layerIndex);
                            gl.Clear(ClearBufferMask.DepthBufferBit);

                            string matrixPath = $"{UBO_NAME}.directionalLights[{lightIndex}].cascades[{cascadeIdx}].{LIGHT_SPACE_MATRIX_SUBDOMAIN}";

                            if (!_uboService.TryGetValue(UBO_BINDING_POINT, matrixPath, out object matrixObj) ||
                                !(matrixObj is Matrix4x4 lightSpaceMatrix))
                            {
                                continue;
                            }

                            Material shadowMaterial = shadowMaterialComponent.Material;
                            shadowMaterial.Use();
                            shadowMaterial.SetUniform("lightSpaceMatrix", lightSpaceMatrix);

                            foreach (var entity in rendererEntities)
                            {
                                ref var transform = ref this.GetComponent<TransformComponent>(entity);
                                ref var pbrComponent = ref this.GetComponent<PBRComponent>(entity);

                                if (pbrComponent.Mesh == null)
                                    continue;

                                shadowMaterial.SetUniform("modelPosition", transform.Position.ToSilk());
                                shadowMaterial.SetUniform("modelRotation", transform.Rotation.ToSilk());
                                shadowMaterial.SetUniform("modelScale", transform.Scale.ToSilk());

                                pbrComponent.Mesh.Draw(shadowMaterial.Shader);
                            }
                        }
                        catch (Exception ex)
                        {
                            DebLogger.Error($"Error rendering shadow map for light {lightComponent.LightId}, cascade {cascadeIdx}: {ex.Message}");
                        }
                    }
                }
                else if (lightComponent.Type == LightType.Point)
                {
                    int layerIndex = shadowMapComponent.GetPointLightLayerIndex(lightComponent.LightId);
                    if (layerIndex == -1)
                    {
                        layerIndex = RegisterCascadeLayer(
                            ref shadowMapComponent,
                            lightComponent.LightId,
                            LightType.Point,
                            -1);

                        if (layerIndex == -1) continue;
                    }

                    // Рендеринг теней для точечного источника...
                }
                else if (lightComponent.Type == LightType.Spot)
                {
                }
            }

            gl.DrawBuffer(DrawBufferMode.None);
            gl.ReadBuffer(ReadBufferMode.None);

            _fbo.Unbind(viewport[2], viewport[3]);

            if (depthTestEnabled) gl.Enable(EnableCap.DepthTest);
            else gl.Disable(EnableCap.DepthTest);

            if (cullFaceEnabled)
            {
                gl.Enable(EnableCap.CullFace);
                gl.CullFace((GLEnum)originalCullFaceMode);
            }
            else
            {
                gl.Disable(EnableCap.CullFace);
            }

            gl.BindFramebuffer(FramebufferTarget.Framebuffer, (uint)prevFBO);
            gl.Viewport(viewport[0], viewport[1], (uint)viewport[2], (uint)viewport[3]);
        }

        public void Resize(Vector2 size)
        {
        }

        public void Dispose()
        {
            CleanupShadowMapArray();
        }
    }

    public static class CascadedShadowCalculator
    {
        public static float[] CalculateCascadeSplits(float nearPlane, float farPlane)
        {
            float[] splits = new float[LightParams.MAX_CASCADES];

            for (int i = 0; i < LightParams.MAX_CASCADES; i++)
            {
                float p = (i + 1) / (float)LightParams.MAX_CASCADES;
                float log = nearPlane * MathF.Pow(farPlane / nearPlane, p);
                float uniform = nearPlane + (farPlane - nearPlane) * p;

                splits[i] = MathF.Min(
                    LightParams.CASCADE_DISTRIBUTION_LAMBDA * log +
                    (1 - LightParams.CASCADE_DISTRIBUTION_LAMBDA) * uniform,
                    LightParams.MAX_SHADOW_DISTANCE);
            }

            for (int i = 0; i < LightParams.MAX_CASCADES; i++)
            {
                splits[i] = splits[i] / farPlane;
            }

            return splits;
        }

        public static Vector3[] GetCascadeFrustumCorners(Vector3[] fullFrustum, float startDepth, float endDepth)
        {
            Vector3[] cascadeCorners = new Vector3[8];

            for (int i = 0; i < 4; i++)
            {
                cascadeCorners[i] = Vector3.Lerp(fullFrustum[i], fullFrustum[i + 4], startDepth);
            }

            for (int i = 0; i < 4; i++)
            {
                cascadeCorners[i + 4] = Vector3.Lerp(fullFrustum[i], fullFrustum[i + 4], endDepth);
            }

            return cascadeCorners;
        }

        public static Matrix4x4 CalculateLightSpaceMatrix(
    Vector3[] frustumCorners,
    Vector3 lightDirection,
    Vector3 lightPosition) // Добавляем позицию света из трансформа
        {
            // 1. Используем позицию и направление света из компонента
            // lightPosition - позиция источника света из TransformComponent
            // lightDirection - направление света (нормализованное)

            // 2. Находим границы фрустума для определения зоны видимости
            Vector3 min = new Vector3(float.MaxValue);
            Vector3 max = new Vector3(float.MinValue);

            foreach (var corner in frustumCorners)
            {
                min = Vector3.Min(min, corner);
                max = Vector3.Max(max, corner);
            }

            // 3. Создаем матрицу вида, смотрящую из позиции света в направлении света
            Vector3 up = Vector3.UnitY;
            if (Math.Abs(Vector3.Dot(lightDirection, up)) > 0.99f)
                up = Vector3.UnitZ;

            // Целевая точка - точка на луче света
            Vector3 target = lightPosition + lightDirection * 100.0f;

            // Создаем матрицу вида для источника света
            Matrix4x4 lightView = Matrix4x4.CreateLookAt(
                lightPosition,  // Позиция источника света
                target,         // Целевая точка в направлении света
                up              // Вектор вверх
            );

            // 4. Трансформируем углы фрустума в пространство света
            Vector3 transformedMin = new Vector3(float.MaxValue);
            Vector3 transformedMax = new Vector3(float.MinValue);

            foreach (var corner in frustumCorners)
            {
                Vector3 transformed = Vector3.Transform(corner, lightView);
                transformedMin = Vector3.Min(transformedMin, transformed);
                transformedMax = Vector3.Max(transformedMax, transformed);
            }

            // 5. Добавляем отступы
            float padding = 15.0f;
            transformedMin.X -= padding;
            transformedMin.Y -= padding;
            transformedMax.X += padding;
            transformedMax.Y += padding;

            // 6. Создаем ортографическую проекцию
            Matrix4x4 lightProjection = Matrix4x4.CreateOrthographicOffCenter(
                transformedMin.X, transformedMax.X,
                transformedMin.Y, transformedMax.Y,
                0.1f, // Небольшой ближний план
                transformedMax.Z - transformedMin.Z + padding * 2
            );

            // 7. Комбинируем матрицы
            return lightView * lightProjection;
        }

    }

}
