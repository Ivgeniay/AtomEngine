using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor
{
    internal class TextureView : BasePropertyView
    {
        //private readonly PropertyDescriptor _descriptor;
        //private string _textureGuid;
        //private Image _previewImage;
        //private TextureManager _textureManager;

        public TextureView(PropertyDescriptor descriptor) : base(descriptor)
        {
            //    _descriptor = descriptor;
            //    _textureManager = ServiceHub.Get<TextureManager>();

            //    // Если значение уже установлено, попытаемся получить GUID
            //    if (descriptor.Value is Texture texture)
            //    {
            //        // Здесь нужно как-то получить GUID текстуры
            //        // Этот вопрос нужно решить отдельно
            //    }
        }

        public override Control GetView()
        {
            var grid = CreateBaseLayout();
            return grid;
            //    var previewPanel = new StackPanel
            //    {
            //        Orientation = Orientation.Horizontal,
            //        Spacing = 5
            //    };

            //    _previewImage = new Image
            //    {
            //        Width = 64,
            //        Height = 64,
            //        Stretch = Stretch.Uniform
            //    };

            //    if (!string.IsNullOrEmpty(_textureGuid))
            //    {
            //        // Загрузить превью текстуры
            //        LoadTexturePreview();
            //    }

            //    var selectButton = new Button
            //    {
            //        Content = "Select",
            //        Classes = { "propertyEditor" },
            //        Command = new Command(SelectTexture)
            //    };

            //    previewPanel.Children.Add(_previewImage);
            //    previewPanel.Children.Add(selectButton);

            //    Grid.SetColumn(previewPanel, 1);
            //    grid.Children.Add(previewPanel);

            //    return grid;
        }

        //private void SelectTexture()
        //{
        //    // Открыть диалог выбора текстуры
        //    var selector = new TextureSelector();
        //    selector.TextureSelected += (guid) =>
        //    {
        //        _textureGuid = guid;
        //        LoadTexturePreview();

        //        // Здесь нужно как-то сохранить GUID в свойство компонента
        //        // Этот вопрос нужно решить отдельно
        //    };
        //    selector.Show();
        //}

        //private void LoadTexturePreview()
        //{
        //    // Загрузить превью текстуры по GUID
        //    var texturePath = ServiceHub.Get<MetadataManager>().GetPathByGuid(_textureGuid);
        //    if (!string.IsNullOrEmpty(texturePath))
        //    {
        //        try
        //        {
        //            var bitmap = new Bitmap(texturePath);
        //            _previewImage.Source = bitmap;
        //        }
        //        catch
        //        {
        //            _previewImage.Source = null;
        //        }
        //    }
        //}
    }
}
