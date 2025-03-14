using Avalonia.Controls.Primitives;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia;
using System;
using AtomEngine;
using Avalonia.Media;
using System.Drawing;
using Avalonia.Controls.Shapes;
using Color = Avalonia.Media.Color;
using System.Diagnostics;

//using Color = Avalonia.Media.Color;

namespace Editor
{
    public class EditorToolbar : Control
    {
        private Border _container;
        private List<EditorToolbarCategory> _categories = new List<EditorToolbarCategory>();
        private Dictionary<EditorToolbarCategory, Flyout> _floyouts = new Dictionary<EditorToolbarCategory, Flyout>();
        private Dictionary<EditorToolbarCategory, StackPanel> _stack = new Dictionary<EditorToolbarCategory, StackPanel>();
        private StackPanel toolbarPanel;

        internal Action<object> OnClose { get; set; }

        internal EditorToolbar(Border container)
        {
            _container = container;

            if (_container == null)
            {
                throw new ArgumentNullException(nameof(container), "Toolbar container cannot be null");
            }

            CreateToolbar();
        }


        private void CreateToolbar()
        {
            if (_container == null) return;
            var toolbarBackground = new Border
            {
                Classes = { "toolbarBackground" },
            };

            toolbarPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 2,
                Margin = new Thickness(5, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Bottom,
            };


            toolbarBackground.Child = toolbarPanel;
            _container.Child = toolbarBackground;

            UpdateToolbar();
        }

        public void RegisterCathegory(EditorToolbarCategory editorToolbarCategory)
        {
            if (!_categories.Contains(editorToolbarCategory))
            {
                _categories.Add(editorToolbarCategory);
                UpdateToolbar();
            }
        }

        public void UpdateToolbar()
        {
            toolbarPanel.Children.Clear();
            _floyouts.Clear();
            _stack.Clear();
            foreach (var category in _categories)
            {
                var menuButton = CreateMenuButtonsFromCategory(category);
                toolbarPanel.Children.Add(menuButton);
            }
            AddStaticView();
        }

        private void AddStaticView()
        {
            Color.TryParse("#CCCCCC", out Color color);
            Button button = new Button()
            {
                Foreground = new SolidColorBrush(color),
                Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
            };

            Viewbox viewbox = new Viewbox()
            {
                Width = 12,
                Height = 12
            };

            Path path = new Path()
            {
                Data = Geometry.Parse("M0,0 L10,5 L0,10 Z"),
                Fill = new SolidColorBrush(color),
                Stretch = Stretch.Uniform
            };

            viewbox.Child = path;
            button.Content = viewbox;

            button.Click += async (s, e) =>
            {
                BuildManager buildManager = ServiceHub.Get<BuildManager>();
                DirectoryExplorer directoryExplorer = ServiceHub.Get<DirectoryExplorer>();
                ScriptProjectGenerator scriptProjectGenerator = ServiceHub.Get<ScriptProjectGenerator>();

                var result = await scriptProjectGenerator.BuildProject();
                if (!result)
                {
                    DebLogger.Error("Building Error");
                    return;
                }

                var assembly = ServiceHub.Get<ScriptProjectGenerator>().LoadCompiledAssembly();
                if (assembly != null)
                {
                    ServiceHub.Get<EditorAssemblyManager>().UpdateScriptAssembly(assembly);
                    DebLogger.Info("Проект скриптов успешно скомпилирован и загружен");
                }
                else
                {
                    DebLogger.Error("Script Assembly error");
                    return;
                }

                BuildConfig config = new BuildConfig();
                string cachepath = directoryExplorer.GetPath(DirectoryType.Cache);
                config.OutputPath = System.IO.Path.Combine(cachepath, "temp_build");
                if (System.IO.Directory.Exists(config.OutputPath))
                {
                    System.IO.Directory.Delete(config.OutputPath, true);
                }
                await buildManager.BuildProject(config);

                string exeName = $"{config.ProjectName}.exe";
                string exePath = System.IO.Path.Combine(config.OutputPath, config.ProjectName, exeName);

                if (System.IO.File.Exists(exePath))
                {
                    try
                    {
                        Process process = new Process();
                        process.StartInfo.FileName = exePath;
                        process.StartInfo.WorkingDirectory = config.OutputPath;

                        process.Start();

                        // process.WaitForExit();
                    }
                    catch (Exception ex)
                    {
                        Status.SetStatus($"Ошибка при запуске: {ex.Message}");
                    }
                }
                else
                {
                    Status.SetStatus($"Файл {exeName} не найден в директории {config.OutputPath}");
                }

            };
            toolbarPanel.Children.Add(button);
        }

        public IEnumerable<EditorToolbarCategory> GetEditorData() => _categories;
        private Button CreateMenuButtonsFromCategory(EditorToolbarCategory category)
        {
            var button = new Button
            {
                Content = category.Title,
                Classes = { "menuButton" },
                Padding = new Thickness(8, 4),
                Margin = new Thickness(1),
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };

            var flyout = new Flyout();
            var menuItemsPanel = new StackPanel
            {
                Spacing = 2,
                Width = 200,
            };
            _floyouts[category] = flyout;
            _stack[category] = menuItemsPanel;

            foreach (EditorToolbarButton item in category.Buttons)
            {
                CreateMenuButton(category, item);
            }

            flyout.Content = menuItemsPanel;
            FlyoutBase.SetAttachedFlyout(button, flyout);

            button.Click += (s, e) =>
            {
                FlyoutBase.ShowAttachedFlyout(button);
            };

            return button;
        }
    
        public Button CreateMenuButton(EditorToolbarCategory category, EditorToolbarButton button)
        {
            var menuItem = new Button
            {
                Content = button.Text,
                Classes = { "menuItem" },
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(8, 6)
            };

            menuItem.Click += (s, e) =>
            {
                button?.Action?.Invoke();
                _floyouts[category].Hide();
            };

            _stack[category].Children.Add(menuItem);
            return menuItem;
        }
    }

}