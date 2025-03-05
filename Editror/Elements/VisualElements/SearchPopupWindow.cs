using Avalonia.Controls.Primitives;
using System.Collections.Generic;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Input;
using Avalonia.Media;
using System.Linq;
using Avalonia;
using System;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Collections;

namespace Editor
{
    public class ComponentSearchDialog : Border
    {
        private TextBox _searchBox;
        private StackPanel _itemsContainer;
        private ScrollViewer _scrollViewer;
        private List<SearchPopupItem> _allItems = new List<SearchPopupItem>();
        private List<SearchPopupItem> _filteredItems = new List<SearchPopupItem>();
        private bool _showSearchBox;
        private Button _targetButton;
        private Point _offset = new Point(0, 2);

        /// <summary>
        /// Событие, вызываемое при выборе элемента
        /// </summary>
        public event Action<object> ItemSelected;

        /// <summary>
        /// Событие, вызываемое при закрытии диалога
        /// </summary>
        public event EventHandler Closed;

        /// <summary>
        /// Создает новый диалог поиска
        /// </summary>
        /// <param name="items">Список элементов для отображения</param>
        /// <param name="showSearchBox">Показывать ли строку поиска</param>
        public ComponentSearchDialog(IEnumerable<SearchPopupItem> items = null, bool showSearchBox = true)
        {
            // Установка высокого ZIndex для отображения поверх других элементов
            ZIndex = 9999;
            IsVisible = false;
            _showSearchBox = showSearchBox;

            InitializeUI();

            if (items != null)
            {
                AddItems(items);
            }
        }

        /// <summary>
        /// Инициализирует пользовательский интерфейс окна
        /// </summary>
        private void InitializeUI()
        {
            Background = new SolidColorBrush(Color.Parse("#252526"));
            BorderBrush = new SolidColorBrush(Color.Parse("#3F3F46"));
            BorderThickness = new Thickness(1);
            CornerRadius = new CornerRadius(3);
            Width = 300;
            MaxHeight = 300;
            BoxShadow = new BoxShadows(new BoxShadow
            {
                OffsetX = 0,
                OffsetY = 3,
                Blur = 5,
                Spread = 0,
                Color = Color.Parse("#0A0A0A")
            });

            var mainPanel = new StackPanel
            {
                Spacing = 0
            };

            if (_showSearchBox)
            {
                _searchBox = new TextBox
                {
                    Margin = new Thickness(8, 8, 8, 0),
                    Watermark = "Поиск...",
                    BorderThickness = new Thickness(1),
                    BorderBrush = new SolidColorBrush(Color.Parse("#3F3F46")),
                    Background = new SolidColorBrush(Color.Parse("#2D2D30")),
                    Foreground = new SolidColorBrush(Color.Parse("#CCCCCC")),
                    Padding = new Thickness(8, 4),
                    CornerRadius = new CornerRadius(2)
                };

                _searchBox.TextChanged += SearchBox_TextChanged;
                _searchBox.KeyDown += SearchBox_KeyDown;

                mainPanel.Children.Add(_searchBox);
            }

            _itemsContainer = new StackPanel
            {
                Spacing = 0
            };

            _scrollViewer = new ScrollViewer
            {
                Content = _itemsContainer,
                Margin = new Thickness(0, 8, 0, 8),
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                MaxHeight = 250
            };

            mainPanel.Children.Add(_scrollViewer);
            Child = mainPanel;
        }

        /// <summary>
        /// Добавляет элементы в диалог
        /// </summary>
        /// <param name="items">Список элементов для добавления</param>
        public void AddItems(IEnumerable<SearchPopupItem> items)
        {
            _allItems.AddRange(items);
            _filteredItems.Clear();
            _filteredItems.AddRange(_allItems);
            RebuildItemsList();
        }

        /// <summary>
        /// Очищает список всех элементов
        /// </summary>
        public void ClearItems()
        {
            _allItems.Clear();
            _filteredItems.Clear();
            _itemsContainer.Children.Clear();
        }

        /// <summary>
        /// Открывает диалог поиска относительно указанной кнопки
        /// </summary>
        /// <param name="targetButton">Кнопка, относительно которой открывается диалог</param>
        public void Show(Button targetButton)
        {
            _targetButton = targetButton;

            UpdatePosition();

            IsVisible = true;

            Dispatcher.UIThread.Post(() =>
            {
                if (_showSearchBox && _searchBox != null)
                {
                    _searchBox.Focus();
                    _searchBox.SelectAll();
                }
            }, DispatcherPriority.Default);

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel != null)
            {
                topLevel.AddHandler(PointerPressedEvent, GlobalPointerPressed, RoutingStrategies.Tunnel);
            }
        }

        /// <summary>
        /// Обработчик глобальных событий мыши для закрытия диалога при клике вне его
        /// </summary>
        private void GlobalPointerPressed(object sender, PointerPressedEventArgs e)
        {
            var point = e.GetPosition(this);
            var bounds = new Rect(0, 0, Bounds.Width, Bounds.Height);

            if (!bounds.Contains(point))
            {
                Close();
            }
        }

        /// <summary>
        /// Обновляет позицию диалога относительно целевой кнопки
        /// </summary>
        private void UpdatePosition()
        {
            if (_targetButton == null) return;

            var targetPosition = _targetButton.TranslatePoint(new Point(0, _targetButton.Bounds.Height), this.GetVisualRoot() as Visual);

            if (targetPosition.HasValue)
            {
                var x = targetPosition.Value.X;
                var y = targetPosition.Value.Y + _offset.Y;

                Canvas.SetLeft(this, x);
                Canvas.SetTop(this, y);
            }
        }

        /// <summary>
        /// Закрывает диалог
        /// </summary>
        public void Close()
        {
            IsVisible = false;

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel != null)
            {
                topLevel.RemoveHandler(PointerPressedEvent, GlobalPointerPressed);
            }

            Closed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Фильтрует список элементов по поисковому запросу
        /// </summary>
        /// <param name="searchText">Поисковый запрос</param>
        private void FilterItems(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                _filteredItems = new List<SearchPopupItem>(_allItems);
            }
            else
            {
                searchText = searchText.ToLower();
                _filteredItems = _allItems
                    .Where(item =>
                        item.DisplayName.ToLower().Contains(searchText) ||
                        (item.SearchTags != null && item.SearchTags.Any(tag => tag.ToLower().Contains(searchText))))
                    .ToList();
            }

            RebuildItemsList();
        }

        /// <summary>
        /// Перестраивает визуальный список элементов на основе отфильтрованного списка
        /// </summary>
        private void RebuildItemsList()
        {
            _itemsContainer.Children.Clear();

            string currentCategory = null;
            foreach (var item in _filteredItems)
            {
                if (!string.IsNullOrEmpty(item.Category) && item.Category != currentCategory)
                {
                    currentCategory = item.Category;

                    var categoryHeader = new TextBlock
                    {
                        Text = currentCategory,
                        Classes = { "categoryHeader" }
                    };

                    _itemsContainer.Children.Add(categoryHeader);
                }

                var itemButton = new Button
                {
                    Content = CreateItemContent(item),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Padding = new Thickness(8, 6),
                    Background = new SolidColorBrush(Color.Parse("#252526")),
                    BorderThickness = new Thickness(0),
                    CornerRadius = new CornerRadius(0),
                    Tag = item
                };

                itemButton.Classes.Add("menuItem");
                itemButton.Click += ItemButton_Click;

                _itemsContainer.Children.Add(itemButton);
            }

            if (_filteredItems.Count == 0)
            {
                var noResultsText = new TextBlock
                {
                    Text = "Ничего не найдено",
                    Classes = { "noResults" }
                };

                _itemsContainer.Children.Add(noResultsText);
            }
        }

        /// <summary>
        /// Создает визуальное представление элемента
        /// </summary>
        /// <param name="item">Элемент списка</param>
        /// <returns>Контрол, представляющий элемент</returns>
        private Control CreateItemContent(SearchPopupItem item)
        {
            var mainPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 2
            };

            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8
            };

            if (!string.IsNullOrEmpty(item.IconText))
            {
                var icon = new TextBlock
                {
                    Text = item.IconText,
                    Classes = { "componentIcon" }
                };

                headerPanel.Children.Add(icon);
            }

            var nameTextBlock = new TextBlock
            {
                Text = item.DisplayName,
                Classes = { "componentName" }
            };

            headerPanel.Children.Add(nameTextBlock);

            if (!string.IsNullOrEmpty(item.Subtitle))
            {
                var subtitleTextBlock = new TextBlock
                {
                    Text = item.Subtitle,
                    Classes = { "componentSubtitle" }
                };

                headerPanel.Children.Add(subtitleTextBlock);
            }

            mainPanel.Children.Add(headerPanel);

            if (item.Value is Type componentType)
            {
                
            }

            return mainPanel;
        }

        /// <summary>
        /// Обработчик изменения текста в поле поиска
        /// </summary>
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterItems(_searchBox.Text);
        }

        /// <summary>
        /// Обработчик нажатия клавиш в поле поиска
        /// </summary>
        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                FocusNextItem();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                SelectFirstVisibleItem();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Обработчик нажатия на элемент списка
        /// </summary>
        private void ItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is SearchPopupItem item)
            {
                ItemSelected?.Invoke(item.Value);
                Close();
            }
        }

        /// <summary>
        /// Фокусирует следующий элемент в списке
        /// </summary>
        private void FocusNextItem()
        {
            if (_itemsContainer.Children.Count > 0)
            {
                foreach (var child in _itemsContainer.Children)
                {
                    if (child is Button button)
                    {
                        button.Focus();
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Выбирает первый видимый элемент в списке
        /// </summary>
        private void SelectFirstVisibleItem()
        {
            if (_filteredItems.Count > 0)
            {
                ItemSelected?.Invoke(_filteredItems[0].Value);
                Close();
            }
        }
    }


    public class SearchPopupItem
    {
        public string DisplayName { get; set; }
        public string Subtitle { get; set; }
        public string IconText { get; set; }
        public string Category { get; set; }
        public string[] SearchTags { get; set; }
        public object Value { get; set; }

        public SearchPopupItem(string displayName, object value)
        {
            DisplayName = displayName;
            Value = value;
        }

        public SearchPopupItem(string displayName, object value, string iconText = null, string category = null, string subtitle = null, string[] searchTags = null)
        {
            DisplayName = displayName;
            Value = value;
            IconText = iconText;
            Category = category;
            Subtitle = subtitle;
            SearchTags = searchTags;
        }
    }

    public class SearchPopupItemCategoryComparer : IComparer<SearchPopupItem>
    {
        public int Compare(SearchPopupItem x, SearchPopupItem y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            int categoryComparison = string.Compare(x.Category, y.Category, StringComparison.OrdinalIgnoreCase);
            if (categoryComparison != 0)
            {
                return categoryComparison;
            }

            return string.Compare(x.DisplayName, y.DisplayName, StringComparison.OrdinalIgnoreCase);
        }
    }
}