using System;
using System.Collections.Generic;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Diagnostics;
using AtomEngine;
using System.Linq;

namespace Editor
{
    internal static class InspectorDistributor
    {
        private static ProjectScene _currentScene;

        public static void Initialize(ProjectScene projectScene) => _currentScene = projectScene;

        public static IInspectable GetInspectable(object source)
        {
            switch (source)
            {
                case EntityHierarchyItem hierarchyItem:
                    var entityData = _currentScene.CurrentWorldData.Entities.Where(e => e.Id == hierarchyItem.Id && e.Version == hierarchyItem.Version).FirstOrDefault();
                    var _entity = new Entity(entityData.Id, entityData.Version);
                    var collection = entityData.Components.Values.ToList();
                    if (collection == null) collection = new List<IComponent>();
                    var inscted = new EntityInspectable(_entity, collection);
                    return inscted;

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
                            default:
                                return new AssetMetadataInspectable(meta);
                        }
                    }


                default:
                    DebLogger.Warn($"Нераспознаный тип для испекции: {source.GetType()}");
                    return null;
            }
        }

    }
}
