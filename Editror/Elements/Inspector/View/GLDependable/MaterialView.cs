using AtomEngine;
using Avalonia.Controls;
using EngineLib;
using OpenglLib;
using Silk.NET.Assimp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Editor
{
    internal class MaterialView : GLDependableViewBase
    {
        public MaterialView(PropertyDescriptor descriptor) : base(descriptor) { }

        public override Control GetView()
        {
            //StackPanel content = new StackPanel();
            //ObjectField objectField = new ObjectField();
            //objectField.AllowedExtensions = new string[] { ".mat" };
            //objectField.Label = descriptor.Name;
            //EntityInspectorContext context = (EntityInspectorContext)descriptor.Context;
            //string? resourseGuid = GettingGUID();
            //if (resourseGuid != null)
            //{
            //    objectField.ObjectPath = metaDataManager.GetPathByGuid(resourseGuid);
            //}
            //else
            //{
            //    objectField.ObjectPath = string.Empty;
            //}
            //objectField.IsEnabled = !descriptor.IsReadOnly;
            //objectField.ObjectChanged += (sender, e) =>
            //{
            //    if (e != null)
            //    {
            //        var fileEvent = Newtonsoft.Json.JsonConvert.DeserializeObject<DragDropEventArgs>(e);
            //        if (fileEvent != null)
            //        {
            //            var metaData = metaDataManager.LoadMetadata(fileEvent.FileFullPath + ".meta");
            //            DebLogger.Debug(metaData.Guid);
            //            descriptor.OnValueChanged?.Invoke(new GLValueRedirection()
            //            {
            //                GUID = metaData.Guid,
            //            });
            //        }
            //    }
            //    else
            //    {
            //        descriptor.OnValueChanged?.Invoke(new GLValueRedirection()
            //        {
            //            GUID = string.Empty,
            //        });
            //    }
            //};

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
                cont.Add(item.Item2.ShaderRepresentationTypeName);
            }
            dropBoxField.AddItems(cont);

            if (resourseGuid != null)
            {
                var path = metaDataManager.GetPathByGuid(resourseGuid);
                var match = materialAssets.FirstOrDefault(x => x.Item1 == path);
                if (match.Item2 != null)
                {
                    dropBoxField.SelectedItem = match.Item2.ShaderRepresentationTypeName;
                }
            }

            dropBoxField.SelectionChanged += (s, e) =>
            {
                if (e.AddedItems.Count > 0)
                {
                    var guid = materialAssets.FirstOrDefault(kvp => kvp.Item2.ShaderRepresentationTypeName == e.AddedItems[0]);
                    if (guid.Item2 != null)
                    {
                        var metaData = metaDataManager.LoadMetadata(guid.Item1 + ".meta");
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
