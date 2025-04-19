using AtomEngine;
using EngineLib;
using Silk.NET.OpenGL;
using System.Numerics;

namespace OpenglLib
{
    public class PBRMaterialUboRenderSystem : IRenderSystem
    {
        public IWorld World { get; set; }

        private QueryEntity queryMaterialEntities;
        private UboService _uboService;
        private MaterialUboData _materialUboData;
        private bool _isDirty = true;

        public PBRMaterialUboRenderSystem(IWorld world)
        {
            World = world;

            queryMaterialEntities = this.CreateEntityQuery()
                .With<PBRSettingsMaterialComponent>()
                ;

            _materialUboData = new MaterialUboData();
            InitializeDefaultMaterial();
        }

        private void InitializeDefaultMaterial()
        {
            _materialUboData.Material.Albedo = new Vector3(1.0f, 1.0f, 1.0f);
            _materialUboData.Material.Metallic = 0.0f;
            _materialUboData.Material.Roughness = 0.5f;
            _materialUboData.Material.Ao = 1.0f;
            _materialUboData.Material.Alpha = 1.0f;
            _materialUboData.UseAlbedoMap = true;
            _materialUboData.UseNormalMap = false;
            _materialUboData.UseMetallicMap = false;
            _materialUboData.UseRoughnessMap = false;
            _materialUboData.UseAoMap = false;
            _materialUboData.CalculateViewDirPerPixel = false;
        }

        public void Initialize()
        {
            _uboService = ServiceHub.Get<UboService>();
            InitializeDefaultMaterial();
            _uboService.SetUboDataByBindingPoint(2, _materialUboData);
        }

        public void Render(double deltaTime, object? context)
        {
            if (context == null)
                return;

            GL gl = (GL)context;

            if (!_uboService.HasUboByBindingPoint(2))
                return;

            Entity[] entities = queryMaterialEntities.Build();
            if (entities.Length == 0) return;

            Entity currentEntity = entities[0];
            ref var material = ref this.GetComponent<PBRSettingsMaterialComponent>(currentEntity);

            if (material.IsDirty || _isDirty)
            {
                _materialUboData.Material.Albedo = material.Albedo;
                _materialUboData.Material.Metallic = material.Metallic;
                _materialUboData.Material.Roughness = material.Roughness;
                _materialUboData.Material.Ao = material.AmbientOcclusion;
                _materialUboData.Material.Alpha = material.Alpha;

                _materialUboData.UseAlbedoMap = material.UseAlbedoMap;
                _materialUboData.UseNormalMap = material.UseNormalMap;
                _materialUboData.UseMetallicMap = material.UseMetallicMap;
                _materialUboData.UseRoughnessMap = material.UseRoughnessMap;
                _materialUboData.UseAoMap = material.UseAoMap;
                _materialUboData.CalculateViewDirPerPixel = material.CalculateViewDirPerPixel;

                _uboService.SetUboDataByBindingPoint(2, _materialUboData);

                material.MakeClean();
                _isDirty = false;
            }
        }


        public void Resize(Vector2 size)
        {}
    }
}
