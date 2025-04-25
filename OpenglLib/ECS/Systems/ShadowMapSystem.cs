using AtomEngine;
using EngineLib;
using MathNet.Numerics.Statistics.Mcmc;
using OpenglLib.Buffers;
using OpenglLib.ECS.Components;
using Silk.NET.OpenGL;
using System.Numerics;

namespace OpenglLib
{
    public class ShadowMapSystem : IRenderSystem, IDisposable
    {
        public IWorld World { get; set; }

        private QueryEntity queryLightEntities;
        private QueryEntity queryRendererEntities;

        private FBOService _fboService;
        private GL _gl;
        private FramebufferObjectDepth _fbo;
        private uint _shadowMapArrayTexture;
        private int _shadowMapSize = 2048;
        private bool _initialized = false;
        private const int SHADOW_TEXTURE_UNIT = 10;

        private Dictionary<int, int> _directionalLightLayerIndices = new Dictionary<int, int>();
        private Dictionary<int, int> _pointLightLayerIndices = new Dictionary<int, int>();

        public ShadowMapSystem(IWorld world)
        {
            World = world;

            queryLightEntities = this.CreateEntityQuery()
                .With<LightComponent>()
                .With<TransformComponent>()
                .With<ShadowMaterialComponent>()
                ;

            queryRendererEntities = this.CreateEntityQuery()
                .With<TransformComponent>()
                .With<PBRComponent>()
                ;
        }

        public void Initialize()
        {
            _fboService = ServiceHub.Get<FBOService>();
        }

        private void InitializeShadowMapArray(GL gl)
        {
            _gl = gl;
            int maxShadowMaps = LightParams.MAX_DIRECTIONAL_LIGHTS + LightParams.MAX_POINT_LIGHTS;

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

        private int RegisterDirectionalLight(int lightId)
        {
            if (_directionalLightLayerIndices.TryGetValue(lightId, out int index))
            {
                return index;
            }

            for (int i = 0; i < LightParams.MAX_DIRECTIONAL_LIGHTS; i++)
            {
                if (!_directionalLightLayerIndices.ContainsValue(i))
                {
                    _directionalLightLayerIndices[lightId] = i;
                    return i;
                }
            }
            return -1;
        }

        private int RegisterPointLight(int lightId)
        {
            if (_pointLightLayerIndices.TryGetValue(lightId, out int index))
            {
                return index;
            }

            for (int i = LightParams.MAX_DIRECTIONAL_LIGHTS; i < LightParams.MAX_DIRECTIONAL_LIGHTS + LightParams.MAX_POINT_LIGHTS; i++)
            {
                if (!_pointLightLayerIndices.ContainsValue(i))
                {
                    _pointLightLayerIndices[lightId] = i;
                    return i;
                }
            }
            return -1;
        }
        private int GetDirectionalLightLayerIndex(int lightId)
        {
            if (_directionalLightLayerIndices.TryGetValue(lightId, out int index))
            {
                return index;
            }
            return -1;
        }

        private int GetPointLightLayerIndex(int lightId)
        {
            if (_pointLightLayerIndices.TryGetValue(lightId, out int index))
            {
                return index;
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

            if (!_initialized)
            {
                InitializeShadowMapArray(gl);
            }

            HashSet<int> activeLightIds = new HashSet<int>();
            foreach (var entity in activeLightEntities)
            {
                ref var lightComponent = ref this.GetComponent<LightComponent>(entity);
                activeLightIds.Add(lightComponent.LightId);
            }

            List<int> inactiveLightIds = new List<int>();
            foreach (var kvp in _directionalLightLayerIndices)
            {
                if (!activeLightIds.Contains(kvp.Key))
                {
                    inactiveLightIds.Add(kvp.Key);
                }
            }
            foreach (var lightId in inactiveLightIds)
            {
                _directionalLightLayerIndices.Remove(lightId);
            }
            inactiveLightIds.Clear();
            foreach (var kvp in _pointLightLayerIndices)
            {
                if (!activeLightIds.Contains(kvp.Key))
                {
                    inactiveLightIds.Add(kvp.Key);
                }
            }
            foreach (var lightId in inactiveLightIds)
            {
                _pointLightLayerIndices.Remove(lightId);
            }


            Span<int> viewport = stackalloc int[4];
            gl.GetInteger(GLEnum.Viewport, viewport);
            int prevFBO = gl.GetInteger(GLEnum.FramebufferBinding);

            bool depthTestEnabled = gl.IsEnabled(EnableCap.DepthTest);
            bool cullFaceEnabled = gl.IsEnabled(EnableCap.CullFace);
            int originalCullFaceMode = gl.GetInteger(GLEnum.CullFaceMode);

            gl.Enable(EnableCap.CullFace);
            gl.CullFace(GLEnum.Front);

            gl.Enable(EnableCap.DepthTest);
            gl.CullFace(GLEnum.Front);
            //gl.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);
            //gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            foreach (var lightEntity in activeLightEntities)
            {
                ref var lightComponent = ref this.GetComponent<LightComponent>(lightEntity);
                ref var shadowMaterialComponent = ref this.GetComponent<ShadowMaterialComponent>(lightEntity);

                int layerIndex;
                if (lightComponent.Type == LightType.Directional)
                {
                    layerIndex = GetDirectionalLightLayerIndex(lightComponent.LightId);
                    if (layerIndex == -1)
                    {
                        layerIndex = RegisterDirectionalLight(lightComponent.LightId);
                        if (layerIndex == -1) continue;
                    }
                }
                else if (lightComponent.Type == LightType.Point)
                {
                    layerIndex = GetPointLightLayerIndex(lightComponent.LightId);
                    if (layerIndex == -1)
                    {
                        layerIndex = RegisterPointLight(lightComponent.LightId);
                        if (layerIndex == -1) continue;
                    }
                }
                else
                {
                    continue;
                }

                try
                {
                    _fbo.BindLayer(layerIndex);
                    gl.ClearDepth(1.0f);
                    gl.Clear(ClearBufferMask.DepthBufferBit);

                    Material shadowMaterial = shadowMaterialComponent.Material;

                    if (shadowMaterial == null || shadowMaterial.Shader == null)
                        continue;

                    shadowMaterial.Use();
                    shadowMaterial.SetUniform("lightSpaceMatrix", lightComponent.LightSpaceMatrix);

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
                    DebLogger.Error($"Error rendering shadow map for light {lightComponent.LightId}: {ex.Message}");
                }
            }

            gl.DrawBuffer(DrawBufferMode.None);
            gl.ReadBuffer(ReadBufferMode.None);

            var unit = TextureUnit.Texture0 + SHADOW_TEXTURE_UNIT;
            gl.ActiveTexture(unit);
            gl.BindTexture(TextureTarget.Texture2DArray, _fbo.DepthTextureArray);

            foreach (var entity in rendererEntities)
            {
                ref var pbrComponent = ref this.GetComponent<PBRComponent>(entity);

                if (pbrComponent.Material == null || pbrComponent.Material.Shader == null)
                    continue;

                Material material = pbrComponent.Material;
                material.Use();
                material.SetUniform("shadowMapsArray", SHADOW_TEXTURE_UNIT);
            }


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

        void CheckGLError(GL gl, string location)
        {
            GLEnum error = gl.GetError();
            if (error != GLEnum.NoError)
            {
                DebLogger.Error($"OpenGL error at {location}: {error}");
            }
        }

        public void Resize(Vector2 size)
        {
        }

        public void Dispose()
        {
            CleanupShadowMapArray();
        }
    }
}
