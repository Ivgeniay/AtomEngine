using Avalonia.Controls;
using EngineLib;

namespace Editor
{
    internal class TextureView : BasePropertyView
    {
        public TextureView(PropertyDescriptor descriptor) : base(descriptor)
        {
        }

        public override Control GetView()
        {
            string guid = (string)descriptor.Value;
            string path = string.Empty;

            if (!string.IsNullOrWhiteSpace(guid))
            {
                path = ServiceHub.Get<EditorMetadataManager>().GetPathByGuid(guid);
            }

            ImageField imageField = new ImageField();

            imageField.IsReadOnly = descriptor.IsReadOnly;
            imageField.Label = descriptor.Name;
            imageField.AllowedExtensions = new string[] { ".png", ".jpg", ".jpeg" };
            imageField.SetImage(path);

            imageField.ImageChanged += (sender, e) =>
            {
                guid = string.Empty;
                FileMetadata asset = ServiceHub.Get<EditorMetadataManager>().GetMetadata(e);
                if (asset != null)
                {
                    guid = asset.Guid;
                }
                if (descriptor.Context is EntityInspectorContext entityContex)
                {
                    GLValueRedirection redirection = new GLValueRedirection()
                    {
                        GUID = guid
                    };
                    descriptor.OnValueChanged(redirection);
                }
                else
                {
                    descriptor.OnValueChanged(guid);
                }
            };

            return imageField;
        }

    }
}
