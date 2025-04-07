using AtomEngine;
using EngineLib;
using OpenglLib;
using System;

namespace Editor
{
    internal class MaterialShaderDependencyHandler : ResourceDependencyHandler
    {
        private MaterialAssetManager _materialManager;
        private MetadataManager _metadataManager;

        public MaterialShaderDependencyHandler()
        {
            _materialManager = ServiceHub.Get<MaterialAssetManager>();
            _metadataManager = ServiceHub.Get<MetadataManager>();
        }

        public override void HandleDependencyChanged(string assetPath, string changedDependencyPath, FileMetadata dependencyMeta)
        {
            try
            {
                var material = _materialManager.GetMaterialAssetByPath(assetPath);
                if (material == null)
                {
                    return;
                }

                if (material.ShaderRepresentationGuid == dependencyMeta.Guid)
                {
                    _materialManager.AssignShaderToMaterial(material, dependencyMeta.Guid);
                }
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при обработке изменения шейдера для материала ({assetPath}): {ex.Message}");
            }
        }

        public override void HandleDependencyDeleted(string assetPath, string deletedDependencyGuid, FileMetadata dependencyMeta)
        {
            try
            {
                var material = _materialManager.GetMaterialAssetByPath(assetPath);
                if (material == null)
                {
                    return;
                }

                if (material.ShaderRepresentationGuid == deletedDependencyGuid)
                {
                    SetDefault(material);

                    _materialManager.SaveMaterialAsset(material);
                }
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при обработке удаления зависимости шейдера для материала ({assetPath}): {ex.Message}");
            }
        }

        private void SetDefault(MaterialAsset material)
        {
            var (defaultGuid, defaultTypeName) = _materialManager.GetDefaulShaderValue();

            material.ShaderRepresentationGuid = defaultGuid;
            material.ShaderRepresentationTypeName = defaultTypeName;
            material.ClearContainers();
            //_materialManager.AssignShaderToMaterial(material, defaultGuid);
        }

        public override void HandleDependencyAdded(string assetPath, string addedDependencyPath, FileMetadata dependencyMeta)
        { }

        public override void HandleDependencyRemoved(string assetPath, string removedDependencyPath, FileMetadata dependencyMeta)
        {
            try
            {
                var material = _materialManager.GetMaterialAssetByPath(assetPath);
                if (material == null)
                {
                    return;
                }

                string removedGuid = dependencyMeta.Guid;
                if (material.ShaderRepresentationGuid == removedGuid)
                {
                    SetDefault(material);

                    _materialManager.SaveMaterialAsset(material);
                }
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при обработке удаления зависимости шейдера для материала ({assetPath}): {ex.Message}");
            }
        }
    }
}
