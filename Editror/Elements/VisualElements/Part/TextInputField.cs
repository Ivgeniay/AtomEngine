using System.Globalization;
using Avalonia.Controls;
using Avalonia;
using System;
using AtomEngine;

namespace Editor
{
    public class TextInputField : Grid
    {
        #region Свойства зависимостей

        public static readonly StyledProperty<string> TextProperty =
            AvaloniaProperty.Register<TextInputField, string>(nameof(Text), string.Empty);

        public static readonly StyledProperty<TextInputType> InputTypeProperty =
            AvaloniaProperty.Register<TextInputField, TextInputType>(nameof(InputType), TextInputType.String);

        public static readonly StyledProperty<string> PlaceholderProperty =
            AvaloniaProperty.Register<TextInputField, string>(nameof(Placeholder), string.Empty);

        public static readonly StyledProperty<bool> IsReadOnlyProperty =
            AvaloniaProperty.Register<TextInputField, bool>(nameof(IsReadOnly), false);

        public static readonly StyledProperty<int?> MaxLengthProperty =
            AvaloniaProperty.Register<TextInputField, int?>(nameof(MaxLength), null);

        public static readonly StyledProperty<decimal?> MaxValueProperty =
            AvaloniaProperty.Register<TextInputField, decimal?>(nameof(MaxValue), null);

        public static readonly StyledProperty<decimal?> MinValueProperty =
            AvaloniaProperty.Register<TextInputField, decimal?>(nameof(MinValue), null);

        #endregion

        #region Публичные свойства

        /// <summary>
        /// Текст в поле ввода
        /// </summary>
        public string Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        /// <summary>
        /// Тип вводимых данных
        /// </summary>
        public TextInputType InputType
        {
            get => GetValue(InputTypeProperty);
            set => SetValue(InputTypeProperty, value);
        }

        /// <summary>
        /// Текст-подсказка (placeholder)
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
        /// Максимальное значение для числовых полей
        /// </summary>
        public decimal? MaxValue
        {
            get => GetValue(MaxValueProperty);
            set => SetValue(MaxValueProperty, value);
        }

        /// <summary>
        /// Минимальное значение для числовых полей
        /// </summary>
        public decimal? MinValue
        {
            get => GetValue(MinValueProperty);
            set => SetValue(MinValueProperty, value);
        }

        #endregion

        #region События

        /// <summary>
        /// Событие изменения текста
        /// </summary>
        public event EventHandler<string> TextChanged;

        #endregion

        #region Приватные поля

        private Border _mainBorder;
        private TextBox _textBox;
        private NumericUpDown _numericValidator;
        private bool _isSettingText = false;

        #endregion

        /// <summary>
        /// Конструктор
        /// </summary>
        public TextInputField()
        {
            InitializeComponent();
            SetupEventHandlers();
        }

        /// <summary>
        /// Инициализация компонентов
        /// </summary>
        private void InitializeComponent()
        {
            ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

            _mainBorder = new Border
            {
                Classes = { "textInputBorder" },
                Padding = new Thickness(4, 0)
            };

            _textBox = new TextBox
            {
                Classes = { "textInpuntTextbox" },
                Watermark = Placeholder
            };

            _numericValidator = new NumericUpDown
            {
                IsVisible = false,
                Width = 0,
                Height = 0
            };

            _mainBorder.Child = _textBox;
            Children.Add(_mainBorder);
            Children.Add(_numericValidator);
        }

        /// <summary>
        /// Устанавливает обработчики событий
        /// </summary>
        private void SetupEventHandlers()
        {
            this.PropertyChanged += (s, e) =>
            {
                if (e.Property == TextProperty)
                {
                    UpdateTextBoxFromText();
                }
                else if (e.Property == InputTypeProperty)
                {
                    UpdateValidatorForInputType();
                }
                else if (e.Property == PlaceholderProperty)
                {
                    _textBox.Watermark = Placeholder;
                }
                else if (e.Property == IsReadOnlyProperty)
                {
                    _textBox.IsReadOnly = IsReadOnly;
                }
                else if (e.Property == MaxLengthProperty)
                {
                    _textBox.MaxLength = MaxLength ?? int.MaxValue;
                }
                else if (e.Property == MinValueProperty || e.Property == MaxValueProperty)
                {
                    UpdateValidatorLimits();
                }
            };

            _textBox.TextChanged += OnTextBoxTextChanged;
            _numericValidator.ValueChanged += OnNumericValidatorValueChanged;
            UpdateValidatorForInputType();
        }

        /// <summary>
        /// Обновляет валидатор в соответствии с текущим типом ввода
        /// </summary>
        private void UpdateValidatorForInputType()
        {
            switch (InputType)
            {
                case TextInputType.Integer:
                    _textBox.Classes.Remove("passwordInput");
                    _textBox.PasswordChar = '\0';

                    _numericValidator.FormatString = "0";
                    _numericValidator.Increment = 1;
                    _numericValidator.Minimum = MinValue ?? decimal.MinValue;
                    _numericValidator.Maximum = MaxValue ?? decimal.MaxValue;

                    if (!string.IsNullOrEmpty(Text) && decimal.TryParse(Text, out decimal intValue))
                    {
                        _numericValidator.Value = intValue;
                    }
                    else
                    {
                        _numericValidator.Value = 0;
                    }
                    break;

                case TextInputType.Float:
                    _textBox.Classes.Remove("passwordInput");
                    _textBox.PasswordChar = '\0';

                    _numericValidator.FormatString = "0.####";
                    _numericValidator.Increment = 0.1m;
                    _numericValidator.Minimum = MinValue ?? decimal.MinValue;
                    _numericValidator.Maximum = MaxValue ?? decimal.MaxValue;

                    if (!string.IsNullOrEmpty(Text) &&
                        decimal.TryParse(Text.Replace(',', '.'),
                        NumberStyles.Any, CultureInfo.InvariantCulture, out decimal floatValue))
                    {
                        _numericValidator.Value = floatValue;
                    }
                    else
                    {
                        _numericValidator.Value = 0;
                    }
                    break;

                case TextInputType.Password:
                    _textBox.Classes.Add("passwordInput");
                    _textBox.PasswordChar = '*';
                    break;

                case TextInputType.String:
                default:
                    _textBox.Classes.Remove("passwordInput");
                    _textBox.PasswordChar = '\0';
                    break;
            }
        }

        /// <summary>
        /// Обновляет пределы валидатора
        /// </summary>
        private void UpdateValidatorLimits()
        {
            if (InputType == TextInputType.Integer || InputType == TextInputType.Float)
            {
                _numericValidator.Minimum = MinValue ?? decimal.MinValue;
                _numericValidator.Maximum = MaxValue ?? decimal.MaxValue;

                // Проверяем текущее значение на соответствие новым ограничениям
                if (_numericValidator.Value < _numericValidator.Minimum)
                {
                    _numericValidator.Value = _numericValidator.Minimum;
                }
                else if (_numericValidator.Value > _numericValidator.Maximum)
                {
                    _numericValidator.Value = _numericValidator.Maximum;
                }
            }
        }

        /// <summary>
        /// Обработчик изменения текста в TextBox
        /// </summary>
        private void OnTextBoxTextChanged(object sender, EventArgs e)
        {
            if (_isSettingText)
            {
                return;
            }

            switch (InputType)
            {
                case TextInputType.Integer:
                    if (string.IsNullOrEmpty(_textBox.Text) || _textBox.Text == "-")
                    {
                        Text = _textBox.Text;
                        return;
                    }

                    // Пытаемся преобразовать в число
                    if (decimal.TryParse(_textBox.Text, out decimal intValue))
                    {
                        _isSettingText = true;
                        _numericValidator.Value = intValue;
                        _isSettingText = false;

                        Text = _textBox.Text;
                        TextChanged?.Invoke(this, intValue.ToString());
                    }
                    else
                    {
                        UpdateTextBoxFromNumericValidator();
                    }
                    break;

                case TextInputType.Float:
                    if (string.IsNullOrEmpty(_textBox.Text) || _textBox.Text == "-" ||
                        _textBox.Text == "." || _textBox.Text == "-." ||
                        _textBox.Text.EndsWith("."))
                    {
                        Text = _textBox.Text;
                        return;
                    }

                    string normalizedText = _textBox.Text.Replace(',', '.');

                    if (decimal.TryParse(normalizedText, NumberStyles.Any,
                        CultureInfo.InvariantCulture, out decimal floatValue))
                    {
                        _isSettingText = true;
                        _numericValidator.Value = floatValue;
                        _isSettingText = false;

                        Text = normalizedText;
                        TextChanged?.Invoke(this, normalizedText);
                    }
                    else
                    {
                        UpdateTextBoxFromNumericValidator();
                    }
                    break;

                case TextInputType.String:
                case TextInputType.Password:
                default:
                    Text = _textBox.Text;
                    TextChanged?.Invoke(this, Text);
                    break;
            }
        }

        /// <summary>
        /// Обработчик изменения значения в NumericUpDown
        /// </summary>
        private void OnNumericValidatorValueChanged(object sender, NumericUpDownValueChangedEventArgs e)
        {
            if (_isSettingText) return;

            UpdateTextBoxFromNumericValidator();
        }

        /// <summary>
        /// Обновляет TextBox из свойства Text
        /// </summary>
        private void UpdateTextBoxFromText()
        {
            if (_isSettingText) return;

            _isSettingText = true;
            try
            {
                _textBox.Text = Text;

                if (InputType == TextInputType.Integer || InputType == TextInputType.Float)
                {
                    if (!string.IsNullOrEmpty(Text) &&
                        decimal.TryParse(Text.Replace(',', '.'),
                        NumberStyles.Any, CultureInfo.InvariantCulture, out decimal value))
                    {
                        _numericValidator.Value = value;
                    }
                }
            }
            finally
            {
                _isSettingText = false;
            }
        }

        /// <summary>
        /// Обновляет TextBox и Text из значения NumericUpDown
        /// </summary>
        private void UpdateTextBoxFromNumericValidator()
        {
            if (_isSettingText) return;

            _isSettingText = true;
            try
            {
                if (_numericValidator.Value.HasValue)
                {
                    string formattedValue;

                    if (InputType == TextInputType.Integer)
                    {
                        formattedValue = _numericValidator.Value.Value.ToString();
                    }
                    else
                    {
                        formattedValue = _numericValidator.Value.Value.ToString(
                            "0.###", CultureInfo.InvariantCulture);
                    }

                    _textBox.Text = formattedValue;
                    Text = formattedValue;
                    TextChanged?.Invoke(this, formattedValue);
                }
            }
            finally
            {
                _isSettingText = false;
            }
        }

        /// <summary>
        /// Получает значение соответствующего типа
        /// </summary>
        public T GetValue<T>()
        {
            if ((InputType == TextInputType.Integer || InputType == TextInputType.Float) &&
                _numericValidator != null && _numericValidator.Value.HasValue)
            {
                if (typeof(T) == typeof(int) || typeof(T) == typeof(int?))
                {
                    return (T)(object)((int)_numericValidator.Value.Value);
                }
                else if (typeof(T) == typeof(float) || typeof(T) == typeof(float?))
                {
                    return (T)(object)((float)_numericValidator.Value.Value);
                }
                else if (typeof(T) == typeof(decimal) || typeof(T) == typeof(decimal?))
                {
                    return (T)(object)_numericValidator.Value.Value;
                }
            }

            if (string.IsNullOrEmpty(Text))
                return default;

            try
            {
                switch (InputType)
                {
                    case TextInputType.Integer:
                        if (typeof(T) == typeof(int) || typeof(T) == typeof(int?))
                            return (T)(object)int.Parse(Text);
                        break;

                    case TextInputType.Float:
                        if (typeof(T) == typeof(float) || typeof(T) == typeof(float?))
                            return (T)(object)float.Parse(Text.Replace(',', '.'), CultureInfo.InvariantCulture);
                        if (typeof(T) == typeof(decimal) || typeof(T) == typeof(decimal?))
                            return (T)(object)decimal.Parse(Text.Replace(',', '.'), CultureInfo.InvariantCulture);
                        break;
                }

                return (T)Convert.ChangeType(Text, typeof(T));
            }
            catch (Exception ex)
            {
                DebLogger.Debug($"Ошибка при конвертации '{Text}' в {typeof(T).Name}: {ex.Message}");
                return default;
            }
        }

        /// <summary>
        /// Устанавливает значение
        /// </summary>
        public void SetValue<T>(T value)
        {
            if (value == null)
            {
                Text = string.Empty;
                return;
            }

            if ((InputType == TextInputType.Float || InputType == TextInputType.Integer) &&
                (typeof(T) == typeof(float) || typeof(T) == typeof(int) ||
                 typeof(T) == typeof(double) || typeof(T) == typeof(decimal)))
            {
                decimal decimalValue = Convert.ToDecimal(value);
                _isSettingText = true;
                _numericValidator.Value = decimalValue;
                _isSettingText = false;

                UpdateTextBoxFromNumericValidator();
            }
            else
            {
                Text = value.ToString();
            }
        }
    }
}