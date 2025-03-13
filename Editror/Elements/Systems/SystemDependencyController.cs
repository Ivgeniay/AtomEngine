using Avalonia.Controls.Primitives;
using System.Collections.Generic;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Input;
using System.Linq;
using AtomEngine;
using Avalonia;
using System;
using MouseButton = Avalonia.Input.MouseButton;

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

        private List<SystemData> _systems;
        private SystemData _selectedSystem;
        private SystemCategory _currentCategory = SystemCategory.System;

        private SceneManager _sceneManager;

        private ContextMenu _contextMenu;
        private bool _isCardContextMenuOpen = false;
        private Point currentCursorPosition;

        private Dictionary<SystemData, SystemCardControl> _systemCards = new Dictionary<SystemData, SystemCardControl>();
        private Dictionary<int, StackPanel> _dependencyStacks = new Dictionary<int, StackPanel>();

        public SystemDependencyController()
        {
            _sceneManager = ServiceHub.Get<SceneManager>();
            _sceneManager.OnSceneInitialize += (e) =>
            {
                LoadSystems(e.Systems);
            };
            _sceneManager.OnSceneBeforeSave += () =>
            {
                if (_systems != null && _systems.Count > 0)
                    _sceneManager.CurrentScene.Systems = _systems.Select(s => s.Clone()).ToList();
            };
            InitializeUI();
            RegisterEvents();
        }

        public void LoadSystems(List<SystemData> systems)
        {
            _systems = systems.Select(s => s.Clone()).ToList();
            RefreshSystemsView();
        }

        public List<SystemData> GetSystemsData() => _systems.Select(s => s.Clone()).ToList();

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

            _contextMenu = CreateContextMenu();

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
        }

        private ContextMenu CreateContextMenu()
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

            return contextMenu;
        }

        private void RegisterEvents()
        {
            _normalSystemsButton.Click += (s, e) =>
            {
                _currentCategory = SystemCategory.System;
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
                case SystemCategory.System:
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
        
        private void OnSystemsContainerPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (e.InitialPressMouseButton == MouseButton.Right)
            {
                OpenContextMenu();
                e.Handled = true;
            }
            else
            {
                CloseContextMenu();
            }
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            CloseContextMenu();
            _isCardContextMenuOpen = false;
        }
        
        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            currentCursorPosition = e.GetPosition(this);
        }

        private void OpenContextMenu()
        {
            if (!_isCardContextMenuOpen)
                _contextMenu.Open(this);
        }
        private void CloseContextMenu()
        {
            _contextMenu?.Close();
        }

        private void OnAddSystemClick(object? sender, RoutedEventArgs e)
        {
            List<SearchPopupItem> availableSystems = GetAvailableSystems();

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
            searchDialog.SetPosition(currentCursorPosition);

            _systemsContainer.Children.Remove(targetButton);
        }

        private IEnumerable<Type> GetAvailableTypes()
        {
            IEnumerable<Type> systems;

            switch (_currentCategory)
            {
                case SystemCategory.System:
                    systems = ServiceHub.Get<AssemblyManager>().FindTypesByInterface<ISystem>(false);
                    break;
                case SystemCategory.Render:
                    systems = ServiceHub.Get<AssemblyManager>().FindTypesByInterface<IRenderSystem>(false);
                    break;
                case SystemCategory.Physics:
                    systems = ServiceHub.Get<AssemblyManager>().FindTypesByInterface<IPhysicSystem>(false);
                    break;
                default:
                    systems = new List<Type>();
                    break;
            }
            return systems;
        }
        private List<SearchPopupItem> GetAvailableSystems()
        {
            IEnumerable<Type> systems = GetAvailableTypes();
            List<Type> typeToRemove = new List<Type>();

            if (_currentCategory == SystemCategory.System)
            {
                foreach (Type type in systems)
                {
                    var interfaces = type.GetInterfaces();
                    if (interfaces != null && interfaces.Contains(typeof(IPhysicSystem)))
                    {
                        typeToRemove.Add(type);
                    }
                }
            }

            var addedSystemTypes = _systems
                .Where(s => s.Category == _currentCategory)
                .Select(s => s.SystemFullTypeName)
                .ToList();

            systems = systems
                .Where(t => !typeToRemove.Contains(t))
                .Where(t => !addedSystemTypes.Contains(t.FullName));

            return systems.Select(t => new SearchPopupItem(t.Name, t)).ToList();
        }

        private void AddSystem(Type systemType)
        {
            var systemData = new SystemData
            {
                SystemFullTypeName = systemType.FullName,
                Category = _currentCategory,
                ExecutionOrder = -1
            };

            _systems.Add(systemData);
            AddSystemCard(systemData, 0);

            SelectSystem(systemData);
            Status.SetStatus($"Added system: {systemData.SystemFullTypeName}");
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

            card.OnContexMenuOpen += (e) =>
            {
                _isCardContextMenuOpen = true;
            };
            card.OnContexMenuClosed += (e) =>
            {
                _isCardContextMenuOpen = false;
            };

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
                Status.SetStatus($"Moved system up: {system.SystemFullTypeName}");
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
                Status.SetStatus($"Moved system down: {system.SystemFullTypeName}");
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

            Status.SetStatus($"Removed system: {system.SystemFullTypeName}");
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
                Text = _selectedSystem.SystemFullTypeName,
                Margin = new Thickness(0, 0, 0, 10),
                Classes = { "parameterTitle" }
            };


            //Oreder
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

            //Dependencies
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
                    Content = system.SystemFullTypeName,
                    IsChecked = _selectedSystem.Dependencies.Any(e => e.SystemFullTypeName == system.SystemFullTypeName),
                };

                item.IsCheckedChanged += (s, e) =>
                {
                    if (e.Source is not CheckBox checkBox) return;

                    bool value = checkBox.IsChecked.HasValue ? checkBox.IsChecked.Value : false;
                    if (value)
                    {
                        var deletedSystem = _selectedSystem.Dependencies.FirstOrDefault(e => e.SystemFullTypeName == system.SystemFullTypeName);
                        if (deletedSystem != null)
                        {
                            _selectedSystem.Dependencies.Remove(deletedSystem);
                        }
                        _selectedSystem.Dependencies.Add(system);
                        RefreshSystemsView();
                    }
                    else
                    {
                        var deletedSystem = _selectedSystem.Dependencies.FirstOrDefault(e => e.SystemFullTypeName == system.SystemFullTypeName);
                        if (deletedSystem != null)
                        {
                            _selectedSystem.Dependencies.Remove(deletedSystem);
                            RefreshSystemsView();
                        }
                    }
                };

                var listBoxItem = new ListBoxItem
                {
                    Content = item,
                    Padding = new Thickness(5)
                };

                dependenciesListBox.Items.Add(listBoxItem);
            }

            //Worlds
            var includeInWorldsLabel = new TextBlock
            {
                Text = "Include in Worlds:",
                Margin = new Thickness(0, 5, 0, 2),
                Classes = { "parameterLabel" }
            };

            var worldsPanel = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 10),
                Spacing = 5
            };

            var worlds = _sceneManager.CurrentScene.Worlds;

            foreach (var world in worlds)
            {
                var isChecked = _selectedSystem.IncludInWorld.Contains(world.WorldId);

                var worldCheckBox = new CheckBox
                {
                    Content = world.WorldName,
                    IsChecked = isChecked,
                    Tag = world.WorldId,
                    Classes = { "worldCheckBox" }
                };

                worldCheckBox.IsCheckedChanged += (s, e) =>
                {
                    if (s is CheckBox checkBox && checkBox.Tag is uint worldId)
                    {
                        bool isSelected = checkBox.IsChecked.HasValue && checkBox.IsChecked.Value;

                        if (isSelected && !_selectedSystem.IncludInWorld.Contains(worldId))
                        {
                            _selectedSystem.IncludInWorld.Add(worldId);
                        }
                        else if (!isSelected && _selectedSystem.IncludInWorld.Contains(worldId))
                        {
                            _selectedSystem.IncludInWorld.Remove(worldId);
                        }
                    }
                };

                worldsPanel.Children.Add(worldCheckBox);
            }


            _parametersPanel.Children.Add(titleBlock);
            _parametersPanel.Children.Add(executionOrderLabel);
            _parametersPanel.Children.Add(executionOrderInput);
            _parametersPanel.Children.Add(dependenciesLabel);
            _parametersPanel.Children.Add(dependenciesListBox);
            _parametersPanel.Children.Add(includeInWorldsLabel);
            _parametersPanel.Children.Add(worldsPanel);
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
                DebLogger.Debug($"Cyclic dependency detected for system {system.SystemFullTypeName}");
            }

            return result;
        }

        public void Open()
        {
            _currentCategory = SystemCategory.System;

            PointerMoved += OnPointerMoved;
            PointerPressed += OnPointerPressed;

            UpdateCategoryButtonsState();
            RefreshSystemsView();
        }

        public void Close()
        {
            PointerMoved -= OnPointerMoved;
            PointerPressed -= OnPointerPressed;

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