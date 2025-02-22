using Avalonia.Controls;
using System.Numerics;

namespace Editor.Elements.Inspector.View
{
    internal class Vector3View : BasePropertyView
    {
        public Vector3View(PropertyDescriptor descriptor) : base(descriptor) { }

        public override Control GetView()
        {
            var grid = CreateBaseLayout();

            var vectorGrid = new Grid
            {
                ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto }, 
                new ColumnDefinition { Width = GridLength.Star }, 
                new ColumnDefinition { Width = GridLength.Auto }, 
                new ColumnDefinition { Width = GridLength.Star }, 
                new ColumnDefinition { Width = GridLength.Auto }, 
                new ColumnDefinition { Width = GridLength.Star }, 
            }
            };

            var vector = (Vector3)Descriptor.Value;

            var xLabel = new TextBlock
            {
                Text = "X",
                Classes = { "propertyLabel" },
                Width = 15
            };

            var xInput = new NumericUpDown
            {
                Value = (decimal)vector.X,
                Classes = { "vectorEditor" },
                Increment = 0.1M,
                FormatString = "0.##",
                IsEnabled = !Descriptor.IsReadOnly,
                ShowButtonSpinner = true
            };

            var yLabel = new TextBlock
            {
                Text = "Y",
                Classes = { "propertyLabel" },
                Width = 15
            };

            var yInput = new NumericUpDown
            {
                Value = (decimal)vector.Y,
                Classes = { "vectorEditor" },
                Increment = 0.1M,
                FormatString = "0.##",
                IsEnabled = !Descriptor.IsReadOnly,
                ShowButtonSpinner = true
            };

            var zLabel = new TextBlock
            {
                Text = "Z",
                Classes = { "propertyLabel" },
                Width = 15
            };

            var zInput = new NumericUpDown
            {
                Value = (decimal)vector.Z,
                Classes = { "vectorEditor" },
                Increment = 0.1M,
                FormatString = "0.##",
                IsEnabled = !Descriptor.IsReadOnly,
                ShowButtonSpinner = true
            };

            Grid.SetColumn(xLabel, 0);
            Grid.SetColumn(xInput, 1);
            Grid.SetColumn(yLabel, 2);
            Grid.SetColumn(yInput, 3);
            Grid.SetColumn(zLabel, 4);
            Grid.SetColumn(zInput, 5);

            vectorGrid.Children.Add(xLabel);
            vectorGrid.Children.Add(xInput);
            vectorGrid.Children.Add(yLabel);
            vectorGrid.Children.Add(yInput);
            vectorGrid.Children.Add(zLabel);
            vectorGrid.Children.Add(zInput);

            xInput.ValueChanged += (s, e) =>
            {
                if (xInput.Value == null) return;
                var newVector = new Vector3((float)xInput.Value, (float)yInput.Value, (float)zInput.Value);
                Descriptor.OnValueChanged?.Invoke(newVector);
            };

            yInput.ValueChanged += (s, e) =>
            {
                if (yInput.Value == null) return;
                var newVector = new Vector3((float)xInput.Value, (float)yInput.Value, (float)zInput.Value);
                Descriptor.OnValueChanged?.Invoke(newVector);
            };

            zInput.ValueChanged += (s, e) =>
            {
                if (zInput.Value == null) return;
                var newVector = new Vector3((float)xInput.Value, (float)yInput.Value, (float)zInput.Value);
                Descriptor.OnValueChanged?.Invoke(newVector);
            };

            Grid.SetColumn(vectorGrid, 1);
            grid.Children.Add(vectorGrid);

            return grid;
        }
    }
}
