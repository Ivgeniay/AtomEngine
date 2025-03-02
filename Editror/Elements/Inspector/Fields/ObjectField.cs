using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;

namespace Editor
{
    public class ObjectField : Grid
    {
        public static readonly StyledProperty<string> ObjectPathProperty =
            AvaloniaProperty.Register<ObjectField, string>(nameof(ObjectPath));

        public static readonly StyledProperty<string[]> AllowedExtensionsProperty =
            AvaloniaProperty.Register<ObjectField, string[]>(nameof(AllowedExtensions));

        public static readonly StyledProperty<string> PlaceholderTextProperty =
            AvaloniaProperty.Register<ObjectField, string>(nameof(PlaceholderText), "None");

        /// <summary>
        /// Путь к выбранному объекту
        /// </summary>
        public string ObjectPath
        {
            get => GetValue(ObjectPathProperty);
            set => SetValue(ObjectPathProperty, value);
        }

        /// <summary>
        /// Разрешенные расширения файлов
        /// </summary>
        public string[] AllowedExtensions
        {
            get => GetValue(AllowedExtensionsProperty);
            set => SetValue(AllowedExtensionsProperty, value);
        }

        /// <summary>
        /// Текст, отображаемый когда объект не выбран
        /// </summary>
        public string PlaceholderText
        {
            get => GetValue(PlaceholderTextProperty);
            set => SetValue(PlaceholderTextProperty, value);
        }


        public static readonly StyledProperty<string> LabelProperty =
            AvaloniaProperty.Register<ObjectField, string>(nameof(Label), string.Empty);

        /// <summary>
        /// Текст метки поля
        /// </summary>
        public string Label
        {
            get => GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        /// <summary>
        /// Событие, вызываемое при изменении объекта
        /// </summary>
        public event EventHandler<string> ObjectChanged;

        private TextBlock _labelControl;
        private ObjectInputField _inputField;
        private bool _isSettingValue = false;

        public ObjectField()
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
            Label = "Object Field";

            _inputField = new ObjectInputField
            {
            };

            Grid.SetColumn(_labelControl, 0);
            Grid.SetColumn(_inputField, 1);

            Children.Add(_labelControl);
            Children.Add(_inputField);
        }

        private void SetupEventHandlers()
        {
            _inputField.ObjectChanged += (sender, e) => ObjectChanged?.Invoke(this, e);

            this.PropertyChanged += (s, e) =>
            {
                if (e.Property == LabelProperty)
                {
                    _labelControl.Text = Label;
                }
                else if (e.Property == AllowedExtensionsProperty)
                {
                    _inputField.AllowedExtensions = AllowedExtensions;
                }
                else if (e.Property == PlaceholderTextProperty)
                {
                    _inputField.PlaceholderText = PlaceholderText;
                }
                else if (e.Property == ObjectPathProperty)
                {
                    _inputField.ObjectPath = ObjectPath;
                }
            };

            _labelControl.Text = Label;
        }
    }
}
