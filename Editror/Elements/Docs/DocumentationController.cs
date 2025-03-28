using Avalonia.Controls.Primitives;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia;
using EngineLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Editor
{
    internal class DocumentationController : Grid, IWindowed
    {
        private DocumentInfo _currentDocument;
        private Dictionary<string, DocumentInfo> _documentLookup = new Dictionary<string, DocumentInfo>();
        private DocumentTreeNode _rootNode = new DocumentTreeNode { Name = "Root", IsCategory = true };

        private TreeView _categoryTreeView;
        private ScrollViewer _documentScrollViewer;
        private StackPanel _documentContent;
        private TextBox _searchBox;
        private StackPanel _breadcrumbsPanel;

        private HtmlDocumentationGenerator _htmlGenerator;

        public Action<object> OnClose { get; set; }

        public DocumentationController()
        {
            InitializeUI();
            LoadDocumentation();
        }

        private void InitializeUI()
        {
            HorizontalAlignment = HorizontalAlignment.Stretch;
            Width = double.NaN;
            VerticalAlignment = VerticalAlignment.Stretch;
            Height = double.NaN;

            RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); 

            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); 
            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });

            var searchPanel = new Grid
            {
                Margin = new Thickness(10, 5),
            };
            searchPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            searchPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            searchPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _searchBox = new TextBox
            {
                Watermark = "Search...",
                Margin = new Thickness(0, 0, 0, 0),
                Height = 38,
            };
            Grid.SetRow(_searchBox, 0);
            Grid.SetColumn(_searchBox, 0);

            _searchBox.TextChanged += OnSearchTextChanged;
            searchPanel.Children.Add(_searchBox);

            var exportButton = new Button
            {
                Content = "📤",
                Width = 38,
                Height = 38,
                Margin = new Thickness(0, 0, 0, 0),
                Padding = new Thickness(0, 0, 0, 0),
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };

            exportButton.Click += OnExportButtonClick;
            Grid.SetRow(exportButton, 0);
            Grid.SetColumn(exportButton, 1);
            searchPanel.Children.Add(exportButton);

            Grid.SetRow(searchPanel, 0);
            Grid.SetColumnSpan(searchPanel, 3);
            Children.Add(searchPanel);

            var contentPanel = new Grid()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            contentPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            contentPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            contentPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });

            _categoryTreeView = new TreeView
            {
                Margin = new Thickness(5)
            };
            _categoryTreeView.SelectionChanged += OnCategorySelected;

            Grid.SetColumn(_categoryTreeView, 0);
            contentPanel.Children.Add(_categoryTreeView);

            var splitter = new GridSplitter
            {
                Width = 4,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            Grid.SetColumn(splitter, 1);
            contentPanel.Children.Add(splitter);

            var documentPanel = new Grid()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            documentPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            documentPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            _breadcrumbsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                Margin = new Thickness(5),
                Classes = { "breadcrumbs" }
            };

            Grid.SetRow(_breadcrumbsPanel, 0);
            documentPanel.Children.Add(_breadcrumbsPanel);

            _documentScrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(5)
            };

            _documentContent = new StackPanel
            {
                Spacing = 10,
                Margin = new Thickness(5),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Classes = { "documentContent" }
            };

            _documentScrollViewer.Content = _documentContent;
            Grid.SetRow(_documentScrollViewer, 1);
            documentPanel.Children.Add(_documentScrollViewer);

            Grid.SetColumn(documentPanel, 2);
            contentPanel.Children.Add(documentPanel);

            Grid.SetRow(contentPanel, 1);
            Grid.SetColumnSpan(contentPanel, 3);
            Children.Add(contentPanel);

            _htmlGenerator = new HtmlDocumentationGenerator();
        }

        private void LoadDocumentation()
        {
            try
            {
                Dispatcher.UIThread.Post(async () =>
                {
                    await LoadDocumentationViaReflection();
                    LoadLocalDocumentation();
                });
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Ошибка загрузки документации: {ex.Message}");
            }
        }

        private async Task LoadDocumentationViaReflection()
        {
            try
            {
                EditorAssemblyManager assemblyManager = ServiceHub.Get<EditorAssemblyManager>();

                var types = assemblyManager.GetTypesByAttribute<DocumentationAttribute>();
                if (types != null)
                {
                    foreach (var type in types)
                    {
                        var attr = type.GetCustomAttribute<DocumentationAttribute>();
                        if (attr != null)
                        {
                            var doc = new DocumentInfo
                            {
                                Name = !string.IsNullOrEmpty(attr.Name) ? attr.Name : type.Name,
                                Title = !string.IsNullOrEmpty(attr.Title) ? attr.Title : string.Empty,
                                Description = attr.Description,
                                Author = string.IsNullOrWhiteSpace(attr.Author) ? "Unknown" : attr.Author,
                                Section = attr.DocumentationSection,
                                SubSection = attr.SubSection,
                                RelatedType = type
                            };

                            _documentLookup[doc.Name] = doc;
                            AddDocumentToTree(doc);
                        }
                    }
                }

                UpdateCategoryTree();
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Ошибка загрузки документации через рефлексию: {ex.Message}");
            }
        }

        private void LoadLocalDocumentation()
        {
            try
            {
                string sourceDocumentation = FileLoader.LoadFile("embedded:Resources/Documentation/doc.json");
                List<DocumentInfo> docsInfo = JsonConvert.DeserializeObject<List<DocumentInfo>>(sourceDocumentation);

                if (docsInfo != null && docsInfo.Count() > 0)
                {
                    foreach (var doc in docsInfo)
                    {
                        _documentLookup[doc.Name] = doc;
                        AddDocumentToTree(doc);
                    }

                    UpdateCategoryTree();
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Error loading local documentation: {ex.Message}");
            }
        }

        private async void OnExportButtonClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Title = "Сохранить документацию как HTML",
                    DefaultExtension = "html",
                    Filters = new List<FileDialogFilter>
                    {
                        new FileDialogFilter
                        {
                            Name = "HTML файлы",
                            Extensions = new List<string> { "html", "htm" }
                        }
                    }
                };

                var result = await saveDialog.ShowAsync(this.VisualRoot as Window);

                if (!string.IsNullOrEmpty(result))
                {
                    var html = _htmlGenerator.GenerateDocumentation(_documentLookup.Values.ToList(), _rootNode);
                    await System.IO.File.WriteAllTextAsync(result, html);
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void AddDocumentToTree(DocumentInfo doc)
        {
            DocumentTreeNode currentNode = _rootNode;

            if (!string.IsNullOrEmpty(doc.Section))
            {
                var sectionNode = currentNode.Children.FirstOrDefault(n => n.Name == doc.Section);
                if (sectionNode == null)
                {
                    sectionNode = new DocumentTreeNode { Name = doc.Section, IsCategory = true };
                    currentNode.Children.Add(sectionNode);
                }
                currentNode = sectionNode;

                if (!string.IsNullOrEmpty(doc.SubSection))
                {
                    var subSections = Regex.Split(doc.SubSection, @"[/\\]+");

                    foreach (var subSection in subSections.Where(s => !string.IsNullOrWhiteSpace(s)))
                    {
                        var subNode = currentNode.Children.FirstOrDefault(n => n.Name == subSection);
                        if (subNode == null)
                        {
                            subNode = new DocumentTreeNode { Name = subSection, IsCategory = true };
                            currentNode.Children.Add(subNode);
                        }
                        currentNode = subNode;
                    }
                }
            }

            var docNode = new DocumentTreeNode
            {
                Name = doc.Name,
                DisplayName = doc.Name,
                IsCategory = false,
                Document = doc
            };
            currentNode.Children.Add(docNode);
        }

        private void UpdateCategoryTree()
        {
            _categoryTreeView.ItemsSource = BuildTreeViewItems(_rootNode.Children);
        }

        private IEnumerable<TreeViewItem> BuildTreeViewItems(List<DocumentTreeNode> nodes)
        {
            foreach (var node in nodes)
            {
                var item = new TreeViewItem
                {
                    Header = node.IsCategory ? node.Name : node.DisplayName,
                    Tag = node
                };

                if (node.Children.Count > 0)
                {
                    item.ItemsSource = BuildTreeViewItems(node.Children);
                }

                yield return item;
            }
        }

        private void OnCategorySelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems != null && e.AddedItems.Count > 0 && e.AddedItems[0] is TreeViewItem item)
            {
                if (item.Tag is DocumentTreeNode node)
                {
                    if (node.IsCategory)
                    {
                        ShowCategoryDocuments(node);
                    }
                    else
                    {
                        ShowDocument(node.Document);
                    }
                    UpdateBreadcrumbs(node);
                }
            }
        }

        private void ShowCategoryDocuments(DocumentTreeNode category)
        {
            _documentContent.Children.Clear();

            _documentContent.Children.Add(new TextBlock
            {
                Text = category.Name,
                FontSize = 20,
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(0, 0, 0, 10),
                Classes = { "categoryTitle" }
            });

            var documents = GetAllDocumentsInCategory(category);

            if (documents.Count == 0)
            {
                _documentContent.Children.Add(new TextBlock
                {
                    Text = "В этой категории нет документов",
                    Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170))
                });
                return;
            }

            foreach (var doc in documents)
            {
                var docLink = new Button
                {
                    Content = doc.Name,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Classes = { "categoryDocLink" }
                };

                docLink.Click += (s, e) => ShowDocument(doc);
                _documentContent.Children.Add(docLink);
            }
        }

        private List<DocumentInfo> GetAllDocumentsInCategory(DocumentTreeNode category)
        {
            var result = new List<DocumentInfo>();

            result.AddRange(category.Children.Where(n => !n.IsCategory).Select(n => n.Document));
            foreach (var subCategory in category.Children.Where(n => n.IsCategory))
            {
                result.AddRange(GetAllDocumentsInCategory(subCategory));
            }

            return result;
        }

        private void ShowDocument(DocumentInfo doc)
        {
            _currentDocument = doc;
            _documentContent.Children.Clear();

            _documentContent.Children.Add(new TextBlock
            {
                Text = doc.Name,
                Classes = { "docName" }
            });

            _documentContent.Children.Add(new TextBlock
            {
                Text = doc.Title,
                Classes = { "docTitle" }
            });

            if (!string.IsNullOrEmpty(doc.Author))
            {
                _documentContent.Children.Add(new TextBlock
                {
                    Text = $"Author: {doc.Author}",
                    Classes = { "docAuthor" }
                });
            }

            if (!string.IsNullOrEmpty(doc.Description))
            {
                var descriptionPanel = new StackPanel();
                var parts = Regex.Split(doc.Description, @"<a>(.*?)<\/a>");

                for (int i = 0; i < parts.Length; i++)
                {
                    if (i % 2 == 0)
                    {
                        if (!string.IsNullOrEmpty(parts[i]))
                        {
                            descriptionPanel.Children.Add(new TextBlock
                            {
                                Text = parts[i],
                                TextWrapping = TextWrapping.Wrap,
                                Classes = { "docDescription" }
                            });
                        }
                    }
                    else
                    {
                        var linkText = parts[i];
                        var linkButton = new Button
                        {
                            Content = linkText,
                            Classes = { "docLink" }
                        };

                        linkButton.Click += (s, e) => OnLinkClicked(linkText);
                        descriptionPanel.Children.Add(linkButton);
                    }
                }

                _documentContent.Children.Add(descriptionPanel);
            }
        }

        private void OnLinkClicked(string docName)
        {
            if (_documentLookup.TryGetValue(docName, out var doc))
            {
                ShowDocument(doc);
                UpdateBreadcrumbsForDocument(doc);
            }
            else
            {
                ShowErrorMessage($"Документ \"{docName}\" не найден");
            }
        }

        private void UpdateBreadcrumbs(DocumentTreeNode node)
        {
            _breadcrumbsPanel.Children.Clear();

            var path = new List<DocumentTreeNode>();
            var current = node;

            while (current != null)
            {
                path.Insert(0, current);
                current = FindParent(_rootNode, current);
            }

            bool isFirst = true;
            foreach (var pathNode in path.Where(n => n != _rootNode))
            {
                if (!isFirst)
                {
                    _breadcrumbsPanel.Children.Add(new TextBlock
                    {
                        Text = " > ",
                        VerticalAlignment = VerticalAlignment.Center,
                        Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150))
                    });
                }

                var crumbButton = new Button
                {
                    Content = pathNode.IsCategory ? pathNode.Name : pathNode.DisplayName,
                    Classes = { "breadcrumbButton" }
                };

                var nodeRef = pathNode;
                crumbButton.Click += (s, e) =>
                {
                    if (nodeRef.IsCategory)
                        ShowCategoryDocuments(nodeRef);
                    else
                        ShowDocument(nodeRef.Document);

                    UpdateBreadcrumbs(nodeRef);
                };

                _breadcrumbsPanel.Children.Add(crumbButton);
                isFirst = false;
            }
        }

        private void UpdateBreadcrumbsForDocument(DocumentInfo doc)
        {
            var node = FindDocumentNode(_rootNode, doc);
            if (node != null)
            {
                UpdateBreadcrumbs(node);
            }
        }

        private DocumentTreeNode FindDocumentNode(DocumentTreeNode root, DocumentInfo doc)
        {
            foreach (var child in root.Children)
            {
                if (!child.IsCategory && child.Document == doc)
                {
                    return child;
                }

                if (child.IsCategory)
                {
                    var found = FindDocumentNode(child, doc);
                    if (found != null)
                        return found;
                }
            }

            return null;
        }

        private DocumentTreeNode FindParent(DocumentTreeNode root, DocumentTreeNode node)
        {
            foreach (var child in root.Children)
            {
                if (child == node)
                {
                    return root;
                }

                if (child.IsCategory)
                {
                    var parent = FindParent(child, node);
                    if (parent != null)
                        return parent;
                }
            }

            return null;
        }


        private void OnSearchTextChanged(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_searchBox.Text))
            {
                UpdateCategoryTree();
                return;
            }

            var searchText = _searchBox.Text.ToLowerInvariant();
            var results = new List<DocumentInfo>();

            results.AddRange(_documentLookup.Values.Where(doc => doc.Name.ToLowerInvariant().Contains(searchText)));

            if (results.Count == 0)
            {
                results.AddRange(_documentLookup.Values.Where(doc => doc.Title.ToLowerInvariant().Contains(searchText)));
            }

            if (results.Count == 0)
            {
                results.AddRange(_documentLookup.Values.Where(doc => doc.Description.ToLowerInvariant().Contains(searchText)));
            }
            ShowSearchResults(results, searchText);
        }

        private void ShowSearchResults(List<DocumentInfo> results, string searchText)
        {
            _documentContent.Children.Clear();

            _documentContent.Children.Add(new TextBlock
            {
                Text = $"Результаты поиска: {searchText}",
                FontSize = 18,
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(0, 0, 0, 15)
            });

            if (results.Count == 0)
            {
                _documentContent.Children.Add(new TextBlock
                {
                    Text = "Ничего не найдено",
                    Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170))
                });
                return;
            }

            foreach (var doc in results)
            {
                var resultPanel = new StackPanel
                {
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var titleButton = new Button
                {
                    Content = doc.Title,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Padding = new Thickness(2),
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    FontWeight = FontWeight.Bold
                };

                titleButton.Click += (s, e) =>
                {
                    ShowDocument(doc);
                    UpdateBreadcrumbsForDocument(doc);
                };

                resultPanel.Children.Add(titleButton);

                if (!string.IsNullOrEmpty(doc.Description))
                {
                    var previewText = doc.Description.Length > 100
                        ? doc.Description.Substring(0, 100) + "..."
                        : doc.Description;

                    resultPanel.Children.Add(new TextBlock
                    {
                        Text = previewText,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(10, 2, 0, 0)
                    });
                }

                var pathText = string.IsNullOrEmpty(doc.Section)
                    ? "(корень)"
                    : doc.Section + (string.IsNullOrEmpty(doc.SubSection) ? "" : " > " + doc.SubSection);

                resultPanel.Children.Add(new TextBlock
                {
                    Text = pathText,
                    FontStyle = FontStyle.Italic,
                    Foreground = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
                    Margin = new Thickness(10, 2, 0, 0)
                });

                _documentContent.Children.Add(resultPanel);
            }
        }

        private void ShowErrorMessage(string message)
        {
            _documentContent.Children.Clear();

            _documentContent.Children.Add(new TextBlock
            {
                Text = "Ошибка",
                FontSize = 18,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 0, 0)),
                Margin = new Thickness(0, 0, 0, 10)
            });

            _documentContent.Children.Add(new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap
            });
        }

        public void Open()
        {
            UpdateCategoryTree();
        }

        public void Close()
        {
        }

        public void Dispose()
        {
            OnClose?.Invoke(this);
        }

        public void Redraw()
        {
        }
    }
}