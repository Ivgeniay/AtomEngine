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

        public string ObjectPath
        {
            get => GetValue(ObjectPathProperty);
            set => SetValue(ObjectPathProperty, value);
        }

        public string[] AllowedExtensions
        {
            get => GetValue(AllowedExtensionsProperty);
            set => SetValue(AllowedExtensionsProperty, value);
        }

        public string PlaceholderText
        {
            get => GetValue(PlaceholderTextProperty);
            set => SetValue(PlaceholderTextProperty, value);
        }


        public static readonly StyledProperty<string> LabelProperty =
            AvaloniaProperty.Register<ObjectField, string>(nameof(Label), string.Empty);

        public string Label
        {
            get => GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public event EventHandler<string> ObjectChanged;

        private TextBlock _labelControl;
        private ObjectInputField _inputField;
        private bool _isSettingValue = false;

        public ObjectField()
        {
            InitializeComponent();
            SetupEventHandlers();
        }

        internal void ResetValue(bool withIvoke = true)
        {
            ObjectPath = string.Empty;
            if (withIvoke) ObjectChanged?.Invoke(this, null);
            _inputField.ResetValue(withIvoke);
        }

        private void InitializeComponent()
        {
            this.InitializeInspectorFieldLayout();

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
