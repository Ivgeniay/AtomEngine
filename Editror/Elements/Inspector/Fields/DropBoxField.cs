using Avalonia;
using Avalonia.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;

namespace Editor
{
    public class DropBoxField : Grid
    {
        public static readonly StyledProperty<string> LabelProperty =
            AvaloniaProperty.Register<DropBoxField, string>(nameof(Label), string.Empty);

        public static readonly StyledProperty<object> SelectedItemProperty =
            AvaloniaProperty.Register<DropBoxField, object>(nameof(SelectedItem), null, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

        public static readonly StyledProperty<IList<object>> SelectedItemsProperty =
            AvaloniaProperty.Register<DropBoxField, IList<object>>(nameof(SelectedItems), null, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

        public static readonly StyledProperty<bool> IsMultiSelectProperty =
            AvaloniaProperty.Register<DropBoxField, bool>(nameof(IsMultiSelect), false);

        public static readonly StyledProperty<int> SelectedIndexProperty =
            AvaloniaProperty.Register<DropBoxField, int>(nameof(SelectedIndex), -1, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

        public static readonly StyledProperty<string> PlaceholderProperty =
            AvaloniaProperty.Register<DropBoxField, string>(nameof(Placeholder), "Выберите значение");

        public static readonly StyledProperty<bool> IsReadOnlyProperty =
            AvaloniaProperty.Register<DropBoxField, bool>(nameof(IsReadOnly), false);

        public static readonly StyledProperty<ObservableCollection<object>> ItemsProperty =
            AvaloniaProperty.Register<DropBoxField, ObservableCollection<object>>(nameof(Items));

        /// <summary>
        /// Текст метки поля
        /// </summary>
        public string Label
        {
            get => GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        /// <summary>
        /// Выбранный элемент (для одиночного выбора)
        /// </summary>
        public object SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        /// <summary>
        /// Выбранные элементы (для множественного выбора)
        /// </summary>
        public IList<object> SelectedItems
        {
            get => GetValue(SelectedItemsProperty);
            set => SetValue(SelectedItemsProperty, value);
        }

        /// <summary>
        /// Флаг включения режима множественного выбора
        /// </summary>
        public bool IsMultiSelect
        {
            get => GetValue(IsMultiSelectProperty);
            set => SetValue(IsMultiSelectProperty, value);
        }

        /// <summary>
        /// Индекс выбранного элемента
        /// </summary>
        public int SelectedIndex
        {
            get => GetValue(SelectedIndexProperty);
            set => SetValue(SelectedIndexProperty, value);
        }

        /// <summary>
        /// Подсказка при отсутствии выбранного элемента
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
        /// Список элементов
        /// </summary>
        public ObservableCollection<object> Items
        {
            get => GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
        }

        /// <summary>
        /// Событие изменения выбранного элемента
        /// </summary>
        public event EventHandler<SelectionChangedEventArgs> SelectionChanged;

        private TextBlock _labelControl;
        private ComboBox _comboBox;
        private ListBox _listBox;
        private bool _isSettingValue = false;

        public DropBoxField()
        {
            // Инициализируем Items новой коллекцией для каждого экземпляра
            Items = new ObservableCollection<object>();
            SelectedItems = new ObservableCollection<object>();

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
            Label = "DropBox Field";

            _comboBox = new ComboBox
            {
                PlaceholderText = Placeholder,
                ItemsSource = Items,
                IsEnabled = !IsReadOnly,
                IsVisible = !IsMultiSelect
            };

            _listBox = new ListBox
            {
                ItemsSource = Items,
                IsEnabled = !IsReadOnly,
                SelectionMode = Avalonia.Controls.SelectionMode.Multiple,
                MaxHeight = 120,
                IsVisible = IsMultiSelect
            };

            Grid.SetColumn(_labelControl, 0);
            Grid.SetColumn(_comboBox, 1);
            Grid.SetColumn(_listBox, 1);

            Children.Add(_labelControl);
            Children.Add(_comboBox);
            Children.Add(_listBox);
        }

        private void SetupEventHandlers()
        {
            this.PropertyChanged += (s, e) =>
            {
                if (e.Property == LabelProperty)
                {
                    _labelControl.Text = Label;
                }
                else if (e.Property == SelectedItemProperty && !_isSettingValue)
                {
                    if (!IsMultiSelect)
                    {
                        _comboBox.SelectedItem = SelectedItem;
                    }
                }
                else if (e.Property == SelectedItemsProperty && !_isSettingValue)
                {
                    if (IsMultiSelect && SelectedItems != null)
                    {
                        _listBox.Selection.Clear();
                        foreach (var item in SelectedItems)
                        {
                            int index = Items.IndexOf(item);
                            if (index >= 0)
                            {
                                _listBox.Selection.Select(index);
                            }
                        }
                    }
                }
                else if (e.Property == SelectedIndexProperty && !_isSettingValue)
                {
                    if (!IsMultiSelect)
                    {
                        _comboBox.SelectedIndex = SelectedIndex;
                    }
                }
                else if (e.Property == PlaceholderProperty)
                {
                    _comboBox.PlaceholderText = Placeholder;
                }
                else if (e.Property == IsReadOnlyProperty)
                {
                    _comboBox.IsEnabled = !IsReadOnly;
                    _listBox.IsEnabled = !IsReadOnly;
                }
                else if (e.Property == ItemsProperty)
                {
                    _comboBox.ItemsSource = Items;
                    _listBox.ItemsSource = Items;
                }
                else if (e.Property == IsMultiSelectProperty)
                {
                    _comboBox.IsVisible = !IsMultiSelect;
                    _listBox.IsVisible = IsMultiSelect;

                    // Если включен мульти-выбор, инициализируем ListBox выбранными элементами
                    if (IsMultiSelect)
                    {
                        if (SelectedItem != null)
                        {
                            // Перенос выбранного элемента из ComboBox в ListBox
                            if (SelectedItems == null)
                            {
                                SelectedItems = new ObservableCollection<object>();
                            }

                            if (!SelectedItems.Contains(SelectedItem))
                            {
                                SelectedItems.Add(SelectedItem);
                            }

                            int index = Items.IndexOf(SelectedItem);
                            if (index >= 0)
                            {
                                _listBox.Selection.Select(index);
                            }
                        }
                    }
                    else
                    {
                        // Если отключаем мульти-выбор, берем первый выбранный элемент и устанавливаем его в ComboBox
                        if (SelectedItems != null && SelectedItems.Count > 0)
                        {
                            SelectedItem = SelectedItems[0];
                            SelectedIndex = Items.IndexOf(SelectedItem);
                            _comboBox.SelectedItem = SelectedItem;
                            _comboBox.SelectedIndex = SelectedIndex;
                        }
                    }
                }
            };

            _comboBox.SelectionChanged += (s, e) =>
            {
                if (_isSettingValue || IsMultiSelect) return;

                _isSettingValue = true;
                try
                {
                    SelectedItem = _comboBox.SelectedItem;
                    SelectedIndex = _comboBox.SelectedIndex;

                    SelectionChanged?.Invoke(this, e);
                }
                finally
                {
                    _isSettingValue = false;
                }
            };

            _listBox.SelectionChanged += (s, e) =>
            {
                if (_isSettingValue || !IsMultiSelect) return;

                _isSettingValue = true;
                try
                {
                    if (SelectedItems == null)
                    {
                        SelectedItems = new ObservableCollection<object>();
                    }
                    else
                    {
                        SelectedItems.Clear();
                    }

                    foreach (var item in _listBox.Selection.SelectedItems)
                    {
                        SelectedItems.Add(item);
                    }

                    // Если есть хотя бы один выбранный элемент, устанавливаем первый как SelectedItem
                    if (SelectedItems.Count > 0)
                    {
                        SelectedItem = SelectedItems[0];
                        SelectedIndex = Items.IndexOf(SelectedItem);
                    }
                    else
                    {
                        SelectedItem = null;
                        SelectedIndex = -1;
                    }

                    SelectionChanged?.Invoke(this, e);
                }
                finally
                {
                    _isSettingValue = false;
                }
            };

            _labelControl.Text = Label;
            _comboBox.SelectedItem = SelectedItem;
            _comboBox.SelectedIndex = SelectedIndex;
            _comboBox.PlaceholderText = Placeholder;
            _comboBox.IsEnabled = !IsReadOnly;
            _listBox.IsEnabled = !IsReadOnly;

            // Инициализация режима отображения
            _comboBox.IsVisible = !IsMultiSelect;
            _listBox.IsVisible = IsMultiSelect;
        }

        /// <summary>
        /// Добавить элемент в список
        /// </summary>
        /// <param name="item">Элемент для добавления</param>
        public void AddItem(object item)
        {
            Items.Add(item);
        }

        /// <summary>
        /// Добавить несколько элементов в список
        /// </summary>
        /// <param name="items">Коллекция элементов для добавления</param>
        public void AddItems(IEnumerable<object> items)
        {
            foreach (var item in items)
            {
                Items.Add(item);
            }
        }

        /// <summary>
        /// Очистить список элементов
        /// </summary>
        public void ClearItems()
        {
            Items.Clear();
            SelectedItem = null;
            SelectedIndex = -1;
        }

        /// <summary>
        /// Установить выбранный элемент по индексу
        /// </summary>
        /// <param name="index">Индекс элемента</param>
        public void SetSelectedIndex(int index)
        {
            if (index >= -1 && index < Items.Count)
            {
                SelectedIndex = index;
                SelectedItem = index >= 0 ? Items[index] : null;
            }
        }

        /// <summary>
        /// Установить выбранный элемент
        /// </summary>
        /// <param name="item">Элемент для выбора</param>
        public void SetSelectedItem(object item)
        {
            SelectedItem = item;
            SelectedIndex = Items.IndexOf(item);

            if (IsMultiSelect && item != null)
            {
                if (SelectedItems == null)
                {
                    SelectedItems = new ObservableCollection<object>();
                }

                if (!SelectedItems.Contains(item))
                {
                    SelectedItems.Add(item);
                }
            }
        }

        /// <summary>
        /// Установить несколько выбранных элементов (для режима множественного выбора)
        /// </summary>
        /// <param name="items">Коллекция элементов для выбора</param>
        public void SetSelectedItems(IEnumerable<object> items)
        {
            if (!IsMultiSelect)
            {
                throw new InvalidOperationException("Метод SetSelectedItems доступен только в режиме множественного выбора (IsMultiSelect = true)");
            }

            if (items == null)
            {
                SelectedItems = null;
                SelectedItem = null;
                SelectedIndex = -1;
                return;
            }

            if (SelectedItems == null)
            {
                SelectedItems = new ObservableCollection<object>();
            }
            else
            {
                SelectedItems.Clear();
            }

            bool first = true;
            foreach (var item in items)
            {
                SelectedItems.Add(item);

                if (first)
                {
                    SelectedItem = item;
                    SelectedIndex = Items.IndexOf(item);
                    first = false;
                }
            }

            if (!first)
            {
                // Обновляем выделение в ListBox
                _listBox.Selection.Clear();
                foreach (var item in SelectedItems)
                {
                    int index = Items.IndexOf(item);
                    if (index >= 0)
                    {
                        _listBox.Selection.Select(index);
                    }
                }
            }
        }

        /// <summary>
        /// Добавить элемент к выбранным (для режима множественного выбора)
        /// </summary>
        /// <param name="item">Элемент для добавления к выбранным</param>
        public void AddSelectedItem(object item)
        {
            if (!IsMultiSelect)
            {
                throw new InvalidOperationException("Метод AddSelectedItem доступен только в режиме множественного выбора (IsMultiSelect = true)");
            }

            if (item == null)
                return;

            if (SelectedItems == null)
            {
                SelectedItems = new ObservableCollection<object>();
                SelectedItem = item;
                SelectedIndex = Items.IndexOf(item);
            }
            else if (!SelectedItems.Contains(item))
            {
                SelectedItems.Add(item);

                if (SelectedItem == null)
                {
                    SelectedItem = item;
                    SelectedIndex = Items.IndexOf(item);
                }
            }

            // Обновляем выделение в ListBox
            int index = Items.IndexOf(item);
            if (index >= 0)
            {
                _listBox.Selection.Select(index);
            }
        }

        /// <summary>
        /// Удалить элемент из выбранных (для режима множественного выбора)
        /// </summary>
        /// <param name="item">Элемент для удаления из выбранных</param>
        public void RemoveSelectedItem(object item)
        {
            if (!IsMultiSelect)
            {
                throw new InvalidOperationException("Метод RemoveSelectedItem доступен только в режиме множественного выбора (IsMultiSelect = true)");
            }

            if (item == null || SelectedItems == null)
                return;

            if (SelectedItems.Contains(item))
            {
                SelectedItems.Remove(item);

                // Если удалили текущий SelectedItem, нужно обновить его
                if (object.Equals(SelectedItem, item))
                {
                    if (SelectedItems.Count > 0)
                    {
                        SelectedItem = SelectedItems[0];
                        SelectedIndex = Items.IndexOf(SelectedItem);
                    }
                    else
                    {
                        SelectedItem = null;
                        SelectedIndex = -1;
                    }
                }

                // Обновляем выделение в ListBox
                int index = Items.IndexOf(item);
                if (index >= 0)
                {
                    _listBox.Selection.Deselect(index);
                }
            }
        }

        /// <summary>
        /// Очистить выбранные элементы
        /// </summary>
        public void ClearSelection()
        {
            SelectedItem = null;
            SelectedIndex = -1;

            if (SelectedItems != null)
            {
                SelectedItems.Clear();
            }

            if (IsMultiSelect)
            {
                _listBox.Selection.Clear();
            }
            else
            {
                _comboBox.SelectedItem = null;
                _comboBox.SelectedIndex = -1;
            }
        }
    }



}
