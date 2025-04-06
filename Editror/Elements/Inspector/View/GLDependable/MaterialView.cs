using AtomEngine;
using Avalonia.Controls;
using EngineLib;
using OpenglLib;
using Silk.NET.Assimp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Editor
{
    internal class MaterialView : GLDependableViewBase
    {
        public MaterialView(PropertyDescriptor descriptor) : base(descriptor) { }

        public override Control GetView()
        {
            SceneManager sceneManager = ServiceHub.Get<SceneManager>();
            EditorMetadataManager metaDataManager = ServiceHub.Get<EditorMetadataManager>();
            var materialAssetManager = ServiceHub.Get<EditorMaterialAssetManager>();
            string? resourseGuid = GettingGUID();

            DropBoxField dropBoxField = new DropBoxField();
            dropBoxField.IsEnabled = !descriptor.IsReadOnly;
            dropBoxField.Label = descriptor.Name;
            dropBoxField.IsMultiSelect = false;

            IEnumerable<(string, MaterialAsset)> materialAssets = materialAssetManager.GetMaterials();

            var cont = new List<object>();
            foreach (var item in materialAssets)
            {
                var name = Path.GetFileNameWithoutExtension(item.Item1);
                cont.Add(name);
            }
            dropBoxField.AddItems(cont);

            if (resourseGuid != null)
            {
                var path = metaDataManager.GetPathByGuid(resourseGuid);
                var match = materialAssets.FirstOrDefault(x => x.Item1 == path);
                if (match.Item2 != null)
                {
                    var name = Path.GetFileNameWithoutExtension(path);
                    dropBoxField.SelectedItem = name;
                }
            }

            dropBoxField.PointerPressed += (s, e) =>
            {
                dropBoxField.Items.Clear();
                materialAssets = materialAssetManager.GetMaterials();

                var cont = new List<object>();
                foreach (var item in materialAssets)
                {
                    var name = Path.GetFileNameWithoutExtension(item.Item1);
                    cont.Add(name);
                }
                dropBoxField.AddItems(cont);
            };

            dropBoxField.SelectionChanged += (s, e) =>
            {
                if (e.AddedItems.Count > 0)
                {
                    var match = materialAssets.FirstOrDefault(kvp =>
                    {
                        var name = Path.GetFileNameWithoutExtension(kvp.Item1);
                        if (name.Equals(e.AddedItems[0]))
                            return true;
                        return false;
                    });
                    if (match.Item2 != null)
                    {
                        var metaData = metaDataManager.LoadMetadata(match.Item1 + ".meta");
                        if (metaData !=null)
                        {
                            descriptor.OnValueChanged?.Invoke(new GLValueRedirection()
                            {
                                GUID = metaData.Guid,
                            });
                        }
                    }
                }
            };


            return dropBoxField;
        }

    }
}
