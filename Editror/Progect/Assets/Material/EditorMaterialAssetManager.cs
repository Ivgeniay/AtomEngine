using System;
using System.Numerics;
using System.Threading.Tasks;
using AtomEngine;
using EngineLib;
using OpenglLib;

namespace Editor
{
    internal class EditorMaterialAssetManager : MaterialAssetManager
    {
        public override Task InitializeAsync()
        {
            return Task.Run(async () => {
                string assetsPath = ServiceHub.Get<EditorDirectoryExplorer>().GetPath<AssetsDirectory>();
                CacheAllMaterials(assetsPath);

                await base.InitializeAsync();

                eventHub.Subscribe<FileEventCreated>(FileCreateHandler);
                eventHub.Subscribe<FileEventDeleted>(FileDeletedHandler);
                eventHub.Subscribe<FileEventRenamed>(FileRenamedHandler);
                eventHub.Subscribe<FileEventChange>(FileChangeHandler);
            });
        }

        private void FileChangeHandler(FileEventChange change)
        {
            if (change == null) return;

            if (!change.FullPath.EndsWith(MATERIAL_EXT, StringComparison.OrdinalIgnoreCase))
                return;

            LoadMaterial(change.FullPath);
        }

        private void FileRenamedHandler(FileEventRenamed renamed)
        {
            if (renamed == null) return;

            if (!renamed.OldFullPath.EndsWith(MATERIAL_EXT, StringComparison.OrdinalIgnoreCase) ||
        !renamed.FullPath.EndsWith(MATERIAL_EXT, StringComparison.OrdinalIgnoreCase))
                return;

            if (_cacheMaterialAssets.TryGetValue(renamed.OldFullPath, out MaterialAsset material))
            {
                _cacheMaterialAssets.Remove(renamed.OldFullPath);
                _cacheMaterialAssets[renamed.FullPath] = material;
            }
            else
            {
                LoadMaterial(renamed.FullPath);
            }
        }

        private void FileDeletedHandler(FileEventDeleted deleted)
        {
            if (deleted == null) return;

            if (!deleted.FullPath.EndsWith(MATERIAL_EXT, StringComparison.OrdinalIgnoreCase))
                return;

            if (_cacheMaterialAssets.TryGetValue(deleted.FullPath, out _))
            {
                _cacheMaterialAssets.Remove(deleted.FullPath);
            }
        }

        private void FileCreateHandler(FileEventCreated created)
        {
            if (created == null) return;

            if (!created.FullPath.EndsWith(MATERIAL_EXT, StringComparison.OrdinalIgnoreCase))
                return;

            if (_cacheMaterialAssets.TryGetValue(created.FullPath, out _)) return;

            MaterialAsset material = GetMaterialAssetByPath(created.FullPath);
            if (material != null)
            {
                DebLogger.Info($"Новый материал загружен: {created.FullPath}");
            }
        }
    }
}
