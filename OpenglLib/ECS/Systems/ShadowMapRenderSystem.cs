using AtomEngine;
using EngineLib;
using OpenglLib.Buffers;
using OpenglLib.ECS.Components;
using Silk.NET.OpenGL;
using System.Numerics;

namespace OpenglLib
{
    public class ShadowMapSystem : IRenderSystem
    {
        public IWorld World { get; set; }

        private QueryEntity queryLightEntities;
        private QueryEntity queryRendererEntities;
        private QueryEntity queryShadowMapEntities;
        private FBOService _fboService;

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

        public void Render(double deltaTime, object? context)
        {
            if (context == null) return;
            GL _gl = null;
            if (context is GL) _gl = (GL)context;
            if (_gl == null) return;

            Entity[] lightEntities = queryLightEntities.Build();
            if (lightEntities.Length == 0) return;

            Entity[] rendererEntities = queryRendererEntities.Build();
            if (rendererEntities.Length == 0) return;

            Entity[] shadowMapEntities = queryShadowMapEntities.Build();
            if (shadowMapEntities.Length == 0) return;

            ref var shadowMapComponent = ref this.GetComponent<ShadowMapComponent>(shadowMapEntities[0]);

            Span<int> viewport = stackalloc int[4];
            _gl.GetInteger(GLEnum.Viewport, viewport);

            foreach (var lightEntity in lightEntities)
            {
                ref var lightComponent = ref this.GetComponent<LightComponent>(lightEntity);

                if (!lightComponent.CastShadows || lightComponent.Enabled < 0.5f)
                    continue;

                ref var shadowMaterialComponent = ref this.GetComponent<ShadowMaterialComponent>(lightEntity);
                if (shadowMaterialComponent.Material == null)
                    continue;

                string fboName = $"ShadowMap_Light_{lightComponent.LightId}";
                int shadowResolution = 2048;
                var fbo = _fboService.GetOrCreateFBO(fboName, shadowResolution, shadowResolution);

                fbo.Bind();
                _gl.Clear(ClearBufferMask.DepthBufferBit);

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

                if (lightComponent.Type == LightType.Directional)
                {
                    int index = shadowMapComponent.GetDirectionalLightIndex(lightComponent.LightId);
                    if (index == -1)
                    {
                        shadowMapComponent.AddDirectionalLightShadowMap(lightComponent.LightId, fbo.DepthTexture);
                    }
                    else
                    {
                        shadowMapComponent.ShadowMapTextureIds[index] = fbo.DepthTexture;
                    }
                }
                else if (lightComponent.Type == LightType.Point)
                {
                    int index = shadowMapComponent.GetPointLightIndex(lightComponent.LightId);
                    if (index == -1)
                    {
                        shadowMapComponent.AddPointLightShadowMap(lightComponent.LightId, fbo.DepthTexture);
                    }
                    else
                    {
                        int actualIndex = index + LightParams.MAX_DIRECTIONAL_LIGHTS;
                        shadowMapComponent.ShadowMapTextureIds[actualIndex] = fbo.DepthTexture;
                    }
                }

                fbo.Unbind(viewport[2], viewport[3]);
            }
        }

        public void Resize(Vector2 size)
        {
            
        }
    }


    public class ShadowMapBindingSystem : IRenderSystem
    {
        public IWorld World { get; set; }

        private QueryEntity queryRendererEntities;
        private QueryEntity queryShadowMapEntities;

        private const int SHADOW_TEXTURE_UNIT_OFFSET = 8; // или другое подходящее значение

        public ShadowMapBindingSystem(IWorld world)
        {
            World = world;

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
        }

        public void Render(double deltaTime, object? context)
        {
            if (context == null) return;
            GL _gl = null;
            if (context is GL) _gl = (GL)context;
            if (_gl == null) return;

            Entity[] rendererEntities = queryRendererEntities.Build();
            if (rendererEntities.Length == 0) return;

            Entity[] shadowMapEntities = queryShadowMapEntities.Build();
            if (shadowMapEntities.Length == 0) return;

            ref var shadowMapComponent = ref this.GetComponent<ShadowMapComponent>(shadowMapEntities[0]);

            foreach (var entity in rendererEntities)
            {
                ref var materialComponent = ref this.GetComponent<MaterialComponent>(entity);
                if (materialComponent.Material?.Shader == null)
                    continue;

                Material material = materialComponent.Material;
                material.Use();

                for (int i = 0; i < LightParams.MAX_DIRECTIONAL_LIGHTS; i++)
                {
                    if (shadowMapComponent.LightIds[i] != -1 &&
                        shadowMapComponent.LightTypes[i] == LightType.Directional &&
                        shadowMapComponent.ShadowMapTextureIds[i] != 0)
                    {
                        int textureUnit = SHADOW_TEXTURE_UNIT_OFFSET + i;
                        //_gl.ActiveTexture(TextureUnit.Texture0 + (uint)textureUnit);
                        //_gl.BindTexture(TextureTarget.Texture2D, shadowMapComponent.ShadowMapTextureIds[i]);
                        material.SetUniform($"directionalShadowMaps[{i}]", textureUnit);
                    }
                }

                for (int i = 0; i < LightParams.MAX_POINT_LIGHTS; i++)
                {
                    int actualIndex = i + LightParams.MAX_DIRECTIONAL_LIGHTS;
                    if (shadowMapComponent.LightIds[actualIndex] != -1 &&
                        shadowMapComponent.LightTypes[actualIndex] == LightType.Point &&
                        shadowMapComponent.ShadowMapTextureIds[actualIndex] != 0)
                    {
                        int textureUnit = SHADOW_TEXTURE_UNIT_OFFSET + actualIndex;
                        //_gl.ActiveTexture(TextureUnit.Texture0 + (uint)textureUnit);
                        //_gl.BindTexture(TextureTarget.Texture2D, shadowMapComponent.ShadowMapTextureIds[actualIndex]);
                        material.SetUniform($"pointShadowMaps[{i}]", textureUnit);
                    }
                }
            }
        }

        public void Resize(Vector2 size)
        {
        }
    }



    //public class ShadowAtlasSystem : IRenderSystem
    //{
    //    // Размер общего атласа теней
    //    private const int SHADOW_ATLAS_SIZE = 4096;

    //    // Размер одной карты тени в атласе
    //    private const int SHADOW_MAP_SIZE = 1024;

    //    // Максимальное количество карт теней в атласе (4x4 grid = 16 maps)
    //    private const int MAX_SHADOW_MAPS_IN_ATLAS = 16;

    //    private FBOService _fboService;
    //    private uint _shadowAtlasFBO;
    //    private uint _shadowAtlasTexture;

    //    // Информация о размещении каждой карты теней в атласе
    //    private Dictionary<int, Rectangle> _shadowMapRects = new Dictionary<int, Rectangle>();

    //    // ... остальной код аналогичен ShadowMapSystem ...

    //    public void Initialize()
    //    {
    //        _fboService = ServiceHub.Get<FBOService>();

    //        // Создаем атлас теней
    //        _shadowAtlasFBO = _gl.GenFramebuffer();
    //        _shadowAtlasTexture = _gl.GenTexture();

    //        _gl.BindTexture(TextureTarget.Texture2D, _shadowAtlasTexture);
    //        _gl.TexImage2D(
    //            TextureTarget.Texture2D,
    //            0,
    //            (int)InternalFormat.DepthComponent,
    //            SHADOW_ATLAS_SIZE,
    //            SHADOW_ATLAS_SIZE,
    //            0,
    //            PixelFormat.DepthComponent,
    //            PixelType.Float,
    //            null
    //        );

    //        // Настройка параметров текстуры
    //        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
    //        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
    //        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
    //        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);

    //        float[] borderColor = { 1.0f, 1.0f, 1.0f, 1.0f };
    //        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, borderColor);

    //        // Настройка FBO
    //        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _shadowAtlasFBO);
    //        _gl.FramebufferTexture2D(
    //            FramebufferTarget.Framebuffer,
    //            FramebufferAttachment.DepthAttachment,
    //            TextureTarget.Texture2D,
    //            _shadowAtlasTexture,
    //        0
    //        );

    //        _gl.DrawBuffer(DrawBufferMode.None);
    //        _gl.ReadBuffer(ReadBufferMode.None);

    //        if (_gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete)
    //        {
    //            throw new Exception("Shadow atlas framebuffer is not complete!");
    //        }

    //        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

    //        // Инициализируем прямоугольники для карт теней в атласе
    //        InitializeShadowMapRects();
    //    }

    //    private void InitializeShadowMapRects()
    //    {
    //        // Размещаем карты теней в сетке 4x4
    //        int mapIndex = 0;
    //        for (int y = 0; y < 4; y++)
    //        {
    //            for (int x = 0; x < 4; x++)
    //            {
    //                _shadowMapRects[mapIndex] = new Rectangle(
    //                    x * SHADOW_MAP_SIZE,
    //                    y * SHADOW_MAP_SIZE,
    //                    SHADOW_MAP_SIZE,
    //                    SHADOW_MAP_SIZE
    //                );
    //                mapIndex++;
    //            }
    //        }
    //    }

    //    public void Render(double deltaTime, object? context)
    //    {
    //        // ... код получения сущностей ...

    //        // Приоритизация источников света
    //        List<(Entity entity, float importance)> prioritizedLights = PrioritizeLights(lightEntities);

    //        // Сохраняем текущий viewport
    //        Span<int> viewport = stackalloc int[4];
    //        _gl.GetInteger(GLEnum.Viewport, viewport);

    //        // Привязываем атлас теней
    //        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _shadowAtlasFBO);

    //        // Очищаем весь атлас
    //        _gl.Viewport(0, 0, SHADOW_ATLAS_SIZE, SHADOW_ATLAS_SIZE);
    //        _gl.Clear(ClearBufferMask.DepthBufferBit);

    //        // Рендерим тени для приоритетных источников света
    //        int processedLights = 0;
    //        foreach (var (lightEntity, importance) in prioritizedLights)
    //        {
    //            // Ограничиваем количество источников света с тенями
    //            if (processedLights >= MAX_SHADOW_MAPS_IN_ATLAS)
    //                break;

    //            ref var lightComponent = ref this.GetComponent<LightComponent>(lightEntity);
    //            if (!lightComponent.CastShadows || lightComponent.Enabled < 0.5f)
    //                continue;

    //            // Получаем прямоугольник в атласе для этого источника
    //            Rectangle rect = _shadowMapRects[processedLights];

    //            // Устанавливаем viewport для этой части атласа
    //            _gl.Viewport(rect.X, rect.Y, rect.Width, rect.Height);

    //            // ... код рендеринга тени для этого источника ...

    //            // Сохраняем информацию о положении в атласе
    //            lightComponent.ShadowMapAtlasRect = rect;
    //            lightComponent.ShadowMapAtlasTextureId = _shadowAtlasTexture;

    //            processedLights++;
    //        }

    //        // Отвязываем атлас
    //        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    //        _gl.Viewport(viewport[0], viewport[1], viewport[2], viewport[3]);
    //    }

    //    // Метод для определения важности источников света
    //    private List<(Entity entity, float importance)> PrioritizeLights(Entity[] lightEntities)
    //    {
    //        List<(Entity entity, float importance)> prioritizedLights = new List<(Entity entity, float importance)>();

    //        foreach (var entity in lightEntities)
    //        {
    //            ref var lightComponent = ref this.GetComponent<LightComponent>(entity);
    //            ref var transformComponent = ref this.GetComponent<TransformComponent>(entity);

    //            // Рассчитываем важность источника света
    //            // Это может быть расстояние до камеры, интенсивность света и т.д.
    //            float importance = CalculateLightImportance(lightComponent, transformComponent);

    //            prioritizedLights.Add((entity, importance));
    //        }

    //        // Сортируем по убыванию важности
    //        prioritizedLights.Sort((a, b) => b.importance.CompareTo(a.importance));

    //        return prioritizedLights;
    //    }

    //    private float CalculateLightImportance(LightComponent light, TransformComponent transform)
    //    {
    //        // Примерный расчет важности света
    //        // В реальном приложении это будет зависеть от многих факторов
    //        float importance = light.Intensity;

    //        // Для точечных источников учитываем расстояние до камеры
    //        if (light.Type == LightType.Point)
    //        {
    //            // Получаем активную камеру
    //            var camera = GetActiveCamera();
    //            if (camera != null)
    //            {
    //                float distance = Vector3.Distance(transform.Position, camera.Position);
    //                importance *= 1.0f / (1.0f + distance * 0.1f);
    //            }
    //        }

    //        return importance;
    //    }
    //}






    //public class ShadowMapPrepareSystem : IRenderSystem
    //{
    //    public IWorld World { get; set; }

    //    private QueryEntity queryLightEntities;
    //    private QueryEntity queryShadowMapEntities;
    //    private FBOService _fboService;

    //    private Entity _currentShadowCaster = Entity.Null;
    //    private FramebufferObject _currentFbo = null;

    //    public ShadowMapPrepareSystem(IWorld world)
    //    {
    //        World = world;

    //        queryLightEntities = this.CreateEntityQuery()
    //            .With<LightComponent>()
    //            .With<TransformComponent>()
    //            .With<ShadowMaterialComponent>()
    //            ;

    //        queryShadowMapEntities = this.CreateEntityQuery()
    //            .With<ShadowMapComponent>()
    //            ;
    //    }

    //    public void Initialize()
    //    {
    //        _fboService = ServiceHub.Get<FBOService>();
    //    }

    //    public void Render(double deltaTime, object? context)
    //    {
    //        if (context == null) return;
    //        GL _gl = null;
    //        if (context is GL) _gl = (GL)context;
    //        if (_gl == null) return;

    //        Entity[] lightEntities = queryLightEntities.Build();
    //        if (lightEntities.Length == 0) return;

    //        Entity[] shadowMapEntities = queryShadowMapEntities.Build();
    //        ShadowMapComponent? shadowMapComponent = this.GetComponent<ShadowMapComponent>(shadowMapEntities[0]);
    //        foreach (var lightEntity in lightEntities)
    //        {
    //            ref var lightComponent = ref this.GetComponent<LightComponent>(lightEntity);
    //            if (!lightComponent.CastShadows || lightComponent.Enabled < 0.5f)
    //                continue;

    //            ref var shadowMaterialComponent = ref this.GetComponent<ShadowMaterialComponent>(lightEntity);
    //            if (shadowMaterialComponent.Material == null)
    //                continue;

    //            string fboName = $"ShadowMap_Light_{lightComponent.LightId}";
    //            int shadowResolution = 2048;
    //            var fbo = _fboService.GetOrCreateFBO(fboName, shadowResolution, shadowResolution);

    //            fbo.Bind();
    //            _gl.Clear(ClearBufferMask.DepthBufferBit);

    //            Material shadowMaterial = shadowMaterialComponent.Material;
    //            shadowMaterial.Use();
    //            shadowMaterial.SetUniform("lightSpaceMatrix", lightComponent.LightSpaceMatrix);

    //            _currentShadowCaster = lightEntity;
    //            _currentFbo = fbo;
    //            break;
    //        }
    //    }

    //    public void Resize(Vector2 size)
    //    {
    //    }

    //    public FramebufferObject GetCurrentFbo()
    //    {
    //        return _currentFbo;
    //    }

    //    public Entity GetCurrentShadowCaster()
    //    {
    //        return _currentShadowCaster;
    //    }
    //}
    //public class ShadowMapCaptureSystem : IRenderSystem
    //{
    //    public IWorld World { get; set; }

    //    private QueryEntity queryShadowMapEntities;
    //    private ShadowMapPrepareSystem _prepareSystem;

    //    public ShadowMapCaptureSystem(IWorld world)
    //    {
    //        World = world;

    //        queryShadowMapEntities = this.CreateEntityQuery()
    //            .With<ShadowMapComponent>()
    //            ;
    //    }

    //    public void Initialize()
    //    {
    //        //_prepareSystem = World.GetSystem<ShadowMapPrepareSystem>();
    //    }

    //    public void Render(double deltaTime, object? context)
    //    {
    //        if (context == null) return;
    //        GL _gl = null;
    //        if (context is GL) _gl = (GL)context;
    //        if (_gl == null) return;

    //        var fbo = _prepareSystem.GetCurrentFbo();
    //        var shadowCaster = _prepareSystem.GetCurrentShadowCaster();

    //        if (fbo == null || shadowCaster == Entity.Null)
    //            return;

    //        Span<int> viewport = stackalloc int[4];
    //        _gl.GetInteger(GLEnum.Viewport, viewport);

    //        Entity[] shadowMapEntities = queryShadowMapEntities.Build();
    //        if (shadowMapEntities.Length == 0)
    //            return;

    //        ref var shadowMapComponent = ref this.GetComponent<ShadowMapComponent>(shadowMapEntities[0]);
    //        ref var lightComponent = ref this.GetComponent<LightComponent>(shadowCaster);

    //        if (lightComponent.Type == LightType.Directional)
    //        {
    //            int index = shadowMapComponent.GetDirectionalLightIndex(lightComponent.LightId);
    //            if (index == -1)
    //            {
    //                shadowMapComponent.AddDirectionalLightShadowMap(lightComponent.LightId, fbo.DepthTexture);
    //            }
    //            else
    //            {
    //            }
    //        }

    //        fbo.Unbind(viewport[2], viewport[3]);
    //    }

    //    public void Resize(Vector2 size)
    //    {
    //    }
    //}

}
