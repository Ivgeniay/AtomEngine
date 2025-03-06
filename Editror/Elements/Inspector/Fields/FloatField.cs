using Avalonia.Controls;
using Avalonia;
using System;

namespace Editor
{
    public class FloatField : Grid
    {
        public static readonly StyledProperty<string> LabelProperty =
            AvaloniaProperty.Register<FloatField, string>(nameof(Label), string.Empty);

        public static readonly StyledProperty<float> ValueProperty =
            AvaloniaProperty.Register<FloatField, float>(nameof(Value), 0f, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

        public static readonly StyledProperty<string> PlaceholderProperty =
            AvaloniaProperty.Register<FloatField, string>(nameof(Placeholder), "0.0");

        public static readonly StyledProperty<bool> IsReadOnlyProperty =
            AvaloniaProperty.Register<FloatField, bool>(nameof(IsReadOnly), false);

        public static readonly StyledProperty<float?> MinValueProperty =
            AvaloniaProperty.Register<FloatField, float?>(nameof(MinValue), null);

        public static readonly StyledProperty<float?> MaxValueProperty =
            AvaloniaProperty.Register<FloatField, float?>(nameof(MaxValue), null);

        public static readonly StyledProperty<string> FormatStringProperty =
            AvaloniaProperty.Register<FloatField, string>(nameof(FormatString), "0.###");

        /// <summary>
        /// Текст метки поля
        /// </summary>
        public string Label
        {
            get => GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        /// <summary>
        /// Значение поля с плавающей точкой
        /// </summary>
        public float Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        /// <summary>
        /// Подсказка при пустом поле
        /// </summary>
        public string Placeholder
        {
            get => GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        /// <summary>
        /// Только для чтения
        /// </summary>
        public bool IsReadOnly
        {
            get => GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        /// <summary>
        /// Минимальное допустимое значение
        /// </summary>
        public float? MinValue
        {
            get => GetValue(MinValueProperty);
            set => SetValue(MinValueProperty, value);
        }

        /// <summary>
        /// Максимальное допустимое значение
        /// </summary>
        public float? MaxValue
        {
            get => GetValue(MaxValueProperty);
            set => SetValue(MaxValueProperty, value);
        }

        /// <summary>
        /// Строка форматирования для отображения
        /// </summary>
        public string FormatString
        {
            get => GetValue(FormatStringProperty);
            set => SetValue(FormatStringProperty, value);
        }

        /// <summary>
        /// Событие изменения значения
        /// </summary>
        public event EventHandler<float> ValueChanged;

        private TextBlock _labelControl;
        private TextInputField _inputField;
        private bool _isSettingValue = false;

        public FloatField()
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
            Label = "Float Field";

            _inputField = new TextInputField
            {
                InputType = TextInputType.Float,
                MinValue = MinValue.HasValue ? (decimal?)MinValue.Value : null,
                MaxValue = MaxValue.HasValue ? (decimal?)MaxValue.Value : null
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
                    _inputField.MinValue = MinValue.HasValue ? (decimal?)MinValue.Value : null;
                }
                else if (e.Property == MaxValueProperty)
                {
                    _inputField.MaxValue = MaxValue.HasValue ? (decimal?)MaxValue.Value : null;
                }
            };

            _inputField.TextChanged += (s, text) =>
            {
                if (string.IsNullOrEmpty(text))
                    return;

                if (float.TryParse(text.Replace(',', '.'),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out float newValue) && Math.Abs(Value - newValue) > float.Epsilon)
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
            _inputField.MinValue = MinValue.HasValue ? (decimal?)MinValue.Value : null;
            _inputField.MaxValue = MaxValue.HasValue ? (decimal?)MaxValue.Value : null;
        }
    }
}