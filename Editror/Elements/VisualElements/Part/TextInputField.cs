using System.Globalization;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia;
using System;
using System.Text.RegularExpressions;
using Silk.NET.Vulkan;
using Avalonia.Threading;
using Avalonia.Input;

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

        public static readonly StyledProperty<int?> MaxValueProperty =
            AvaloniaProperty.Register<TextInputField, int?>(nameof(MaxValue), null);

        public static readonly StyledProperty<int?> MinValueProperty =
            AvaloniaProperty.Register<TextInputField, int?>(nameof(MinValue), null);

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
        public int? MaxValue
        {
            get => GetValue(MaxValueProperty);
            set => SetValue(MaxValueProperty, value);
        }

        /// <summary>
        /// Минимальное значение для числовых полей
        /// </summary>
        public int? MinValue
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
        private bool _processingTextChange = false;
        private string _previousText = string.Empty;
        private int _previousCaretIndex = 0;

        #endregion

        /// <summary>
        /// Конструктор
        /// </summary>
        public TextInputField()
        {
            InitializeComponent();
            Text = string.Empty;
            _textBox.Text = string.Empty;
            _previousText = string.Empty;
            _previousCaretIndex = 0;
            SetupEventHandlers();
        }

        /// <summary>
        /// Инициализация компонентов
        /// </summary>
        private void InitializeComponent()
        {
            ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

            // Основная рамка
            _mainBorder = new Border
            {
                Classes = { "textInputBorder" },
                Padding = new Thickness(4, 0)
            };

            Children.Add(_mainBorder);

            // Текстовое поле
            _textBox = new TextBox
            {
                Classes = { "textInpuntTextbox" },
                Watermark = Placeholder
            };

            _mainBorder.Child = _textBox;
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
                    UpdateTextBox();
                }
                else if (e.Property == InputTypeProperty)
                {
                    UpdateInputType();
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
            };

            _textBox.GetObservable(TextBox.TextProperty).Subscribe(text =>
            {
                if (_processingTextChange) return;

                _processingTextChange = true;
                try
                {
                    // Проверяем, вызовет ли новый текст переполнение
                    bool willOverflow = CheckOverflow(text);

                    if (willOverflow)
                    {
                        // Возвращаем предыдущее состояние
                        _textBox.Text = _previousText;
                        //_textBox.CaretIndex = _previousCaretIndex;

                        // Показываем предупреждение
                        ShowOverflowWarning();
                    }
                    else
                    {
                        // Если нет переполнения, обрабатываем нормально
                        if (text != Text)
                        {
                            // Получаем корректное значение
                            object processedValue = GetAvailableInput(text);
                            string newText = processedValue.ToString();

                            // Обновляем Text свойство
                            Text = newText;

                            // Обновляем текст в TextBox
                            if (_textBox.Text != newText)
                            {
                                int caretIndex = _textBox.CaretIndex;
                                _textBox.Text = newText;
                                _textBox.CaretIndex = Math.Min(caretIndex, newText.Length);
                            }

                            // Сохраняем успешное состояние как предыдущее
                            _previousText = _textBox.Text;
                            _previousCaretIndex = _textBox.CaretIndex;

                            // Скрываем предупреждение
                            HideOverflowWarning();

                            // Вызываем событие изменения
                            TextChanged?.Invoke(this, Text);
                        }
                    }
                }
                finally
                {
                    _processingTextChange = false;
                }
            });

            //_textBox. += (s, e) =>
            //{
            //    if (!_processingTextChange && !CheckOverflow(_textBox.Text))
            //    {
            //        _previousCaretIndex = _textBox.CaretIndex;
            //    }
            //};

            _textBox.GotFocus += (s, e) =>
            {
                if (!CheckOverflow(_textBox.Text))
                {
                    HideOverflowWarning();
                }
            };

            // Прямая обработка KeyDown для более быстрого реагирования
            _textBox.KeyDown += (s, e) =>
            {
                if (_processingTextChange) return;

                // После каждого нажатия клавиши проверяем актуальность текста
                if (e.Key != Key.Tab)
                {
                    // Используем Dispatcher для гарантии выполнения после обновления UI
                    Dispatcher.UIThread.Post(() =>
                    {
                        // Проверяем и исправляем расхождение между Text и TextBox.Text
                        if (Text != _textBox.Text)
                        {
                            _processingTextChange = true;
                            try
                            {
                                _textBox.Text = Text;
                                _textBox.InvalidateVisual();
                            }
                            finally
                            {
                                _processingTextChange = false;
                            }
                        }
                    }, DispatcherPriority.Render);
                }
            };


            this.PropertyChanged += (s, e) =>
            {
                if (e.Property == TextProperty && !_processingTextChange)
                {
                    _processingTextChange = true;
                    try
                    {
                        UpdateTextBox();
                    }
                    finally
                    {
                        _processingTextChange = false;
                    }
                }
                else if (e.Property == InputTypeProperty)
                {
                    UpdateInputType();
                }
            };

            UpdateInputType();
        }

        /// <summary>
        /// Обновляет текст в текстбоксе
        /// </summary>
        private void UpdateTextBox()
        {
            if (_textBox.Text != Text)
            {
                _textBox.Text = Text;
            }
        }

        /// <summary>
        /// Применяет настройки типа ввода
        /// </summary>
        private void UpdateInputType()
        {
            switch (InputType)
            {
                case TextInputType.Integer:
                    _textBox.Classes.Remove("passwordInput");
                    _textBox.PasswordChar = '\0';
                    MinValue = -2147483648;
                    MaxValue = 2147483647;
                    break;

                case TextInputType.Float:
                    _textBox.Classes.Remove("passwordInput");
                    _textBox.PasswordChar = '\0';
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


        private bool _isOverflowWarning = false;
        /// <summary>
        /// Проверяет ввод в зависимости от типа
        /// </summary>
        private object GetAvailableInput(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return InputType switch
                {
                    TextInputType.Integer => 0,
                    TextInputType.Float => 0f,
                    _ => string.Empty
                };
            }

            switch (InputType)
            {
                case TextInputType.Integer:
                    if (text.Length == 1 && text == "-")
                        return "-";

                    string cleanedInt = Regex.Replace(text, @"[^\d\-]", "");

                    // Обработка минуса
                    if (cleanedInt.Contains('-'))
                    {
                        if (cleanedInt.StartsWith("-"))
                            cleanedInt = "-" + cleanedInt.Substring(1).Replace("-", "");
                        else
                            cleanedInt = cleanedInt.Replace("-", "");
                    }

                    // Обработка ведущих нулей
                    if (cleanedInt.StartsWith("0") && cleanedInt.Length > 1 && !cleanedInt.StartsWith("-"))
                        cleanedInt = cleanedInt.TrimStart('0');
                    else if (cleanedInt.StartsWith("-0") && cleanedInt.Length > 2)
                        cleanedInt = "-" + cleanedInt.Substring(2).TrimStart('0');

                    // Проверка на минус для продолжения ввода
                    if (cleanedInt == "-")
                        return cleanedInt;

                    // Проверка на парсинг и ограничения
                    if (int.TryParse(cleanedInt, out int intValue))
                    {
                        if (MinValue.HasValue && intValue < MinValue.Value)
                            return MinValue.Value;

                        if (MaxValue.HasValue && intValue > MaxValue.Value)
                            return MaxValue.Value;

                        return intValue;
                    }

                    return string.IsNullOrEmpty(cleanedInt) ? 0 : 0;

                case TextInputType.Float:

                case TextInputType.Password:
                case TextInputType.String:
                default:
                    return text;
            }
        }

        /// <summary>
        /// Проверяет, вызовет ли ввод переполнение типа
        /// </summary>
        private bool CheckOverflow(string text)
        {
            switch (InputType)
            {
                case TextInputType.Integer:
                    // Очищаем и нормализуем ввод
                    string cleanedInt = Regex.Replace(text, @"[^\d\-]", "");

                    // Обработка минуса
                    if (cleanedInt.Contains('-'))
                    {
                        if (cleanedInt.StartsWith("-"))
                            cleanedInt = "-" + cleanedInt.Substring(1).Replace("-", "");
                        else
                            cleanedInt = cleanedInt.Replace("-", "");
                    }

                    // Для промежуточного состояния "-" переполнения нет
                    if (cleanedInt == "-")
                        return false;

                    // Проверка на переполнение для положительных чисел
                    if (!cleanedInt.StartsWith("-"))
                    {
                        if (cleanedInt.Length > 10)
                            return true;

                        if (cleanedInt.Length == 10 && string.Compare(cleanedInt, "2147483647") > 0)
                            return true;
                    }
                    // Проверка на переполнение для отрицательных чисел
                    else
                    {
                        if (cleanedInt.Length > 11)
                            return true;

                        if (cleanedInt.Length == 11 && string.Compare(cleanedInt, "-2147483648") < 0)
                            return true;
                    }

                    // Проверяем ограничения
                    if (int.TryParse(cleanedInt, out int intValue))
                    {
                        if (MinValue.HasValue && intValue < MinValue.Value)
                            return true;

                        if (MaxValue.HasValue && intValue > MaxValue.Value)
                            return true;
                    }

                    return false;

                case TextInputType.Float:
                    // Аналогичные проверки для float
                    string normalizedText = text.Replace(',', '.');
                    string cleanedFloat = Regex.Replace(normalizedText, @"[^\d\.\-]", "");

                    // Проверка на промежуточные состояния
                    if (cleanedFloat == "-" || cleanedFloat == "." || cleanedFloat == "-." ||
                        cleanedFloat == "0." || cleanedFloat == "-0.")
                        return false;

                    // Проверка на переполнение через парсинг
                    if (float.TryParse(cleanedFloat, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue))
                    {
                        return floatValue == float.PositiveInfinity || floatValue == float.NegativeInfinity;
                    }

                    return false;

                default:
                    return false;
            }
        }

        private void ShowOverflowWarning()
        {
            _mainBorder.Classes.Add("overflowWarning");
            ToolTip.SetTip(_mainBorder, "Достигнуто максимальное допустимое значение");
        }

        private void HideOverflowWarning()
        {
            _mainBorder.Classes.Remove("overflowWarning");
            ToolTip.SetTip(_mainBorder, null);
        }

        /// <summary>
        /// Вычисляет оптимальную позицию каретки после валидации текста
        /// </summary>
        /// <param name="oldText">Текст до изменения</param>
        /// <param name="newText">Текст после валидации</param>
        /// <param name="oldCaretIndex">Позиция каретки до изменения</param>
        /// <returns>Новая позиция каретки</returns>
        private int CalculateCaretPosition(string oldText, string newText, int oldCaretIndex)
        {
            // Если позиция в самом конце, сохраняем её в конце
            if (oldCaretIndex >= oldText.Length)
                return newText.Length;

            // Если тексты идентичны, позиция не меняется
            if (oldText == newText)
                return oldCaretIndex;

            // Находим самое длинное общее начало строк
            int commonPrefixLength = 0;
            int minLength = Math.Min(oldCaretIndex, Math.Min(oldText.Length, newText.Length));

            for (int i = 0; i < minLength; i++)
            {
                if (oldText[i] != newText[i])
                    break;
                commonPrefixLength++;
            }

            // Вычисляем коэффициент масштабирования
            double scale = 1.0;

            // Если каретка находится после общего префикса
            if (oldCaretIndex > commonPrefixLength)
            {
                // Вычисляем относительную позицию каретки в изменяемой части
                double relativePos = 0;

                if (oldText.Length > commonPrefixLength)
                {
                    relativePos = (double)(oldCaretIndex - commonPrefixLength) /
                                  (oldText.Length - commonPrefixLength);
                }

                // Применяем эту относительную позицию к новому тексту
                int offsetInNew = 0;

                if (newText.Length > commonPrefixLength)
                {
                    offsetInNew = (int)Math.Round(relativePos * (newText.Length - commonPrefixLength));
                    return Math.Min(commonPrefixLength + offsetInNew, newText.Length);
                }

                return commonPrefixLength;
            }

            // Если каретка в общем префиксе, оставляем как есть
            return oldCaretIndex;
        }

        /// <summary>
        /// Получает значение соответствующего типа
        /// </summary>
        public T GetValue<T>()
        {
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
                        break;
                }

                // Для других типов пробуем конвертировать
                return (T)Convert.ChangeType(Text, typeof(T));
            }
            catch
            {
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

            if (InputType == TextInputType.Float && typeof(T) == typeof(float))
            {
                // Форматируем число с плавающей точкой
                Text = ((float)(object)value).ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                Text = value.ToString();
            }
        }
    }
}
