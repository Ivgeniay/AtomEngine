using AtomEngine;
using EngineLib;
using Silk.NET.OpenGL;
using System.Numerics;

namespace OpenglLib
{
    public class PBRMaterialUboRenderSystem : IRenderSystem
    {
        const uint UBO_BINDING_POINT = 2;

        const string ALBEDO_DOMAIN = "material.albedo";
        const string METALLIC_DOMAIN = "material.metallic";
        const string ROUGHNESS_DOMAIN = "material.roughness";
        const string AO_DOMAIN = "material.ao";
        const string ALPHA_DOMAIN = "material.alpha";

        const string USE_ALBEDO_MAP_DOMAIN = "useAlbedoMap";
        const string USE_NORMAL_MAP_DOMAIN = "useNormalMap";
        const string USE_METALLIC_MAP_DOMAIN = "useMetallicMap";
        const string USE_ROUGHNESS_MAP_DOMAIN = "useRoughnessMap";
        const string USE_AO_MAP_DOMAIN = "useAoMap";
        const string CALC_VIEW_DIR_DOMAIN = "calculateViewDirPerPixel";

        public IWorld World { get; set; }

        private QueryEntity queryMaterialEntities;
        private UboService _uboService;
        private bool _isDirty = true;
        private Dictionary<string, object> valuePairs = new Dictionary<string, object>();

        public PBRMaterialUboRenderSystem(IWorld world)
        {
            World = world;

            queryMaterialEntities = this.CreateEntityQuery()
                .With<PBRSettingsMaterialComponent>()
                ;
        }

        private void InitializeDefaultMaterial()
        {
            valuePairs[ALBEDO_DOMAIN]            = new Vector3(1.0f, 1.0f, 1.0f);
            valuePairs[METALLIC_DOMAIN]          = 0.0f;
            valuePairs[ROUGHNESS_DOMAIN]         = 0.5f;
            valuePairs[AO_DOMAIN]                = 1.0f;
            valuePairs[ALPHA_DOMAIN]             = 1.0f;

            valuePairs[USE_ALBEDO_MAP_DOMAIN]    = true;
            valuePairs[USE_NORMAL_MAP_DOMAIN]    = false;
            valuePairs[USE_METALLIC_MAP_DOMAIN]  = false;
            valuePairs[USE_ROUGHNESS_MAP_DOMAIN] = false;
            valuePairs[USE_AO_MAP_DOMAIN]        = false;
            valuePairs[CALC_VIEW_DIR_DOMAIN]     = false;

            if (!_uboService.HasUboByBindingPoint(UBO_BINDING_POINT))
                return;

            _uboService.SetUboDataByBindingPoint(UBO_BINDING_POINT, valuePairs);
            _uboService.Update(UBO_BINDING_POINT);
        }
        

        public void Initialize()
        {
            _uboService = ServiceHub.Get<UboService>();
            InitializeDefaultMaterial();
        }

        public void Render(double deltaTime, object? context)
        {
            if (!_uboService.HasUboByBindingPoint(UBO_BINDING_POINT))
                return;

            Entity[] entities = queryMaterialEntities.Build();
            if (entities.Length == 0) return;

            Entity currentEntity = entities[0];
            ref var pbrSettings = ref this.GetComponent<PBRSettingsMaterialComponent>(currentEntity);

            if (pbrSettings.IsDirty || _isDirty)
            {
                valuePairs[ALBEDO_DOMAIN]            = pbrSettings.Albedo;
                valuePairs[METALLIC_DOMAIN]          = pbrSettings.Metallic;
                valuePairs[ROUGHNESS_DOMAIN]         = pbrSettings.Roughness;
                valuePairs[AO_DOMAIN]                = pbrSettings.AmbientOcclusion;
                valuePairs[ALPHA_DOMAIN]             = pbrSettings.Alpha;

                valuePairs[USE_ALBEDO_MAP_DOMAIN]    = pbrSettings.UseAlbedoMap;
                valuePairs[USE_NORMAL_MAP_DOMAIN]    = pbrSettings.UseNormalMap;
                valuePairs[USE_METALLIC_MAP_DOMAIN]  = pbrSettings.UseMetallicMap;
                valuePairs[USE_ROUGHNESS_MAP_DOMAIN] = pbrSettings.UseRoughnessMap;
                valuePairs[USE_AO_MAP_DOMAIN]        = pbrSettings.UseAoMap;
                valuePairs[CALC_VIEW_DIR_DOMAIN]     = pbrSettings.CalculateViewDirPerPixel;

                _uboService.SetUboDataByBindingPoint(UBO_BINDING_POINT, valuePairs);
                pbrSettings.MakeClean();
                _isDirty = false;
            }
        }


        public void Resize(Vector2 size)
        {}
    }

}
