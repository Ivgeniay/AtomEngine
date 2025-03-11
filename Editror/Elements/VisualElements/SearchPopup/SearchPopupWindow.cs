using Avalonia.Controls.Primitives;
using System.Collections.Generic;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Threading;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Input;
using Avalonia.Media;
using System.Linq;
using Avalonia;
using System;
using AtomEngine;

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


        public event Action<object> ItemSelected;
        public event EventHandler Closed;
        public ComponentSearchDialog(IEnumerable<SearchPopupItem> items = null, bool showSearchBox = true)
        {
            ZIndex = 9999;
            IsVisible = false;
            _showSearchBox = showSearchBox;

            InitializeUI();

            if (items != null)
            {
                AddItems(items);
            }
        }

        private void InitializeUI()
        {
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
                    Watermark = "Search...",
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

        public void AddItems(IEnumerable<SearchPopupItem> items)
        {
            _allItems.AddRange(items);
            _filteredItems.Clear();
            _filteredItems.AddRange(_allItems);
            RebuildItemsList();
        }
        public void ClearItems()
        {
            _allItems.Clear();
            _filteredItems.Clear();
            _itemsContainer.Children.Clear();
        }
        public void Show(Button targetButton)
        {
            var rootCanvas = MainWindow.MainCanvas_;
            if (rootCanvas != null)
            {
                var existingDialogs = rootCanvas.Children.OfType<ComponentSearchDialog>().ToList();
                foreach (var dlg in existingDialogs)
                {
                    rootCanvas.Children.Remove(dlg);
                }

                rootCanvas.Children.Add(this);
            }
            else
            {
                DebLogger.Error("Не удалось найти корневой Canvas для отображения диалога");
                return;
            }

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
        private void GlobalPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var point = e.GetPosition(this);
            var bounds = new Rect(0, 0, Bounds.Width, Bounds.Height);

            if (!bounds.Contains(point))
            {
                Close();
            }
        }
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
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterItems(_searchBox.Text);
        }
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
        private void ItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is SearchPopupItem item)
            {
                ItemSelected?.Invoke(item.Value);
                Close();
            }
        }
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
        private void SelectFirstVisibleItem()
        {
            if (_filteredItems.Count > 0)
            {
                ItemSelected?.Invoke(_filteredItems[0].Value);
                Close();
            }
        }
    }
}