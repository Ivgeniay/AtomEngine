using Avalonia.Controls;
using Silk.NET.Core.Native;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Editor
{
    internal class MeshView : GLDependableViewBase
    {
        public MeshView(PropertyDescriptor descriptor) : base(descriptor) { }


        public override Control GetView()
        {
            ObjectField objectField = new ObjectField();
            objectField.AllowedExtensions = new string[] { ".obj" };
            objectField.Label = descriptor.Name;

            string? meshGuid = GettingGUID();
            if (!string.IsNullOrWhiteSpace(meshGuid))
            {
                objectField.ObjectPath = ServiceHub.Get<MeshManager>().GetPath(meshGuid);
            }
            else
            {
                objectField.ObjectPath = string.Empty;
            }
            objectField.IsEnabled = !descriptor.IsReadOnly;

            SceneManager sceneManager = ServiceHub.Get<SceneManager>();
            objectField.ObjectChanged += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e))
                {
                    var fileEvent = Newtonsoft.Json.JsonConvert.DeserializeObject<DragDropEventArgs>(e);
                    if (fileEvent != null)
                    {
                        if (fileEvent.Context != null)
                        {
                            ExpandableFileItemChild cont = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandableFileItemChild>(fileEvent.Context.ToString());
                            if (cont != null)
                            {
                                var metaData = ServiceHub.Get<MetadataManager>().GetMetadata(fileEvent.FileFullPath);
                                if (metaData != null && metaData is ModelMetadata modelData)
                                {
                                    var meshData = modelData.MeshesData.First(m => m.MeshName == cont.Name);
                                    if (meshData.Index >= 0)
                                    {
                                        descriptor.OnValueChanged?.Invoke(new GLValueRedirection()
                                        {
                                            GUID = metaData.Guid,
                                            Indexator = meshData.Index.ToString(),
                                        });
                                        return;
                                    }
                                }
                            }

                        }

                    }
                }
                else
                {
                    descriptor.OnValueChanged?.Invoke(new GLValueRedirection()
                    {
                        GUID = string.Empty,
                        Indexator = string.Empty,
                    });
                }

                objectField.ObjectPath = string.Empty;
                objectField.PlaceholderText = string.Empty;
            };
            return objectField;
        }
    }
}
