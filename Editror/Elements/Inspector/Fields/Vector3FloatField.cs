using Avalonia.Controls;
using Avalonia.Layout;
using System.Numerics;
using Avalonia;
using System;
using AtomEngine;

namespace Editor
{
    public class Vector3FloatField : Grid
    {
        public static readonly StyledProperty<string> LabelProperty =
            AvaloniaProperty.Register<Vector3FloatField, string>(nameof(Label), string.Empty);

        public static readonly StyledProperty<Vector3> ValueProperty =
            AvaloniaProperty.Register<Vector3FloatField, Vector3>(nameof(Value), new Vector3(0f, 0f, 0f), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

        public static readonly StyledProperty<bool> IsReadOnlyProperty =
            AvaloniaProperty.Register<Vector3FloatField, bool>(nameof(IsReadOnly), false);

        public static readonly StyledProperty<float?> MinValueProperty =
            AvaloniaProperty.Register<Vector3FloatField, float?>(nameof(MinValue), null);

        public static readonly StyledProperty<float?> MaxValueProperty =
            AvaloniaProperty.Register<Vector3FloatField, float?>(nameof(MaxValue), null);

        public string Label
        {
            get => GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public Vector3 Value
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

        public event EventHandler<Vector3> ValueChanged;

        private TextBlock _labelControl;
        private TextInputField _xInputField;
        private TextInputField _yInputField;
        private TextInputField _zInputField;
        private bool _isSettingValue = false;

        public Vector3FloatField()
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

            var zLabel = new TextBlock
            {
                Text = "Z",
                Classes = { "propertyLabel" },
                Width = 15,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 5, 0)
            };

            _zInputField = new TextInputField
            {
                InputType = TextInputType.Float,
                MinValue = MinValue.HasValue ? (decimal?)MinValue.Value : null,
                MaxValue = MaxValue.HasValue ? (decimal?)MaxValue.Value : null
            };

            Grid.SetColumn(xLabel, 0);
            Grid.SetColumn(_xInputField, 1);
            Grid.SetColumn(yLabel, 2);
            Grid.SetColumn(_yInputField, 3);
            Grid.SetColumn(zLabel, 4);
            Grid.SetColumn(_zInputField, 5);

            vectorGrid.Children.Add(xLabel);
            vectorGrid.Children.Add(_xInputField);
            vectorGrid.Children.Add(yLabel);
            vectorGrid.Children.Add(_yInputField);
            vectorGrid.Children.Add(zLabel);
            vectorGrid.Children.Add(_zInputField);

            Grid.SetColumn(_labelControl, 0);
            Grid.SetColumn(vectorGrid, 1);

            Children.Add(_labelControl);
            Children.Add(vectorGrid);
        }

        private void SetupEventHandlers()
        {
            _isSettingValue = false;

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
                    _zInputField.IsReadOnly = IsReadOnly;
                }
                else if (e.Property == MinValueProperty)
                {
                    decimal? minValue = MinValue.HasValue ? (decimal?)MinValue.Value : null;
                    _xInputField.MinValue = minValue;
                    _yInputField.MinValue = minValue;
                    _zInputField.MinValue = minValue;
                }
                else if (e.Property == MaxValueProperty)
                {
                    decimal? maxValue = MaxValue.HasValue ? (decimal?)MaxValue.Value : null;
                    _xInputField.MaxValue = maxValue;
                    _yInputField.MaxValue = maxValue;
                    _zInputField.MaxValue = maxValue;
                }
            };

            _xInputField.TextChanged += OnTextBoxTextChanged;
            _yInputField.TextChanged += OnTextBoxTextChanged;
            _zInputField.TextChanged += OnTextBoxTextChanged;

            _labelControl.Text = Label;
            UpdateInputFields();
            _xInputField.IsReadOnly = IsReadOnly;
            _yInputField.IsReadOnly = IsReadOnly;
            _zInputField.IsReadOnly = IsReadOnly;

            decimal? minValue = MinValue.HasValue ? (decimal?)MinValue.Value : null;
            _xInputField.MinValue = minValue;
            _yInputField.MinValue = minValue;
            _zInputField.MinValue = minValue;

            decimal? maxValue = MaxValue.HasValue ? (decimal?)MaxValue.Value : null;
            _xInputField.MaxValue = maxValue;
            _yInputField.MaxValue = maxValue;
            _zInputField.MaxValue = maxValue;
        }

        private void OnTextBoxTextChanged(object? sender, string text)
        {
            UpdateVectorValue();
        }

        private void UpdateInputFields()
        {
            _xInputField.TextChanged -= OnTextBoxTextChanged;
            _yInputField.TextChanged -= OnTextBoxTextChanged;
            _zInputField.TextChanged -= OnTextBoxTextChanged;

            try
            {
                _xInputField.SetValue(Value.X);
                _yInputField.SetValue(Value.Y);
                _zInputField.SetValue(Value.Z);
            }
            finally
            {
                _xInputField.TextChanged += OnTextBoxTextChanged;
                _yInputField.TextChanged += OnTextBoxTextChanged;
                _zInputField.TextChanged += OnTextBoxTextChanged;
            }
        }

        internal void UpdateVectorValue()
        {
            float x = _xInputField.GetValue<float>();
            float y = _yInputField.GetValue<float>();
            float z = _zInputField.GetValue<float>();

            Vector3 newValue = new Vector3(x, y, z);

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