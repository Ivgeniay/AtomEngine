using Avalonia.Controls;
using EngineLib;

namespace Editor
{
    internal class MaterialView : GLDependableViewBase
    {
        public MaterialView(PropertyDescriptor descriptor) : base(descriptor) { }

        public override Control GetView()
        {
            ObjectField objectField = new ObjectField();
            objectField.AllowedExtensions = new string[] { ".mat" };
            objectField.Label = descriptor.Name;

            EntityInspectorContext context = (EntityInspectorContext)descriptor.Context;

            string? resourseGuid = GettingGUID();
            if (resourseGuid != null)
            {
                objectField.ObjectPath = ServiceHub.Get<EditorMetadataManager>().GetPathByGuid(resourseGuid);
            }
            else
            {
                objectField.ObjectPath = string.Empty;
            }
            objectField.IsEnabled = !descriptor.IsReadOnly;

            SceneManager sceneManager = ServiceHub.Get<SceneManager>();
            objectField.ObjectChanged += (sender, e) =>
            {
                if (e != null)
                {
                    var fileEvent = Newtonsoft.Json.JsonConvert.DeserializeObject<DragDropEventArgs>(e);
                    if (fileEvent != null)
                    {
                        var metaData = ServiceHub.Get<EditorMetadataManager>().LoadMetadata(fileEvent.FileFullPath + ".meta");
                        descriptor.OnValueChanged?.Invoke(new GLValueRedirection()
                        {
                            GUID = metaData.Guid,
                        });
                    }
                }
                else
                {
                    descriptor.OnValueChanged?.Invoke(new GLValueRedirection()
                    {
                        GUID = string.Empty,
                    });
                }
            };
            return objectField;
        }

    }
}
