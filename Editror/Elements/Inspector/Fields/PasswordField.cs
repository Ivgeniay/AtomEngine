﻿using Avalonia.Controls;
using Avalonia;
using System;

namespace Editor
{
    public class PasswordField : Grid
    {
        public static readonly StyledProperty<string> LabelProperty =
            AvaloniaProperty.Register<PasswordField, string>(nameof(Label), string.Empty);

        public static readonly StyledProperty<string> TextProperty =
            AvaloniaProperty.Register<PasswordField, string>(nameof(Text), string.Empty, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

        public static readonly StyledProperty<string> PlaceholderProperty =
            AvaloniaProperty.Register<PasswordField, string>(nameof(Placeholder), "Введите пароль");

        public static readonly StyledProperty<bool> IsReadOnlyProperty =
            AvaloniaProperty.Register<PasswordField, bool>(nameof(IsReadOnly), false);

        public static readonly StyledProperty<int?> MaxLengthProperty =
            AvaloniaProperty.Register<PasswordField, int?>(nameof(MaxLength), null);

        public string Label
        {
            get => GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public string Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
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
        public int? MaxLength
        {
            get => GetValue(MaxLengthProperty);
            set => SetValue(MaxLengthProperty, value);
        }

        public event EventHandler<string> TextChanged;

        private TextBlock _labelControl;
        private TextInputField _inputField;

        public PasswordField()
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
            Label = "Password Field";

            _inputField = new TextInputField
            {
                InputType = TextInputType.Password,
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

            _labelControl.Text = Label;
            _inputField.Text = Text;
            _inputField.Placeholder = Placeholder;
            _inputField.IsReadOnly = IsReadOnly;
            _inputField.MaxLength = MaxLength;
        }
    }
}