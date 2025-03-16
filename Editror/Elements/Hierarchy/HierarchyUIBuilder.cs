using System.Collections.ObjectModel;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Controls;
using Avalonia.Data;
using System.Linq;
using Avalonia;
using System;
using System.Collections.Generic;
using Avalonia.Layout;
using Avalonia.Media;

namespace Editor
{
    internal class HierarchyUIBuilder
    {
        private readonly HierarchyController _controller;

        public ListBox EntitiesList { get; private set; }
        public Canvas IndicatorCanvas { get; private set; }
        public Border DropIndicator { get; private set; }

        public HierarchyUIBuilder(HierarchyController controller)
        {
            _controller = controller;
        }

        public void InitializeGrid(Grid grid)
        {
             
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });

             
            var header = CreateHeader();
            Grid.SetRow(header, 0);

             
            EntitiesList = CreateEntitiesList();
            Grid.SetRow(EntitiesList, 1);

             
            DropIndicator = new Border
            {
                Height = 2,
                Background = new SolidColorBrush(Colors.DodgerBlue),
                IsVisible = false,
                ZIndex = 1000
            };

             
            IndicatorCanvas = new Canvas
            {
                Background = null,
                ZIndex = 100
            };
            Grid.SetRow(IndicatorCanvas, 1);
            IndicatorCanvas.Children.Add(DropIndicator);

             
            grid.Children.Add(header);
            grid.Children.Add(EntitiesList);
            grid.Children.Add(IndicatorCanvas);

             
            EntitiesList.ZIndex = 2;
            IndicatorCanvas.ZIndex = 3;
        }

        private Border CreateHeader()
        {
            return new Border
            {
                Classes = { "hierarchyHeader" }
            };
        }

        private ListBox CreateEntitiesList()
        {
            var listBox = new ListBox
            {
                Classes = { "entityList" },
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0),
                ItemsSource = _controller.Entities,
                AutoScrollToSelectedItem = true,
                SelectionMode = SelectionMode.Multiple,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top,
                MaxHeight = 10000
            };

            ScrollViewer.SetHorizontalScrollBarVisibility(listBox, ScrollBarVisibility.Disabled);
            ScrollViewer.SetVerticalScrollBarVisibility(listBox, ScrollBarVisibility.Auto);

             
            listBox.ItemTemplate = CreateEntityItemTemplate();

            return listBox;
        }

        public IDataTemplate CreateEntityItemTemplate()
        {
            return new FuncDataTemplate<EntityHierarchyItem>((entity, scope) =>
            {
                var grid = new Grid
                {
                    Classes = { "entityCell" }
                };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

                var indent = new Border
                {
                    Width = entity.Level * 20,
                    Background = null
                };
                Grid.SetColumn(indent, 0);

                var expandButton = new ToggleButton
                {
                    Classes = { "expandButton" },
                    IsChecked = entity.IsExpanded,
                    Content = entity.IsExpanded ? "▼" : "►",
                    Width = 10,
                    Height = 10,
                    IsVisible = entity.Children.Count > 0
                };

                expandButton.Click += (s, e) => {
                    if (s is ToggleButton button && button.DataContext is EntityHierarchyItem item)
                    {
                        var updatedItem = item;
                        updatedItem.IsExpanded = !item.IsExpanded;
                        button.Content = updatedItem.IsExpanded ? "▼" : "►";

                        int index = FindIndex(_controller.Entities, en => en.Id == item.Id);
                        if (index >= 0)
                        {
                            _controller.Entities[index] = updatedItem;
                            UpdateChildrenVisibility(updatedItem.Id, updatedItem.IsExpanded);
                        }

                        e.Handled = true;
                    }
                };
                Grid.SetColumn(expandButton, 1);

                var iconAndNameContainer = new StackPanel
                {
                    Classes = { "entityCell" },
                    Orientation = Orientation.Horizontal
                };

                var entityIcon = new TextBlock
                {
                    Text = "⬚",
                    Classes = { "entityIcon" },
                };

                var entityName = new TextBlock
                {
                    Classes = { "entityName" }
                };
                entityName.Bind(TextBlock.TextProperty, new Binding("Name"));

                iconAndNameContainer.Children.Add(entityIcon);
                iconAndNameContainer.Children.Add(entityName);
                Grid.SetColumn(iconAndNameContainer, 2);

                grid.Children.Add(indent);
                grid.Children.Add(expandButton);
                grid.Children.Add(iconAndNameContainer);

                return grid;
            });
        }

        private void UpdateChildrenVisibility(uint parentId, bool isVisible)
        {
            var entities = _controller.Entities.ToList();
            var entitiesToUpdate = new List<(int index, EntityHierarchyItem entity)>();

            foreach (var entity in entities)
            {
                if (entity.ParentId == parentId)
                {
                    var updatedEntity = entity;
                    updatedEntity.IsVisible = isVisible;

                    int index = FindIndex(_controller.Entities, e => e.Id == entity.Id);
                    if (index >= 0)
                    {
                        entitiesToUpdate.Add((index, updatedEntity));
                    }

                    if (entity.Children.Count > 0)
                    {
                        UpdateChildrenVisibility(entity.Id, isVisible && entity.IsExpanded);
                    }
                }
            }

            foreach (var (index, updatedEntity) in entitiesToUpdate)
            {
                if (index < _controller.Entities.Count)
                {
                    _controller.Entities[index] = updatedEntity;
                }
            }

            RefreshList();
        }

        private void RefreshList()
        {
            var visibleEntities = _controller.Entities.Where(e => e.IsVisible).ToList();
            EntitiesList.ItemsSource = null;
            EntitiesList.ItemsSource = visibleEntities;
        }

        private int FindIndex(ObservableCollection<EntityHierarchyItem> collection, Func<EntityHierarchyItem, bool> predicate)
        {
            for (int i = 0; i < collection.Count; i++)
            {
                if (predicate(collection[i]))
                    return i;
            }
            return -1;
        }
    }

}