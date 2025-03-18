using System.Collections.Generic;
using System.Linq;
using AtomEngine;
using System;
using System.Threading.Tasks;

namespace Editor
{
    internal class InspectorDistributor : IService
    {
        private SceneManager _sceneManager;

        public IInspectable GetInspectable(object source)
        {
            switch (source)
            {
                case EntityHierarchyItem hierarchyItem:
                    var inspected = new EntityInspectable(hierarchyItem.Id);
                    return inspected;

                case FileSelectionEvent eventSelectionEvent:

                    if (eventSelectionEvent.FileExtension.EndsWith(".mat"))
                    {
                        var matAsset = ServiceHub.Get<MaterialManager>().LoadMaterial(eventSelectionEvent.FileFullPath);
                        return new MaterialInspectable(matAsset);
                    }
                    else
                    {
                        var meta = ServiceHub.Get<MetadataManager>().GetMetadata(eventSelectionEvent.FileFullPath);
                        Type t = meta.GetType();
                        switch(meta.AssetType)
                        {
                            case MetadataType.Texture:
                                return new TextureMetadataInspectable((TextureMetadata)meta);
                            case MetadataType.ShaderSource:
                                return new ShaderSourceInspectable((ShaderSourceMetadata)meta);
                            case MetadataType.Script:
                                return new ScriptInspectable((ScriptMetadata)meta);
                            case MetadataType.Model:
                                return new ModelInspectable((ModelMetadata)meta);
                            default:
                                return new AssetMetadataInspectable(meta);
                        }
                    }


                default:
                    DebLogger.Warn($"Нераспознаный тип для испекции: {source.GetType()}");
                    return null;
            }
        }

        public Task InitializeAsync()
        {
            _sceneManager = ServiceHub.Get<SceneManager>();
            return Task.CompletedTask;
        }
    }
}
