using Avalonia.Controls.Primitives;
using System.Collections.Generic;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Input;
using System.Linq;
using AtomEngine;
using Avalonia;
using System;

namespace Editor
{
    internal class SystemDependencyController : Grid, IWindowed
    {
        public Action<object> OnClose { get; set; }

        private StackPanel _parametersPanel;
        private Grid _systemsContainer;
        private ScrollViewer _scrollViewer;

        private Button _normalSystemsButton;
        private Button _renderSystemsButton;
        private Button _physicsSystemsButton;

        private List<SystemData> _systems = new List<SystemData>();
        private SystemData _selectedSystem;
        private SystemCategory _currentCategory = SystemCategory.Normal;

        private Dictionary<SystemData, SystemCardControl> _systemCards = new Dictionary<SystemData, SystemCardControl>();
        private Dictionary<int, StackPanel> _dependencyStacks = new Dictionary<int, StackPanel>();

        public SystemDependencyController()
        {
            InitializeUI();
            RegisterEvents();
        }

        public void LoadSystems(List<SystemData> systems)
        {
            _systems = systems.Select(s => s.Clone()).ToList();
            RefreshSystemsView();
        }

        public List<SystemData> GetSystemsData()
        {
            return _systems.Select(s => s.Clone()).ToList();
        }

        private void InitializeUI()
        {
            RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });

            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

            var categoryPanel = new Grid();
            categoryPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            categoryPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

            var categoryLabel = new TextBlock
            {
                Text = "Parameters",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0),
                Classes = { "parameterTitle" }
            };

            var buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            _normalSystemsButton = new Button
            {
                Content = "Systems",
                Classes = { "categoryButton" }
            };

            _renderSystemsButton = new Button
            {
                Content = "Render Systems",
                Classes = { "categoryButton" }
            };

            _physicsSystemsButton = new Button
            {
                Content = "Physic Systems",
                Classes = { "categoryButton" }
            };

            buttonsPanel.Children.Add(_normalSystemsButton);
            buttonsPanel.Children.Add(_renderSystemsButton);
            buttonsPanel.Children.Add(_physicsSystemsButton);

            Grid.SetColumn(categoryLabel, 0);
            Grid.SetColumn(buttonsPanel, 1);

            categoryPanel.Children.Add(categoryLabel);
            categoryPanel.Children.Add(buttonsPanel);

            _parametersPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Classes = { "parametersPanel" }
            };

            _systemsContainer = new Grid
            {
                Margin = new Thickness(10)
            };

            _scrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = _systemsContainer
            };

            Grid.SetRow(categoryPanel, 0);
            Grid.SetColumn(categoryPanel, 0);
            Grid.SetColumnSpan(categoryPanel, 2);

            Grid.SetRow(_parametersPanel, 1);
            Grid.SetColumn(_parametersPanel, 0);

            Grid.SetRow(_scrollViewer, 1);
            Grid.SetColumn(_scrollViewer, 1);

            Children.Add(categoryPanel);
            Children.Add(_parametersPanel);
            Children.Add(_scrollViewer);

            InitializeSystemsContainer();
        }

        private void InitializeSystemsContainer()
        {
            _systemsContainer.Children.Clear();
            _dependencyStacks.Clear();

            var initialStack = new StackPanel
            {
                Classes = { "systemStack" }
            };

            _dependencyStacks[0] = initialStack;
            _systemsContainer.Children.Add(initialStack);

            _scrollViewer.PointerReleased += OnSystemsContainerPointerReleased;
            //_systemsContainer.PointerReleased += OnSystemsContainerPointerReleased;
        }

        private void RegisterEvents()
        {
            _normalSystemsButton.Click += (s, e) =>
            {
                _currentCategory = SystemCategory.Normal;
                RefreshSystemsView();
                UpdateCategoryButtonsState();
            };

            _renderSystemsButton.Click += (s, e) =>
            {
                _currentCategory = SystemCategory.Render;
                RefreshSystemsView();
                UpdateCategoryButtonsState();
            };

            _physicsSystemsButton.Click += (s, e) =>
            {
                _currentCategory = SystemCategory.Physics;
                RefreshSystemsView();
                UpdateCategoryButtonsState();
            };
        }

        private void UpdateCategoryButtonsState()
        {
            _normalSystemsButton.Classes.Remove("selected");
            _renderSystemsButton.Classes.Remove("selected");
            _physicsSystemsButton.Classes.Remove("selected");

            switch (_currentCategory)
            {
                case SystemCategory.Normal:
                    _normalSystemsButton.Classes.Add("selected");
                    break;
                case SystemCategory.Render:
                    _renderSystemsButton.Classes.Add("selected");
                    break;
                case SystemCategory.Physics:
                    _physicsSystemsButton.Classes.Add("selected");
                    break;
            }
        }

        private void OnSystemsContainerPointerReleased(object sender, PointerReleasedEventArgs e)
        {
            if (e.InitialPressMouseButton == MouseButton.Right)
            {
                var contextMenu = new ContextMenu
                {
                    Classes = { "systemContextMenu" }
                };

                var addSystemMenuItem = new MenuItem
                {
                    Header = "Add System",
                    Classes = { "systemMenuItem" }
                };

                addSystemMenuItem.Click += OnAddSystemClick;
                contextMenu.Items.Add(addSystemMenuItem);

                contextMenu.Open(_systemsContainer);
                e.Handled = true;
            }
        }

        private void OnAddSystemClick(object sender, RoutedEventArgs e)
        {
            List<SearchPopupItem> availableSystems = GetAvailableSystems();

            if (availableSystems.Count == 0)
            {
                Status.SetStatus("No available systems to add for this category");
                return;
            }

            var searchDialog = new ComponentSearchDialog(availableSystems);

            searchDialog.ItemSelected += (selectedType) =>
            {
                AddSystem((Type)selectedType);
            };

            Point position = new Point(
                _systemsContainer.Bounds.Width / 2,
                _systemsContainer.Bounds.Height / 2);

            var targetButton = new Button { IsVisible = false };
            _systemsContainer.Children.Add(targetButton);
            targetButton.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            targetButton.Arrange(new Rect(position, targetButton.DesiredSize));

            searchDialog.Show(targetButton);

            _systemsContainer.Children.Remove(targetButton);
        }

        private List<SearchPopupItem> GetAvailableSystems()
        {
            IEnumerable<Type> systems;

            switch (_currentCategory)
            {
                case SystemCategory.Normal:
                    systems = ServiceHub.Get<AssemblyManager>().FindTypesByInterface<ISystem>();
                    break;
                case SystemCategory.Render:
                    systems = ServiceHub.Get<AssemblyManager>().FindTypesByInterface<IRenderSystem>();
                    break;
                case SystemCategory.Physics:
                    systems = ServiceHub.Get<AssemblyManager>().FindTypesByInterface<IPhysicSystem>();
                    break;
                default:
                    systems = new List<Type>();
                    break;
            }

            var addedSystemTypes = _systems
                .Where(s => s.Category == _currentCategory)
                .Select(s => s.SystemType)
                .ToList();

            systems = systems.Where(t => !addedSystemTypes.Contains(t));

            return systems.Select(t => new SearchPopupItem(t.Name, t)).ToList();
        }

        private void AddSystem(Type systemType)
        {
            var systemData = new SystemData
            {
                SystemType = systemType,
                SystemName = systemType.Name,
                Category = _currentCategory,
                ExecutionOrder = -1
            };

            _systems.Add(systemData);
            AddSystemCard(systemData, 0);

            SelectSystem(systemData);
            Status.SetStatus($"Added system: {systemData.SystemName}");
        }

        private void AddSystemCard(SystemData system, int level)
        {
            if (!_dependencyStacks.ContainsKey(level))
            {
                var stack = new StackPanel
                {
                    Classes = { "systemStack" },
                    Margin = new Thickness(10 + level * 220, 10, 0, 0)
                };

                _dependencyStacks[level] = stack;
                _systemsContainer.Children.Add(stack);
            }

            var card = new SystemCardControl(system);
            card.Selected += (s, e) => SelectSystem(system);
            card.Delete += (s, e) => RemoveSystem(system);
            card.MoveUp += (s, e) => MoveSystemUp(system, level);
            card.MoveDown += (s, e) => MoveSystemDown(system, level);

            _systemCards[system] = card;
            _dependencyStacks[level].Children.Add(card);
        }

        private void MoveSystemUp(SystemData system, int level)
        {
            var stack = _dependencyStacks[level];
            var card = _systemCards[system];
            int index = stack.Children.IndexOf(card);

            if (index > 0)
            {
                stack.Children.RemoveAt(index);
                stack.Children.Insert(index - 1, card);
                Status.SetStatus($"Moved system up: {system.SystemName}");
            }
        }

        private void MoveSystemDown(SystemData system, int level)
        {
            var stack = _dependencyStacks[level];
            var card = _systemCards[system];
            int index = stack.Children.IndexOf(card);

            if (index < stack.Children.Count - 1)
            {
                stack.Children.RemoveAt(index);
                stack.Children.Insert(index + 1, card);
                Status.SetStatus($"Moved system down: {system.SystemName}");
            }
        }

        private void SelectSystem(SystemData system)
        {
            _selectedSystem = system;
            UpdateParametersPanel();

            foreach (var card in _systemCards.Values)
            {
                card.IsSelected = false;
            }

            if (_systemCards.ContainsKey(system))
            {
                _systemCards[system].IsSelected = true;
            }
        }

        private void RemoveSystem(SystemData system)
        {
            foreach (var s in _systems)
            {
                s.Dependencies.Remove(system);
            }

            _systems.Remove(system);

            RefreshSystemsView();

            if (_selectedSystem == system)
            {
                _selectedSystem = null;
                UpdateParametersPanel();
            }

            Status.SetStatus($"Removed system: {system.SystemName}");
        }

        private void UpdateParametersPanel()
        {
            _parametersPanel.Children.Clear();

            if (_selectedSystem == null)
            {
                var noSelectionText = new TextBlock
                {
                    Text = "Select a system to edit parameters",
                    Margin = new Thickness(10),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Classes = { "parameterLabel" }
                };

                _parametersPanel.Children.Add(noSelectionText);
                return;
            }

            var titleBlock = new TextBlock
            {
                Text = _selectedSystem.SystemName,
                Margin = new Thickness(0, 0, 0, 10),
                Classes = { "parameterTitle" }
            };

            var executionOrderLabel = new TextBlock
            {
                Text = "Execution Order:",
                Margin = new Thickness(0, 5, 0, 2),
                Classes = { "parameterLabel" }
            };

            var executionOrderInput = new NumericUpDown
            {
                Value = _selectedSystem.ExecutionOrder,
                Minimum = -1,
                Maximum = 100,
                Increment = 1,
                FormatString = "0",
                Margin = new Thickness(0, 0, 0, 10)
            };

            executionOrderInput.ValueChanged += (s, e) =>
            {
                if (executionOrderInput.Value.HasValue)
                {
                    _selectedSystem.ExecutionOrder = (int)executionOrderInput.Value.Value;
                }
            };

            var dependenciesLabel = new TextBlock
            {
                Text = "Dependencies:",
                Margin = new Thickness(0, 5, 0, 2),
                Classes = { "parameterLabel" }
            };

            var availableSystems = _systems
                .Where(s => s != _selectedSystem && s.Category == _selectedSystem.Category)
                .ToList();

            var dependenciesListBox = new ListBox
            {
                SelectionMode = SelectionMode.Multiple,
                Margin = new Thickness(0, 0, 0, 10),
                MaxHeight = 200
            };

            foreach (var system in availableSystems)
            {
                var item = new CheckBox
                {
                    Content = system.SystemName,
                    IsChecked = _selectedSystem.Dependencies.Contains(system)
                };

                item.Checked += (s, e) =>
                {
                    if (!_selectedSystem.Dependencies.Contains(system))
                    {
                        _selectedSystem.Dependencies.Add(system);
                        RefreshSystemsView();
                    }
                };

                item.Unchecked += (s, e) =>
                {
                    if (_selectedSystem.Dependencies.Contains(system))
                    {
                        _selectedSystem.Dependencies.Remove(system);
                        RefreshSystemsView();
                    }
                };

                var listBoxItem = new ListBoxItem
                {
                    Content = item,
                    Padding = new Thickness(5)
                };

                dependenciesListBox.Items.Add(listBoxItem);
            }

            _parametersPanel.Children.Add(titleBlock);
            _parametersPanel.Children.Add(executionOrderLabel);
            _parametersPanel.Children.Add(executionOrderInput);
            _parametersPanel.Children.Add(dependenciesLabel);
            _parametersPanel.Children.Add(dependenciesListBox);
        }

        private void RefreshSystemsView()
        {
            _systemCards.Clear();

            foreach (var stack in _dependencyStacks.Values)
            {
                stack.Children.Clear();
            }

            var categorySystems = _systems.Where(s => s.Category == _currentCategory).ToList();

            var levels = CalculateDependencyLevels(categorySystems);

            foreach (var entry in levels)
            {
                AddSystemCard(entry.Key, entry.Value);
            }

            if (_selectedSystem != null && _systemCards.ContainsKey(_selectedSystem))
            {
                _systemCards[_selectedSystem].IsSelected = true;
            }
        }

        private Dictionary<SystemData, int> CalculateDependencyLevels(List<SystemData> systems)
        {
            var result = new Dictionary<SystemData, int>();
            var processed = new HashSet<SystemData>();

            foreach (var system in systems.Where(s => !s.Dependencies.Any()))
            {
                result[system] = 0;
                processed.Add(system);
            }

            bool changed;
            do
            {
                changed = false;
                foreach (var system in systems.Where(s => !processed.Contains(s)))
                {
                    if (system.Dependencies.All(d => processed.Contains(d)))
                    {
                        int maxDependencyLevel = 0;
                        foreach (var dep in system.Dependencies)
                        {
                            maxDependencyLevel = Math.Max(maxDependencyLevel, result[dep]);
                        }

                        result[system] = maxDependencyLevel + 1;
                        processed.Add(system);
                        changed = true;
                    }
                }
            } while (changed && processed.Count < systems.Count);

            int lastLevel = result.Any() ? result.Values.Max() + 1 : 0;
            foreach (var system in systems.Where(s => !processed.Contains(s)))
            {
                result[system] = lastLevel;
                DebLogger.Debug($"Cyclic dependency detected for system {system.SystemName}");
            }

            return result;
        }

        public void Open()
        {
            _currentCategory = SystemCategory.Normal;
            UpdateCategoryButtonsState();
            RefreshSystemsView();
        }

        public void Close()
        {
            OnClose?.Invoke(this);
        }

        public void Dispose()
        {
        }

        public void Redraw()
        {
            RefreshSystemsView();
        }
    }

}