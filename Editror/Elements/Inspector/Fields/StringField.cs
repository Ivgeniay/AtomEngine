using Avalonia.Controls;
using Avalonia;
using System;

namespace Editor
{
    /// <summary>
    /// Компонент для ввода строковых значений с меткой
    /// </summary>
    public class StringField : Grid
    {
        public static readonly StyledProperty<string> LabelProperty =
            AvaloniaProperty.Register<StringField, string>(nameof(Label), string.Empty);

        public static readonly StyledProperty<string> TextProperty =
            AvaloniaProperty.Register<StringField, string>(nameof(Text), string.Empty, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

        public static readonly StyledProperty<string> PlaceholderProperty =
            AvaloniaProperty.Register<StringField, string>(nameof(Placeholder), string.Empty);

        public static readonly StyledProperty<bool> IsReadOnlyProperty =
            AvaloniaProperty.Register<StringField, bool>(nameof(IsReadOnly), false);

        public static readonly StyledProperty<int?> MaxLengthProperty =
            AvaloniaProperty.Register<StringField, int?>(nameof(MaxLength), null);

        /// <summary>
        /// Текст метки поля
        /// </summary>
        public string Label
        {
            get => GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        /// <summary>
        /// Значение поля
        /// </summary>
        public string Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
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
        /// Максимальная длина текста
        /// </summary>
        public int? MaxLength
        {
            get => GetValue(MaxLengthProperty);
            set => SetValue(MaxLengthProperty, value);
        }

        /// <summary>
        /// Событие изменения текста
        /// </summary>
        public event EventHandler<string> TextChanged;

        private TextBlock _labelControl;
        private TextInputField _inputField;

        public StringField()
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
            Label = "String Field";

            _inputField = new TextInputField
            {
                InputType = TextInputType.String,
                MinValue = null,
                MaxValue = null
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
                else if (e.Property == TextProperty)
                {
                    if (_inputField.Text != Text)
                    {
                        _inputField.Text = Text;
                    }
                }
                else if (e.Property == PlaceholderProperty)
                {
                    _inputField.Placeholder = Placeholder;
                }
                else if (e.Property == IsReadOnlyProperty)
                {
                    _inputField.IsReadOnly = IsReadOnly;
                }
                else if (e.Property == MaxLengthProperty)
                {
                    _inputField.MaxLength = MaxLength;
                }
            };

            _inputField.TextChanged += (s, text) =>
            {
                if (Text != text)
                {
                    Text = text;
                    TextChanged?.Invoke(this, text);
                }
            };

            // Инициализация начальных значений
            _labelControl.Text = Label;
            _inputField.Text = Text;
            _inputField.Placeholder = Placeholder;
            _inputField.IsReadOnly = IsReadOnly;
            _inputField.MaxLength = MaxLength;
        }
    }
}