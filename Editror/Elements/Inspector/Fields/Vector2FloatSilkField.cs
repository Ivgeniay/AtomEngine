using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia;
using Silk.NET.Maths;
using System;

namespace Editor
{
    public class Vector2FloatSilkField : Grid
    {
        public static readonly StyledProperty<string> LabelProperty =
            AvaloniaProperty.Register<Vector2FloatSilkField, string>(nameof(Label), string.Empty);

        public static readonly StyledProperty<Vector2D<float>> ValueProperty =
            AvaloniaProperty.Register<Vector2FloatSilkField, Vector2D<float>>(
                nameof(Value),
                new Vector2D<float>(0f, 0f),
                defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

        public static readonly StyledProperty<bool> IsReadOnlyProperty =
            AvaloniaProperty.Register<Vector2FloatSilkField, bool>(nameof(IsReadOnly), false);

        public static readonly StyledProperty<float?> MinValueProperty =
            AvaloniaProperty.Register<Vector2FloatSilkField, float?>(nameof(MinValue), null);

        public static readonly StyledProperty<float?> MaxValueProperty =
            AvaloniaProperty.Register<Vector2FloatSilkField, float?>(nameof(MaxValue), null);

        public string Label
        {
            get => GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public Vector2D<float> Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public bool IsReadOnly
        {
            get => GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public float? MinValue
        {
            get => GetValue(MinValueProperty);
            set => SetValue(MinValueProperty, value);
        }

        public float? MaxValue
        {
            get => GetValue(MaxValueProperty);
            set => SetValue(MaxValueProperty, value);
        }

        public event EventHandler<Vector2D<float>> ValueChanged;

        private TextBlock _labelControl;
        private TextInputField _xInputField;
        private TextInputField _yInputField;
        private bool _isSettingValue = false;

        public Vector2FloatSilkField()
        {
            InitializeComponent();
            SetupEventHandlers();
        }

        private void InitializeComponent()
        {
            this.InitializeInspectorFieldLayout();

            _labelControl = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                Classes = { "propertyLabel" }
            };

            var vectorGrid = new Grid();
            vectorGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            vectorGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            vectorGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            vectorGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

            var xLabel = new TextBlock
            {
                Text = "X",
                Classes = { "propertyLabel" },
                Width = 15,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0)
            };

            _xInputField = new TextInputField
            {
                InputType = TextInputType.Float,
                MinValue = MinValue.HasValue ? (decimal?)MinValue.Value : null,
                MaxValue = MaxValue.HasValue ? (decimal?)MaxValue.Value : null
            };

            var yLabel = new TextBlock
            {
                Text = "Y",
                Classes = { "propertyLabel" },
                Width = 15,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 5, 0)
            };

            _yInputField = new TextInputField
            {
                InputType = TextInputType.Float,
                MinValue = MinValue.HasValue ? (decimal?)MinValue.Value : null,
                MaxValue = MaxValue.HasValue ? (decimal?)MaxValue.Value : null
            };

            Grid.SetColumn(xLabel, 0);
            Grid.SetColumn(_xInputField, 1);
            Grid.SetColumn(yLabel, 2);
            Grid.SetColumn(_yInputField, 3);

            vectorGrid.Children.Add(xLabel);
            vectorGrid.Children.Add(_xInputField);
            vectorGrid.Children.Add(yLabel);
            vectorGrid.Children.Add(_yInputField);

            Grid.SetColumn(_labelControl, 0);
            Grid.SetColumn(vectorGrid, 1);

            Children.Add(_labelControl);
            Children.Add(vectorGrid);
        }

        private void SetupEventHandlers()
        {
            this.PropertyChanged += (s, e) =>
            {
                if (e.Property == LabelProperty)
                {
                    _labelControl.Text = Label;
                }
                else if (e.Property == ValueProperty && !_isSettingValue)
                {
                    UpdateInputFields();
                }
                else if (e.Property == IsReadOnlyProperty)
                {
                    _xInputField.IsReadOnly = IsReadOnly;
                    _yInputField.IsReadOnly = IsReadOnly;
                }
                else if (e.Property == MinValueProperty)
                {
                    _xInputField.MinValue = MinValue.HasValue ? (decimal?)MinValue.Value : null;
                    _yInputField.MinValue = MinValue.HasValue ? (decimal?)MinValue.Value : null;
                }
                else if (e.Property == MaxValueProperty)
                {
                    _xInputField.MaxValue = MaxValue.HasValue ? (decimal?)MaxValue.Value : null;
                    _yInputField.MaxValue = MaxValue.HasValue ? (decimal?)MaxValue.Value : null;
                }
            };

            _xInputField.TextChanged += OnTextBoxTextChanged;
            _yInputField.TextChanged += OnTextBoxTextChanged;

            _labelControl.Text = Label;
            UpdateInputFields();
            _xInputField.IsReadOnly = IsReadOnly;
            _yInputField.IsReadOnly = IsReadOnly;
            _xInputField.MinValue = MinValue.HasValue ? (decimal?)MinValue.Value : null;
            _xInputField.MaxValue = MaxValue.HasValue ? (decimal?)MaxValue.Value : null;
            _yInputField.MinValue = MinValue.HasValue ? (decimal?)MinValue.Value : null;
            _yInputField.MaxValue = MaxValue.HasValue ? (decimal?)MaxValue.Value : null;
        }
        private void OnTextBoxTextChanged(object? sender, string text)
        {
            UpdateVectorValue();
        }

        private void UpdateInputFields()
        {
            _xInputField.TextChanged -= OnTextBoxTextChanged;
            _yInputField.TextChanged -= OnTextBoxTextChanged;

            try
            {
                _xInputField.SetValue(Value.X);
                _yInputField.SetValue(Value.Y);
            }
            finally
            {
                _xInputField.TextChanged += OnTextBoxTextChanged;
                _yInputField.TextChanged += OnTextBoxTextChanged;
            }
        }


        private void UpdateVectorValue()
        {
            float x = _xInputField.GetValue<float>();
            float y = _yInputField.GetValue<float>();

            Vector2D<float> newValue = new Vector2D<float>(x, y);

            if (newValue != Value)
            {
                _isSettingValue = true;
                try
                {
                    Value = newValue;
                    ValueChanged?.Invoke(this, newValue);
                }
                finally
                {
                    _isSettingValue = false;
                }
            }
        }
    }
}