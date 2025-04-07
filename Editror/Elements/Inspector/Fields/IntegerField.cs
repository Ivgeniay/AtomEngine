using Avalonia.Controls;
using Avalonia;
using System;

namespace Editor
{

    public class IntegerField : Grid
    {
        public static readonly StyledProperty<string> LabelProperty =
            AvaloniaProperty.Register<IntegerField, string>(nameof(Label), string.Empty);

        public static readonly StyledProperty<int> ValueProperty =
            AvaloniaProperty.Register<IntegerField, int>(nameof(Value), 0, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

        public static readonly StyledProperty<string> PlaceholderProperty =
            AvaloniaProperty.Register<IntegerField, string>(nameof(Placeholder), "0");

        public static readonly StyledProperty<bool> IsReadOnlyProperty =
            AvaloniaProperty.Register<IntegerField, bool>(nameof(IsReadOnly), false);

        public static readonly StyledProperty<int?> MinValueProperty =
            AvaloniaProperty.Register<IntegerField, int?>(nameof(MinValue), null);

        public static readonly StyledProperty<int?> MaxValueProperty =
            AvaloniaProperty.Register<IntegerField, int?>(nameof(MaxValue), null);

        public string Label
        {
            get => GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public int Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public string Placeholder
        {
            get => GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        public bool IsReadOnly
        {
            get => GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public int? MinValue
        {
            get => GetValue(MinValueProperty);
            set => SetValue(MinValueProperty, value);
        }

        public int? MaxValue
        {
            get => GetValue(MaxValueProperty);
            set => SetValue(MaxValueProperty, value);
        }

        public event EventHandler<int> ValueChanged;

        private TextBlock _labelControl;
        private TextInputField _inputField;
        private bool _isSettingValue = false;

        public IntegerField()
        {
            InitializeComponent();
            SetupEventHandlers();
        }

        private void InitializeComponent()
        {
            this.InitializeInspectorFieldLayout();

            _labelControl = new TextBlock
            {
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Classes = { "propertyLabel" }
            };
            Label = "Integer Field";

            _inputField = new TextInputField
            {
                InputType = TextInputType.Integer,
                MinValue = MinValue,
                MaxValue = MaxValue
            };

            Grid.SetColumn(_labelControl, 0);
            Grid.SetColumn(_inputField, 1);

            Children.Add(_labelControl);
            Children.Add(_inputField);
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
                    _inputField.SetValue(Value);
                }
                else if (e.Property == PlaceholderProperty)
                {
                    _inputField.Placeholder = Placeholder;
                }
                else if (e.Property == IsReadOnlyProperty)
                {
                    _inputField.IsReadOnly = IsReadOnly;
                }
                else if (e.Property == MinValueProperty)
                {
                    _inputField.MinValue = MinValue;
                }
                else if (e.Property == MaxValueProperty)
                {
                    _inputField.MaxValue = MaxValue;
                }
            };

            _inputField.TextChanged += (s, text) =>
            {
                if (string.IsNullOrEmpty(text))
                    return;

                if (int.TryParse(text, out int newValue) && Value != newValue)
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
            };

            _labelControl.Text = Label;
            _inputField.SetValue(Value);
            _inputField.Placeholder = Placeholder;
            _inputField.IsReadOnly = IsReadOnly;
            _inputField.MinValue = MinValue;
            _inputField.MaxValue = MaxValue;
        }
    }

    public class UIntegerField : Grid
    {
        public static readonly StyledProperty<string> LabelProperty =
            AvaloniaProperty.Register<UIntegerField, string>(nameof(Label), string.Empty);

        public static readonly StyledProperty<uint> ValueProperty =
            AvaloniaProperty.Register<UIntegerField, uint>(nameof(Value), 0, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

        public static readonly StyledProperty<string> PlaceholderProperty =
            AvaloniaProperty.Register<UIntegerField, string>(nameof(Placeholder), "0");

        public static readonly StyledProperty<bool> IsReadOnlyProperty =
            AvaloniaProperty.Register<UIntegerField, bool>(nameof(IsReadOnly), false);

        public static readonly StyledProperty<uint?> MinValueProperty =
            AvaloniaProperty.Register<UIntegerField, uint?>(nameof(MinValue), null);

        public static readonly StyledProperty<uint?> MaxValueProperty =
            AvaloniaProperty.Register<UIntegerField, uint?>(nameof(MaxValue), null);

        public string Label
        {
            get => GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public uint Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public string Placeholder
        {
            get => GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        public bool IsReadOnly
        {
            get => GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public uint? MinValue
        {
            get => GetValue(MinValueProperty);
            set => SetValue(MinValueProperty, value);
        }

        public uint? MaxValue
        {
            get => GetValue(MaxValueProperty);
            set => SetValue(MaxValueProperty, value);
        }

        public event EventHandler<uint> ValueChanged;

        private TextBlock _labelControl;
        private TextInputField _inputField;
        private bool _isSettingValue = false;

        public UIntegerField()
        {
            InitializeComponent();
            SetupEventHandlers();
        }

        private void InitializeComponent()
        {
            Margin = new Thickness(4, 0);
            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

            _labelControl = new TextBlock
            {
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Classes = { "propertyLabel" }
            };
            Label = "UInteger Field";

            _inputField = new TextInputField
            {
                InputType = TextInputType.Integer,
                MinValue = MinValue,
                MaxValue = MaxValue
            };

            Grid.SetColumn(_labelControl, 0);
            Grid.SetColumn(_inputField, 1);

            Children.Add(_labelControl);
            Children.Add(_inputField);
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
                    _inputField.SetValue(Value);
                }
                else if (e.Property == PlaceholderProperty)
                {
                    _inputField.Placeholder = Placeholder;
                }
                else if (e.Property == IsReadOnlyProperty)
                {
                    _inputField.IsReadOnly = IsReadOnly;
                }
                else if (e.Property == MinValueProperty)
                {
                    _inputField.MinValue = MinValue;
                }
                else if (e.Property == MaxValueProperty)
                {
                    _inputField.MaxValue = MaxValue;
                }
            };

            _inputField.TextChanged += (s, text) =>
            {
                if (string.IsNullOrEmpty(text))
                    return;

                if (uint.TryParse(text, out uint newValue) && Value != newValue)
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
            };

            _labelControl.Text = Label;
            _inputField.SetValue(Value);
            _inputField.Placeholder = Placeholder;
            _inputField.IsReadOnly = IsReadOnly;
            _inputField.MinValue = MinValue;
            _inputField.MaxValue = MaxValue;
        }
    }
}