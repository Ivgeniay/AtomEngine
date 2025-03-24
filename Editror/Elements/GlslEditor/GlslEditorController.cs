using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using AvaloniaEdit.Rendering;
using AvaloniaEdit.Document;
using AvaloniaEdit;
using AvaloniaEdit.Search;
using AvaloniaEdit.Folding;
using AvaloniaEdit.TextMate;
using AvaloniaEdit.Highlighting;
using TextMateSharp.Grammars;
using AvaloniaEdit.Highlighting.Xshd;
using System.Reflection;
using System.Xml;

namespace Editor
{
    public class GlslEditorController : Grid, IWindowed, ICacheble
    {
        private string _currentFilePath;
        private TextEditor _textEditor;
        private GlslAnalyzer _glslAnalyzer;
        private Button _saveButton;
        private StackPanel _toolbarPanel;
        private List<SimpleTextMarker> _errorMarkers = new List<SimpleTextMarker>();
        private List<IncludeFileInfo> _includedFiles = new List<IncludeFileInfo>();

        public Action<object> OnClose { get; set; }

        public GlslEditorController()
        {
            InitializeUI();
            _glslAnalyzer = new GlslAnalyzer();
            _glslAnalyzer.ErrorFound += OnErrorFound;
            _glslAnalyzer.IncludeFound += OnIncludeFound;
        }

        private void InitializeUI()
        {
            RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });

            _toolbarPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Classes = { "toolbarPanel" }
            };

            var toolbarBackground = new Border
            {
                Classes = { "toolbarBackground" },
                Child = _toolbarPanel
            };

            Grid.SetRow(toolbarBackground, 0);
            Children.Add(toolbarBackground);

            _saveButton = new Button
            {
                Content = "Сохранить",
                Classes = { "menuButton" },
                Margin = new Avalonia.Thickness(5)
            };

            _saveButton.Click += OnSaveClick;
            _toolbarPanel.Children.Add(_saveButton);

            _textEditor = new TextEditor
            {
                FontFamily = new FontFamily("Consolas, Menlo, Monospace"),
                FontSize = 12,
                ShowLineNumbers = true,
                WordWrap = false,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.Parse("#1E1E1E")),
                Options = new TextEditorOptions
                {
                    ConvertTabsToSpaces = true,
                    IndentationSize = 4
                }
            };

            SetupSyntaxHighlighting();

            _textEditor.TextChanged += OnTextChanged;

            Grid.SetRow(_textEditor, 1);
            Children.Add(_textEditor);
        }

        private void SetupSyntaxHighlighting()
        {
            try
            {
                // Пытаемся загрузить подсветку GLSL из ресурсов
                using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("Editor.Resources.GLSL.xshd"))
                {
                    if (s != null)
                    {
                        using (XmlReader reader = XmlReader.Create(s))
                        {
                            _textEditor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                        }
                    }
                    else
                    {
                        // Если не нашли ресурс, используем подсветку C#
                        _textEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");
                    }
                }
            }
            catch (Exception ex)
            {
                Status.SetStatus($"Ошибка при установке подсветки синтаксиса: {ex.Message}");

                // В случае ошибки используем подсветку C#
                _textEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");
            }
        }

        public void OpenFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                Status.SetStatus("Файл не существует");
                return;
            }

            try
            {
                _currentFilePath = filePath;
                string content = File.ReadAllText(filePath);

                // Отладочная информация
                Status.SetStatus($"Файл прочитан. Размер: {content.Length} символов");

                _textEditor.Document.Text = content;

                _textEditor.InvalidateVisual();
                _textEditor.TextArea.TextView.InvalidateVisual();

                _textEditor.CaretOffset = 0;
                _glslAnalyzer.Analyze(content, filePath);

                Status.SetStatus($"Файл открыт: {Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                Status.SetStatus($"Ошибка при открытии файла: {ex.Message}");
            }
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                Status.SetStatus("Нет открытого файла для сохранения");
                return;
            }

            try
            {
                File.WriteAllText(_currentFilePath, _textEditor.Document.Text);
                Status.SetStatus($"Файл сохранен: {Path.GetFileName(_currentFilePath)}");
            }
            catch (Exception ex)
            {
                Status.SetStatus($"Ошибка при сохранении файла: {ex.Message}");
            }
        }

        private void OnTextChanged(object sender, EventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (!string.IsNullOrEmpty(_currentFilePath))
                {
                    ClearErrorMarkers();
                    _glslAnalyzer.Analyze(_textEditor.Document.Text, _currentFilePath);
                }
            }, DispatcherPriority.Background);
        }

        private void OnErrorFound(object sender, GlslErrorEventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    var line = _textEditor.Document.GetLineByNumber(e.Line);
                    int column = Math.Min(e.Column, line.Length + 1);

                    var errorMarker = new SimpleTextMarker
                    {
                        Line = e.Line,
                        Column = column,
                        Length = e.Length > 0 ? e.Length : 1,
                        Message = e.Message
                    };

                    // Применяем визуальное выделение ошибки в тексте
                    _textEditor.TextArea.TextView.InvalidateVisual();

                    _errorMarkers.Add(errorMarker);
                }
                catch (Exception ex)
                {
                    Status.SetStatus($"Ошибка при создании маркера: {ex.Message}");
                }
            });
        }

        private void OnIncludeFound(object sender, GlslIncludeEventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    string includeFilePath = Path.GetFullPath(Path.Combine(
                        Path.GetDirectoryName(_currentFilePath), e.IncludePath));

                    if (File.Exists(includeFilePath))
                    {
                        string includeContent = File.ReadAllText(includeFilePath);

                        _includedFiles.Add(new IncludeFileInfo
                        {
                            DirectiveLine = e.Line,
                            FilePath = includeFilePath,
                            Content = includeContent,
                            IsFolded = true
                        });

                        InsertIncludeContent(e.Line, includeFilePath, includeContent);
                    }
                    else
                    {
                        OnErrorFound(this, new GlslErrorEventArgs
                        {
                            Line = e.Line,
                            Column = e.Column,
                            Length = e.Length,
                            Message = $"Включаемый файл не найден: {e.IncludePath}"
                        });
                    }
                }
                catch (Exception ex)
                {
                    Status.SetStatus($"Ошибка при обработке включения: {ex.Message}");
                }
            });
        }

        private void InsertIncludeContent(int line, string filePath, string content)
        {
            try
            {
                var document = _textEditor.Document;
                var lineObj = document.GetLineByNumber(line);

                // Вставляем содержимое включаемого файла как комментарий
                document.Insert(lineObj.EndOffset, $"\n// Начало {Path.GetFileName(filePath)}\n{content}\n// Конец {Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                Status.SetStatus($"Ошибка при вставке включаемого содержимого: {ex.Message}");
            }
        }

        private void ClearErrorMarkers()
        {
            _errorMarkers.Clear();
            _textEditor.TextArea.TextView.InvalidateVisual();
        }

        public void Open()
        {
            // Метод вызывается при открытии окна
        }

        public void Close()
        {
            OnClose?.Invoke(this);
        }

        public void Dispose()
        {
            Close();
        }

        public void Redraw()
        {
            _textEditor.TextArea.TextView.InvalidateVisual();
        }

        public void FreeCache()
        {
            Dispatcher.UIThread.Invoke(new Action(() =>
            {
                _currentFilePath = null;
                _textEditor.Document.Text = string.Empty;
                ClearErrorMarkers();
                _includedFiles.Clear();
            }));
        }
    }

    public class IncludeFileInfo
    {
        public int DirectiveLine { get; set; }
        public string FilePath { get; set; }
        public string Content { get; set; }
        public bool IsFolded { get; set; }
    }

    public class SimpleTextMarker
    {
        public int Line { get; set; }
        public int Column { get; set; }
        public int Length { get; set; }
        public string Message { get; set; }
    }







    public class GlslAnalyzer
    {
        public event EventHandler<GlslErrorEventArgs> ErrorFound;
        public event EventHandler<GlslIncludeEventArgs> IncludeFound;

        private static readonly Regex IncludeRegex = new Regex(@"#include\s+""([^""]+)""", RegexOptions.Compiled);
        private static readonly Regex VertexDirectiveRegex = new Regex(@"#vertex", RegexOptions.Compiled);
        private static readonly Regex FragmentDirectiveRegex = new Regex(@"#fragment", RegexOptions.Compiled);
        private static readonly Regex AttributeRegex = new Regex(@"\[([a-zA-Z]+)\s*:\s*([a-zA-Z0-9_]+)\]", RegexOptions.Compiled);

        private static readonly HashSet<string> GlslKeywords = new HashSet<string>
        {
            "attribute", "const", "uniform", "varying",
            "layout", "centroid", "flat", "smooth", "noperspective",
            "break", "continue", "do", "for", "while", "if", "else", "switch", "case", "default",
            "in", "out", "inout",
            "float", "int", "void", "bool", "true", "false",
            "invariant", "discard", "return",
            "mat2", "mat3", "mat4", "mat2x2", "mat2x3", "mat2x4",
            "mat3x2", "mat3x3", "mat3x4", "mat4x2", "mat4x3", "mat4x4",
            "vec2", "vec3", "vec4", "ivec2", "ivec3", "ivec4", "bvec2", "bvec3", "bvec4",
            "uint", "uvec2", "uvec3", "uvec4", "sampler1D", "sampler2D", "sampler3D",
            "samplerCube", "sampler1DShadow", "sampler2DShadow", "samplerCubeShadow",
            "sampler1DArray", "sampler2DArray", "sampler1DArrayShadow", "sampler2DArrayShadow"
        };

        private string _currentFilePath;
        private string _baseDirectory;
        private HashSet<string> _processedIncludes = new HashSet<string>();

        public void Analyze(string content, string filePath)
        {
            _currentFilePath = filePath;
            _baseDirectory = Path.GetDirectoryName(filePath);
            _processedIncludes.Clear();

            string[] lines = content.Split('\n');

            bool inVertexSection = false;
            bool inFragmentSection = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                int lineNumber = i + 1;

                CheckForIncludes(line, lineNumber);

                if (VertexDirectiveRegex.IsMatch(line))
                {
                    inVertexSection = true;
                    inFragmentSection = false;
                    continue;
                }

                if (FragmentDirectiveRegex.IsMatch(line))
                {
                    inVertexSection = false;
                    inFragmentSection = true;
                    continue;
                }

                if (Path.GetExtension(_currentFilePath)?.ToLowerInvariant() == ".rs")
                {
                    CheckForAttributes(line, lineNumber);
                }

                CheckSyntax(line, lineNumber, inVertexSection, inFragmentSection);
            }

            if (!content.Contains("#vertex") && !content.Contains("#fragment"))
            {
                OnErrorFound(new GlslErrorEventArgs
                {
                    Line = 1,
                    Column = 1,
                    Length = 10,
                    Message = "Отсутствует директива #vertex или #fragment"
                });
            }
        }

        private void CheckForIncludes(string line, int lineNumber)
        {
            var match = IncludeRegex.Match(line);
            if (match.Success)
            {
                string includePath = match.Groups[1].Value;
                string fullPath = Path.GetFullPath(Path.Combine(_baseDirectory, includePath));

                if (_processedIncludes.Contains(fullPath))
                {
                    OnErrorFound(new GlslErrorEventArgs
                    {
                        Line = lineNumber,
                        Column = match.Index,
                        Length = match.Length,
                        Message = $"Циклическое включение файла: {includePath}"
                    });
                    return;
                }

                _processedIncludes.Add(fullPath);

                OnIncludeFound(new GlslIncludeEventArgs
                {
                    Line = lineNumber,
                    Column = match.Index,
                    Length = match.Length,
                    IncludePath = includePath,
                    FullPath = fullPath
                });

                if (!File.Exists(fullPath))
                {
                    OnErrorFound(new GlslErrorEventArgs
                    {
                        Line = lineNumber,
                        Column = match.Index,
                        Length = match.Length,
                        Message = $"Включаемый файл не найден: {includePath}"
                    });
                }
                else
                {
                    try
                    {
                        string includeContent = File.ReadAllText(fullPath);
                        string oldCurrentPath = _currentFilePath;
                        _currentFilePath = fullPath;

                        Analyze(includeContent, fullPath);

                        _currentFilePath = oldCurrentPath;
                    }
                    catch (Exception ex)
                    {
                        OnErrorFound(new GlslErrorEventArgs
                        {
                            Line = lineNumber,
                            Column = match.Index,
                            Length = match.Length,
                            Message = $"Ошибка при анализе включаемого файла: {ex.Message}"
                        });
                    }
                }
            }
        }

        private void CheckForAttributes(string line, int lineNumber)
        {
            var matches = AttributeRegex.Matches(line);
            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    string attributeName = match.Groups[1].Value;
                    string attributeValue = match.Groups[2].Value;

                    if (string.IsNullOrEmpty(attributeName) || string.IsNullOrEmpty(attributeValue))
                    {
                        OnErrorFound(new GlslErrorEventArgs
                        {
                            Line = lineNumber,
                            Column = match.Index,
                            Length = match.Length,
                            Message = "Неверный формат атрибута: " + match.Value
                        });
                    }
                }
            }
        }

        private void CheckSyntax(string line, int lineNumber, bool inVertexSection, bool inFragmentSection)
        {
            int openBraces = 0;
            int openBrackets = 0;
            int openParens = 0;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                switch (c)
                {
                    case '{': openBraces++; break;
                    case '}': openBraces--; break;
                    case '[': openBrackets++; break;
                    case ']': openBrackets--; break;
                    case '(': openParens++; break;
                    case ')': openParens--; break;
                }

                if (openBraces < 0)
                {
                    OnErrorFound(new GlslErrorEventArgs
                    {
                        Line = lineNumber,
                        Column = i + 1,
                        Length = 1,
                        Message = "Непарная закрывающая фигурная скобка"
                    });
                    openBraces = 0;
                }

                if (openBrackets < 0)
                {
                    OnErrorFound(new GlslErrorEventArgs
                    {
                        Line = lineNumber,
                        Column = i + 1,
                        Length = 1,
                        Message = "Непарная закрывающая квадратная скобка"
                    });
                    openBrackets = 0;
                }

                if (openParens < 0)
                {
                    OnErrorFound(new GlslErrorEventArgs
                    {
                        Line = lineNumber,
                        Column = i + 1,
                        Length = 1,
                        Message = "Непарная закрывающая круглая скобка"
                    });
                    openParens = 0;
                }
            }
        }

        protected virtual void OnErrorFound(GlslErrorEventArgs e)
        {
            ErrorFound?.Invoke(this, e);
        }

        protected virtual void OnIncludeFound(GlslIncludeEventArgs e)
        {
            IncludeFound?.Invoke(this, e);
        }
    }

    public class GlslErrorEventArgs : EventArgs
    {
        public int Line { get; set; }
        public int Column { get; set; }
        public int Length { get; set; }
        public string Message { get; set; }
    }

    public class GlslIncludeEventArgs : EventArgs
    {
        public int Line { get; set; }
        public int Column { get; set; }
        public int Length { get; set; }
        public string IncludePath { get; set; }
        public string FullPath { get; set; }
    }




    public class GlslFoldingStrategy
    {
        public void UpdateFoldings(FoldingManager foldingManager, TextDocument document)
        {
            if (foldingManager == null) throw new ArgumentNullException(nameof(foldingManager));
            if (document == null) throw new ArgumentNullException(nameof(document));

            List<NewFolding> newFoldings = CreateFoldings(document);
            newFoldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
            foldingManager.UpdateFoldings(newFoldings, -1);
        }

        private List<NewFolding> CreateFoldings(TextDocument document)
        {
            List<NewFolding> newFoldings = new List<NewFolding>();

            Stack<int> braceStack = new Stack<int>();

            var vertexDirectiveRegex = new Regex(@"#vertex", RegexOptions.Compiled);
            var fragmentDirectiveRegex = new Regex(@"#fragment", RegexOptions.Compiled);
            var includeDirectiveRegex = new Regex(@"#include\s+""([^""]+)""", RegexOptions.Compiled);

            string text = document.Text;

            AddDirectiveFoldings(document, text, newFoldings, vertexDirectiveRegex, fragmentDirectiveRegex);

            AddIncludeFoldings(document, text, newFoldings, includeDirectiveRegex);

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (c == '{')
                {
                    braceStack.Push(i);
                }
                else if (c == '}' && braceStack.Count > 0)
                {
                    int startOffset = braceStack.Pop();

                    int lineStart = document.GetLineByOffset(startOffset).Offset;
                    string startText = text.Substring(lineStart, startOffset - lineStart).Trim();

                    string name = GetFoldingName(startText);

                    newFoldings.Add(new NewFolding(startOffset, i + 1)
                    {
                        Name = name,
                        DefaultClosed = false
                    });
                }
            }

            return newFoldings;
        }

        private void AddDirectiveFoldings(TextDocument document, string text, List<NewFolding> newFoldings,
            Regex vertexDirectiveRegex, Regex fragmentDirectiveRegex)
        {
            foreach (Match vertexMatch in vertexDirectiveRegex.Matches(text))
            {
                int vertexOffset = vertexMatch.Index;

                Match fragmentMatch = fragmentDirectiveRegex.Match(text, vertexOffset + vertexMatch.Length);
                if (fragmentMatch.Success)
                {
                    newFoldings.Add(new NewFolding(vertexOffset, fragmentMatch.Index)
                    {
                        Name = "Vertex section",
                        DefaultClosed = false
                    });

                    newFoldings.Add(new NewFolding(fragmentMatch.Index, text.Length)
                    {
                        Name = "Fragment section",
                        DefaultClosed = false
                    });
                }
                else
                {
                    newFoldings.Add(new NewFolding(vertexOffset, text.Length)
                    {
                        Name = "Vertex section",
                        DefaultClosed = false
                    });
                }
            }

            if (!vertexDirectiveRegex.IsMatch(text))
            {
                Match fragmentMatch = fragmentDirectiveRegex.Match(text);
                if (fragmentMatch.Success)
                {
                    newFoldings.Add(new NewFolding(fragmentMatch.Index, text.Length)
                    {
                        Name = "Fragment section",
                        DefaultClosed = false
                    });
                }
            }
        }

        private void AddIncludeFoldings(TextDocument document, string text, List<NewFolding> newFoldings,
            Regex includeDirectiveRegex)
        {
            foreach (Match includeMatch in includeDirectiveRegex.Matches(text))
            {
                string includePath = includeMatch.Groups[1].Value;
                int includeLine = document.GetLineByOffset(includeMatch.Index).LineNumber;

                DocumentLine nextLine = document.GetLineByNumber(includeLine + 1);
                if (nextLine != null && document.GetText(nextLine).Contains("// Начало"))
                {
                    int endLineNumber = includeLine + 2;
                    while (endLineNumber <= document.LineCount)
                    {
                        DocumentLine line = document.GetLineByNumber(endLineNumber);
                        string lineText = document.GetText(line);

                        if (lineText.Contains("// Конец"))
                        {
                            int startOffset = document.GetLineByNumber(includeLine + 1).Offset;
                            int endOffset = line.EndOffset;

                            newFoldings.Add(new NewFolding(startOffset, endOffset)
                            {
                                Name = $"// {includePath} //",
                                DefaultClosed = true
                            });

                            break;
                        }

                        endLineNumber++;
                    }
                }
            }
        }

        private string GetFoldingName(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "{ ... }";

            if (text.Length > 40)
                text = text.Substring(0, 37) + "...";

            if (text.Contains("void") || text.Contains("float") || text.Contains("int") ||
                text.Contains("vec") || text.Contains("mat") || text.Contains("bool"))
            {
                return $"{text.Trim()} {{ ... }}";
            }
            else if (text.Contains("struct"))
            {
                return $"{text.Trim()} {{ ... }}";
            }
            else if (text.Contains("if") || text.Contains("else") ||
                     text.Contains("for") || text.Contains("while"))
            {
                return $"{text.Trim()} {{ ... }}";
            }

            return $"{text.Trim()} {{ ... }}";
        }
    }





    public class TextMarkerService : DocumentColorizingTransformer, IBackgroundRenderer
    {
        private readonly TextDocument _document;
        private readonly List<TextMarker> _markers = new List<TextMarker>();

        public TextMarkerService(TextDocument document)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            _document.Changed += DocumentChanged;
        }

        public void Dispose()
        {
            _document.Changed -= DocumentChanged;
        }

        public TextMarker Create(int startOffset, int length)
        {
            if (startOffset < 0 || startOffset >= _document.TextLength)
                throw new ArgumentOutOfRangeException(nameof(startOffset));
            if (length < 0 || startOffset + length > _document.TextLength)
                throw new ArgumentOutOfRangeException(nameof(length));

            var marker = new TextMarker(this, startOffset, length);
            _markers.Add(marker);
            Redraw(marker);
            return marker;
        }

        public void Remove(TextMarker marker)
        {
            if (marker == null || !_markers.Contains(marker))
                return;

            _markers.Remove(marker);
            Redraw(marker);
        }

        public void RemoveAll()
        {
            TextMarker[] oldMarkers = _markers.ToArray();
            _markers.Clear();
            foreach (TextMarker marker in oldMarkers)
            {
                Redraw(marker);
            }
        }

        public IEnumerable<TextMarker> GetMarkersAtOffset(int offset)
        {
            return _markers.Where(m => m.StartOffset <= offset && offset <= (m.StartOffset + m.Length));
        }

        private void DocumentChanged(object sender, DocumentChangeEventArgs e)
        {
            foreach (TextMarker marker in _markers.ToList())
            {
                if (marker.StartOffset >= e.Offset + e.RemovalLength)
                {
                    // Изменение произошло перед маркером -> сдвигаем маркер
                    int newStartOffset = marker.StartOffset - e.RemovalLength + e.InsertionLength;
                    _markers.Remove(marker);
                    _markers.Add(new TextMarker(this, newStartOffset, marker.Length)
                    {
                        BackgroundColor = marker.BackgroundColor,
                        ForegroundColor = marker.ForegroundColor,
                        FontStyle = marker.FontStyle,
                        FontWeight = marker.FontWeight,
                        BorderPen = marker.BorderPen,
                        BorderThickness = marker.BorderThickness,
                        CornerRadius = marker.CornerRadius,
                        Tag = marker.Tag,
                        ToolTip = marker.ToolTip,
                        MarkerType = marker.MarkerType,
                        MarkerColor = marker.MarkerColor
                    });
                }
                else if (marker.StartOffset + marker.Length <= e.Offset)
                {
                    // Изменение произошло после маркера -> ничего не делаем
                }
                else if (marker.StartOffset <= e.Offset && marker.StartOffset + marker.Length >= e.Offset + e.RemovalLength)
                {
                    // Изменение произошло внутри маркера -> изменяем длину маркера
                    int newLength = marker.Length - e.RemovalLength + e.InsertionLength;
                    _markers.Remove(marker);
                    _markers.Add(new TextMarker(this, marker.StartOffset, newLength)
                    {
                        BackgroundColor = marker.BackgroundColor,
                        ForegroundColor = marker.ForegroundColor,
                        FontStyle = marker.FontStyle,
                        FontWeight = marker.FontWeight,
                        BorderPen = marker.BorderPen,
                        BorderThickness = marker.BorderThickness,
                        CornerRadius = marker.CornerRadius,
                        Tag = marker.Tag,
                        ToolTip = marker.ToolTip,
                        MarkerType = marker.MarkerType,
                        MarkerColor = marker.MarkerColor
                    });
                }
                else
                {
                    // Изменение частично затрагивает маркер -> удаляем маркер
                    _markers.Remove(marker);
                }
            }
        }

        private void Redraw(TextMarker marker)
        {
            RedrawRequested?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler RedrawRequested;

        #region IBackgroundRenderer implementation

        public KnownLayer Layer => KnownLayer.Background;

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (textView == null)
                throw new ArgumentNullException(nameof(textView));
            if (drawingContext == null)
                throw new ArgumentNullException(nameof(drawingContext));

            if (_markers.Count == 0)
                return;

            var visualLines = textView.VisualLines;
            if (visualLines.Count == 0)
                return;

            var viewStart = visualLines.First().FirstDocumentLine.Offset;
            var viewEnd = visualLines.Last().LastDocumentLine.EndOffset;

            foreach (var marker in _markers.Where(m => (m.StartOffset + m.Length) >= viewStart && m.StartOffset <= viewEnd))
            {
                if (marker.BackgroundColor != null)
                {
                    var geoBuilder = new BackgroundGeometryBuilder();
                    geoBuilder.AlignToWholePixels = true;

                    // В новой версии AvaloniaEdit метод AddSegment принимает только 2 аргумента,
                    // а не 3 (textView, startOffset, endOffset)
                    geoBuilder.AddSegment(textView, new TextSegment
                    {
                        StartOffset = marker.StartOffset,
                        Length = marker.Length
                    });

                    var geometry = geoBuilder.CreateGeometry();
                    if (geometry != null)
                    {
                        drawingContext.DrawGeometry(marker.BackgroundColor, marker.BorderPen, geometry);
                    }
                }
            }
        }

        #endregion

        #region DocumentColorizingTransformer implementation

        protected override void ColorizeLine(DocumentLine line)
        {
            if (_markers.Count == 0)
                return;

            int lineStart = line.Offset;
            int lineEnd = line.EndOffset;

            foreach (var marker in _markers.Where(m => (m.StartOffset + m.Length) >= lineStart && m.StartOffset <= lineEnd))
            {
                ChangeLinePart(
                    Math.Max(marker.StartOffset, lineStart),
                    Math.Min(marker.StartOffset + marker.Length, lineEnd),
                    element =>
                    {
                        // В новой версии AvaloniaEdit свойства TextRunProperties только для чтения,
                        // поэтому мы должны создать новый экземпляр Typeface и присвоить его
                        if (marker.ForegroundColor != null)
                        {
                            // Вместо прямого присваивания используем другой подход
                            // element.TextRunProperties.ForegroundBrush = marker.ForegroundColor;

                            // Альтернативный подход (если применимо)
                            ApplyForegroundColor(element, marker.ForegroundColor);
                        }

                        // Аналогично с Typeface
                        if (marker.FontStyle != null || marker.FontWeight != null)
                        {
                            ApplyTextStyle(element, marker.FontStyle, marker.FontWeight);
                        }
                    });
            }
        }

        // Метод для применения цвета переднего плана
        private void ApplyForegroundColor(VisualLineElement element, IBrush brush)
        {
            // Данный метод следует адаптировать под вашу версию AvaloniaEdit
            // В некоторых версиях могут быть другие способы изменения свойств элемента

            // Пример:
            // element.Properties.Set(TextElementForegroundBrushProperty, brush);

            // В новой реализации может потребоваться создание нового TextRunProperties
            // с нужным цветом
        }

        // Метод для применения стиля текста
        private void ApplyTextStyle(VisualLineElement element, FontStyle? style, FontWeight? weight)
        {
            // Данный метод следует адаптировать под вашу версию AvaloniaEdit
            // В некоторых версиях могут быть другие способы изменения свойств элемента

            // Пример:
            // var typeface = element.TextRunProperties.Typeface;
            // var newTypeface = new Typeface(
            //     typeface.FontFamily,
            //     style ?? typeface.Style,
            //     weight ?? typeface.Weight,
            //     typeface.Stretch
            // );
            // element.Properties.Set(TextElementTypefaceProperty, newTypeface);
        }

        #endregion
    }

    public class TextMarker
    {
        private readonly TextMarkerService _service;
        private readonly int _startOffset;
        private readonly int _length;

        public TextMarker(TextMarkerService service, int startOffset, int length)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _startOffset = startOffset;
            _length = length;
        }

        public int StartOffset => _startOffset;
        public int Length => _length;

        public IBrush BackgroundColor { get; set; }
        public IBrush ForegroundColor { get; set; }
        public FontStyle? FontStyle { get; set; }
        public FontWeight? FontWeight { get; set; }
        public IPen BorderPen { get; set; }
        public double BorderThickness { get; set; }
        public double CornerRadius { get; set; }

        public object Tag { get; set; }
        public string ToolTip { get; set; }

        public TextMarkerType MarkerType { get; set; }
        public Color MarkerColor { get; set; }

        public void Delete()
        {
            _service.Remove(this);
        }

        public void Redraw()
        {
            switch (MarkerType)
            {
                case TextMarkerType.SquigglyUnderline:
                    SetSquigglyUnderline(MarkerColor);
                    break;
                case TextMarkerType.DottedUnderline:
                    SetDottedUnderline(MarkerColor);
                    break;
                case TextMarkerType.SolidUnderline:
                    SetSolidUnderline(MarkerColor);
                    break;
                case TextMarkerType.Highlight:
                    BackgroundColor = new SolidColorBrush(MarkerColor);
                    break;
            }
        }

        private void SetSquigglyUnderline(Color color)
        {
            BackgroundColor = null;
            BorderPen = new Pen(new SolidColorBrush(color), 1);
            BorderThickness = 1;
            CornerRadius = 0;
        }

        private void SetDottedUnderline(Color color)
        {
            BackgroundColor = null;
            BorderPen = new Pen(new SolidColorBrush(color), 1);
            BorderThickness = 1;
            CornerRadius = 0;
        }

        private void SetSolidUnderline(Color color)
        {
            BackgroundColor = null;
            BorderPen = new Pen(new SolidColorBrush(color), 1);
            BorderThickness = 1;
            CornerRadius = 0;
        }
    }

    public enum TextMarkerType
    {
        None,
        SquigglyUnderline,
        DottedUnderline,
        SolidUnderline,
        Highlight
    }

}
