using Avalonia.Controls;

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
                path = ServiceHub.Get<MetadataManager>().GetPathByGuid(guid);
            }

            ImageField imageField = new ImageField();
            imageField.Label = descriptor.Name;
            imageField.AllowedExtensions = new string[] { ".png", ".jpg", ".jpeg" };
            imageField.SetImage(path);

            imageField.ImageChanged += (sender, e) =>
            {
                guid = string.Empty;
                AssetMetadata asset = ServiceHub.Get<MetadataManager>().GetMetadata(e);
                if (asset != null)
                {
                    guid = asset.Guid;
                }
                descriptor.OnValueChanged(path);
            };

            return imageField;
        }

    }
}
