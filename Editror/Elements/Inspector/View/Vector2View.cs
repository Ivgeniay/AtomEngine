using Avalonia.Controls;
using System.Numerics;

namespace Editor.Elements.Inspector.View
{
    internal class Vector2View : BasePropertyView
    {
        public Vector2View(PropertyDescriptor descriptor) : base(descriptor) { }

        public override Control GetView()
        {
            var grid = CreateBaseLayout();

            var vectorGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto }, // Для X label
                    new ColumnDefinition { Width = GridLength.Star }, // Для X value
                    new ColumnDefinition { Width = GridLength.Auto }, // Для Y label
                    new ColumnDefinition { Width = GridLength.Star }, // Для Y value
                },
                //ColumnSpacing = 4
            };

            var vector = (Vector2)Descriptor.Value;

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
                IsEnabled = !Descriptor.IsReadOnly
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
                IsEnabled = !Descriptor.IsReadOnly
            };

            Grid.SetColumn(xLabel, 0);
            Grid.SetColumn(xInput, 1);
            Grid.SetColumn(yLabel, 2);
            Grid.SetColumn(yInput, 3);

            vectorGrid.Children.Add(xLabel);
            vectorGrid.Children.Add(xInput);
            vectorGrid.Children.Add(yLabel);
            vectorGrid.Children.Add(yInput);

            xInput.ValueChanged += (s, e) =>
            {
                if (xInput.Value == null) return;
                var newVector = new Vector2((float)xInput.Value, (float)yInput.Value);
                Descriptor.OnValueChanged?.Invoke(newVector);
            };

            yInput.ValueChanged += (s, e) =>
            {
                if (yInput.Value == null) return;
                var newVector = new Vector2((float)xInput.Value, (float)yInput.Value);
                Descriptor.OnValueChanged?.Invoke(newVector);
            };

            Grid.SetColumn(vectorGrid, 1);
            grid.Children.Add(vectorGrid);

            return grid;
        }
    }
}
