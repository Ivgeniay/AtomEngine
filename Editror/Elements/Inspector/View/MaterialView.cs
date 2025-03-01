using AtomEngine.RenderEntity;
using Avalonia.Controls;
using Avalonia.Layout;
using System.IO;

namespace Editor
{
    internal class MaterialView : BasePropertyView
    {
        private readonly PropertyDescriptor _descriptor;
        private string _materialGuid;

        public MaterialView(PropertyDescriptor descriptor) : base(descriptor)
        {
            _descriptor = descriptor;

            // Если значение уже установлено, попытаемся получить GUID
            if (descriptor.Value is ShaderBase material)
            {
                // Здесь нужно как-то получить GUID материала
            }
        }

        public override Control GetView()
        {
            var grid = CreateBaseLayout();

            var selectPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5
            };

            var materialName = new TextBlock
            {
                Text = GetMaterialName(),
                VerticalAlignment = VerticalAlignment.Center
            };

            var selectButton = new Button
            {
                Content = "Select",
                Classes = { "propertyEditor" },
                Command = new Command(SelectMaterial)
            };

            selectPanel.Children.Add(materialName);
            selectPanel.Children.Add(selectButton);

            Grid.SetColumn(selectPanel, 1);
            grid.Children.Add(selectPanel);

            return grid;
        }

        private string GetMaterialName()
        {
            if (string.IsNullOrEmpty(_materialGuid))
                return "None";

            var materialPath = ServiceHub.Get<MetadataManager>().GetPathByGuid(_materialGuid);
            return string.IsNullOrEmpty(materialPath) ? "Unknown" : Path.GetFileNameWithoutExtension(materialPath);
        }

        private void SelectMaterial()
        {
            // Открыть диалог выбора материала
            //var selector = new MaterialSelector();
            //selector.MaterialSelected += (guid) =>
            //{
            //    _materialGuid = guid;

            //    // Здесь нужно как-то сохранить GUID в свойство компонента
            //    // Этот вопрос нужно решить отдельно
            //};
            //selector.Show();
        }
    }
}