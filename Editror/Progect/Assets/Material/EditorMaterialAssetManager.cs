using System.Threading.Tasks;
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
            });
        }

    }
}
