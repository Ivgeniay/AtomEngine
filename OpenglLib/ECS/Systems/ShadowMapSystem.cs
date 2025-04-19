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
        private QueryEntity queryShadowMapEntities;

        private FBOService _fboService;
        private GL _gl;
        private FramebufferObject _fbo;
        private uint _shadowMapArrayTexture;
        private int _shadowMapSize = 2048;
        private bool _initialized = false;

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

            queryShadowMapEntities = this.CreateEntityQuery()
                .With<ShadowMapComponent>()
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
                _fbo = new FramebufferObject(gl, _shadowMapSize, _shadowMapSize, maxShadowMaps);
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

        private int RegisterDirectionalLight(ref ShadowMapComponent component, int lightId)
        {
            for (int i = 0; i < LightParams.MAX_DIRECTIONAL_LIGHTS; i++)
            {
                if (component.LightIds[i] == -1)
                {
                    component.LightIds[i] = lightId;
                    component.LightTypes[i] = LightType.Directional;
                    return i;
                }
            }
            return -1;
        }

        private int RegisterPointLight(ref ShadowMapComponent component, int lightId)
        {
            for (int i = LightParams.MAX_DIRECTIONAL_LIGHTS; i < ShadowMapComponent.MAX_SHADOW_MAPS; i++)
            {
                if (component.LightIds[i] == -1)
                {
                    component.LightIds[i] = lightId;
                    component.LightTypes[i] = LightType.Point;
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
                shadowMapComponent.ShadowMapArrayTextureId = _fbo.DepthTextureArray; ;
            }

            HashSet<int> activeLightIds = new HashSet<int>();
            foreach (var entity in activeLightEntities)
            {
                ref var lightComponent = ref this.GetComponent<LightComponent>(entity);
                activeLightIds.Add(lightComponent.LightId);
            }

            for (int i = 0; i < ShadowMapComponent.MAX_SHADOW_MAPS; i++)
            {
                if (shadowMapComponent.LightIds[i] != -1 && !activeLightIds.Contains(shadowMapComponent.LightIds[i]))
                {
                    shadowMapComponent.LightIds[i] = -1;
                    shadowMapComponent.LightTypes[i] = (LightType)(-1);
                }
            }

            Span<int> viewport = stackalloc int[4];
            gl.GetInteger(GLEnum.Viewport, viewport);
            int prevFBO = gl.GetInteger(GLEnum.FramebufferBinding);

            bool depthTestEnabled = gl.IsEnabled(EnableCap.DepthTest);
            gl.Enable(EnableCap.DepthTest);

            foreach (var lightEntity in activeLightEntities)
            {
                ref var lightComponent = ref this.GetComponent<LightComponent>(lightEntity);
                ref var shadowMaterialComponent = ref this.GetComponent<ShadowMaterialComponent>(lightEntity);

                int layerIndex;
                if (lightComponent.Type == LightType.Directional)
                {
                    layerIndex = shadowMapComponent.GetDirectionalLightIndex(lightComponent.LightId);
                    if (layerIndex == -1)
                    {
                        layerIndex = RegisterDirectionalLight(ref shadowMapComponent, lightComponent.LightId);
                        if (layerIndex == -1) continue;
                    }
                }
                else if (lightComponent.Type == LightType.Point)
                {
                    layerIndex = shadowMapComponent.GetPointLightLayerIndex(lightComponent.LightId);
                    if (layerIndex == -1)
                    {
                        layerIndex = RegisterPointLight(ref shadowMapComponent, lightComponent.LightId);
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
                    gl.Clear(ClearBufferMask.DepthBufferBit);

                    Material shadowMaterial = shadowMaterialComponent.Material;
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

            int curFBO = gl.GetInteger(GLEnum.FramebufferBinding);

            _fbo.Unbind(viewport[2], viewport[3]);

            if (depthTestEnabled) gl.Enable(EnableCap.DepthTest);
            else gl.Disable(EnableCap.DepthTest);

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
