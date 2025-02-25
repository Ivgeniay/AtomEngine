using System.Collections.Generic;
using Avalonia.Controls;
using System.Linq;
using System.IO;
using System;

namespace Editor
{
    public class AssetMetadataInspectable : IInspectable
    {
        private readonly AssetMetadata _metadata;
        private readonly string _filePath;

        public AssetMetadataInspectable(AssetMetadata metaData)
        {
            _metadata = metaData;
        }

        public AssetMetadataInspectable(string filePath)
        {
            _filePath = filePath;
            _metadata = MetadataManager.Instance.GetMetadata(filePath);
        }

        public string Title => $"Asset Metadata: {Path.GetFileName(_filePath)}";

        public IEnumerable<Control> GetCustomControls()
        {
            return null;
        }

        public IEnumerable<PropertyDescriptor> GetProperties()
        {
            yield return new PropertyDescriptor
            {
                Name = "GUID",
                Type = typeof(String),
                Value = _metadata.Guid,
                IsReadOnly = true
            };

            yield return new PropertyDescriptor
            {
                Name = "Asset Type",
                Type = typeof(String),
                Value = _metadata.AssetType.ToString(),
                IsReadOnly = true
            };

            yield return new PropertyDescriptor
            {
                Name = "Version",
                Type = typeof(Int32),
                Value = _metadata.Version,
                IsReadOnly = true
            };

            yield return new PropertyDescriptor
            {
                Name = "Last Modified",
                Type = typeof(String),
                Value = _metadata.LastModified.ToString(),
                IsReadOnly = true
            };

            yield return new PropertyDescriptor
            {
                Name = "Tags",
                Type = typeof(String),
                Value = string.Join(", ", _metadata.Tags),
                OnValueChanged = (value) => {
                    var tags = ((string)value).Split(',')
                        .Select(t => t.Trim())
                        .Where(t => !string.IsNullOrEmpty(t))
                        .ToList();

                    _metadata.Tags.Clear();
                    _metadata.Tags.AddRange(tags);

                    if (_filePath == null) MetadataManager.Instance.SaveMetadata(_metadata);
                    else MetadataManager.Instance.SaveMetadata(_filePath, _metadata);
                }
            };
        }
    }
}
