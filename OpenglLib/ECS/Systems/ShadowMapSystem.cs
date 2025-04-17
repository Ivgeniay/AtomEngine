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
        private uint _fboHandle;
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
                .With<MaterialComponent>()
                .With<MeshComponent>()
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

            _shadowMapArrayTexture = gl.GenTexture();
            gl.BindTexture(TextureTarget.Texture2DArray, _shadowMapArrayTexture);

            unsafe
            {
                gl.TexImage3D(
                    TextureTarget.Texture2DArray,
                    0,
                    (int)InternalFormat.DepthComponent32f,
                    (uint)_shadowMapSize,
                    (uint)_shadowMapSize,
                    (uint)maxShadowMaps,
                    0,
                    PixelFormat.DepthComponent,
                    PixelType.Float,
                    null
                );
            }

            gl.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            gl.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            gl.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
            gl.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);

            float[] borderColor = { 1.0f, 1.0f, 1.0f, 1.0f };
            unsafe
            {
                fixed (float* borderColorPtr = borderColor)
                {
                    gl.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureBorderColor, borderColorPtr);
                }
            }

            _fboHandle = gl.GenFramebuffer();
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fboHandle);

            gl.DrawBuffer(DrawBufferMode.None);
            gl.ReadBuffer(ReadBufferMode.None);

            if (gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete)
            {
                throw new Exception("Shadow map framebuffer is not complete!");
            }

            gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            _initialized = true;
        }

        private void CleanupShadowMapArray()
        {
            if (_initialized && _gl != null)
            {
                _gl.DeleteFramebuffer(_fboHandle);
                _gl.DeleteTexture(_shadowMapArrayTexture);
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
                shadowMapComponent.ShadowMapArrayTextureId = _shadowMapArrayTexture;
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

            gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fboHandle);
            gl.Viewport(0, 0, (uint)_shadowMapSize, (uint)_shadowMapSize);

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

                gl.FramebufferTextureLayer(
                    FramebufferTarget.Framebuffer,
                    FramebufferAttachment.DepthAttachment,
                    _shadowMapArrayTexture,
                    0,
                    layerIndex
                );

                if (gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete)
                {
                    DebLogger.Error($"Shadow map framebuffer is not complete for layer {layerIndex}!");
                    continue;
                }

                gl.Clear(ClearBufferMask.DepthBufferBit);

                Material shadowMaterial = shadowMaterialComponent.Material;
                shadowMaterial.Use();
                shadowMaterial.SetUniform("lightSpaceMatrix", lightComponent.LightSpaceMatrix);

                foreach (var entity in rendererEntities)
                {
                    ref var transform = ref this.GetComponent<TransformComponent>(entity);
                    ref var meshComponent = ref this.GetComponent<MeshComponent>(entity);

                    if (meshComponent.Mesh == null)
                        continue;

                    shadowMaterial.SetUniform("modelPosition", transform.Position.ToSilk());
                    shadowMaterial.SetUniform("modelRotation", transform.Rotation.ToSilk());
                    shadowMaterial.SetUniform("modelScale", transform.Scale.ToSilk());

                    meshComponent.Mesh.Draw(shadowMaterial.Shader);
                }
            }

            gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
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
}
