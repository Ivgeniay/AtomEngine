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
using AvaloniaEdit.Folding;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using System.Reflection;
using System.Xml;
using AtomEngine;
using Avalonia.Input;
using AvaloniaEdit.Editing;
using TextMateSharp.Grammars;
using AvaloniaEdit.Indentation;
using AvaloniaEdit.TextMate;

namespace Editor
{
    public class GlslEditorController : Grid, IWindowed, ICacheble
    {
        public event Action<string>? OnFileChange;
        public event Action? OnPrepareFileChange;

        private string _currentFilePath;
        private ComboBox _syntaxModeCombo;
        private TextEditor _textEditor;
        private GlslAnalyzer _glslAnalyzer;
        private Button _saveButton;
        private StackPanel _toolbarPanel;
        private TextMarkerService _textMarkerService;
        private GlslIncludeManager _includeManager;
        private GlslEditorStateProvider _stateProvider;
        private List<TextMarker> _errorMarkers = new List<TextMarker>();
        private ReadOnlySections _readOnlySections;
        private ReadOnlySectionEditingBehavior _readOnlyBehavior;

        // Флаг для отслеживания внутренних изменений документа
        private bool _internalChanging = false;

        public Action<object> OnClose { get; set; }

        private RegistryOptions _registryOptions;
        private int _currentTheme = (int)ThemeName.DarkPlus;
        private readonly TextMate.Installation _textMateInstallation;

        public GlslEditorController()
        {
            InitializeUI();

            _textEditor.HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Visible;
            _textEditor.Background = Brushes.Transparent;
            _textEditor.ShowLineNumbers = true;
            _textEditor.TextArea.Background = this.Background;
            //_textEditor.TextArea.TextEntered += textEditor_TextArea_TextEntered;
            //_textEditor.TextArea.TextEntering += textEditor_TextArea_TextEntering;
            _textEditor.Options.AllowToggleOverstrikeMode = true;
            _textEditor.Options.EnableTextDragDrop = true;
            _textEditor.Options.ShowBoxForControlCharacters = true;
            _textEditor.Options.ColumnRulerPositions = new List<int>() { 80, 100 };
            _textEditor.TextArea.IndentationStrategy = new GlslFoldingStrategy();
            _textEditor.TextArea.Caret.PositionChanged += Caret_PositionChanged;
            _textEditor.TextArea.RightClickMovesCaret = true;
            _textEditor.Options.HighlightCurrentLine = true;
            _textEditor.TextArea.TextView.LineTransformers.Add(new UnderlineAndStrikeThroughTransformer());


            _registryOptions = new RegistryOptions((ThemeName)_currentTheme);
            _textMateInstallation = _textEditor.InstallTextMate(_registryOptions);
            Language csharpLanguage = _registryOptions.GetLanguageByExtension(".c");
            string scopeName = _registryOptions.GetScopeByLanguageId(csharpLanguage.Id);
            _textMateInstallation.SetGrammar(_registryOptions.GetScopeByLanguageId(csharpLanguage.Id));
            _textMateInstallation.AppliedTheme += TextMateInstallationOnAppliedTheme;

            this.AddHandler(PointerWheelChangedEvent, (o, i) =>
            {
                if (i.KeyModifiers != KeyModifiers.Control) return;
                if (i.Delta.Y > 0) _textEditor.FontSize++;
                else _textEditor.FontSize = _textEditor.FontSize > 1 ? _textEditor.FontSize - 1 : 1;
            }, RoutingStrategies.Bubble, true);


            _stateProvider = new GlslEditorStateProvider();

            _readOnlySections = new ReadOnlySections();
            _readOnlyBehavior = new ReadOnlySectionEditingBehavior(_textEditor, offset => _readOnlySections.IsReadOnly(offset));

            _textMarkerService = new TextMarkerService(_textEditor.Document);
            _textEditor.TextArea.TextView.BackgroundRenderers.Add(_textMarkerService);
            _textEditor.TextArea.TextView.LineTransformers.Add(_textMarkerService);

            _includeManager = new GlslIncludeManager(_textEditor, _textMarkerService, _readOnlySections, _stateProvider);
            _includeManager.OnIncludeFileOpen += OnIncludeFileOpen;
            _includeManager.OnDocumentChanging += (isChanging) => _internalChanging = isChanging;

            _glslAnalyzer = new GlslAnalyzer();
            _glslAnalyzer.ErrorFound += OnErrorFound;
            _glslAnalyzer.IncludeFound += OnIncludeFound;

            OnPrepareFileChange += () =>
            {
                _includeManager.Reset();
            };

            _textEditor.TextChanged += OnTextChanged;
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

            Grid.SetRow(_textEditor, 1);
            Children.Add(_textEditor);
        }

        private void SetupSyntaxHighlighting()
        {
            try
            {
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
                        _textEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");
                    }
                }
            }
            catch (Exception ex)
            {
                Status.SetStatus($"Ошибка при установке подсветки синтаксиса: {ex.Message}");
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
                OnPrepareFileChange?.Invoke();

                _currentFilePath = filePath;
                string content = File.ReadAllText(filePath);

                // Создаем состояние загрузки файла
                var loadingState = new GlslFileLoadingState();
                _stateProvider.RegisterState(loadingState);

                // Устанавливаем флаг внутреннего изменения
                _internalChanging = true;
                try
                {
                    _textEditor.Document.Text = content;

                    _textEditor.InvalidateVisual();
                    _textEditor.TextArea.TextView.InvalidateVisual();

                    _textEditor.CaretOffset = 0;

                    // Устанавливаем текущий файл в менеджере включений
                    _includeManager.SetCurrentFile(_currentFilePath);

                    // Анализируем содержимое файла
                    _glslAnalyzer.Analyze(content, filePath);

                    OnFileChange?.Invoke(_currentFilePath);

                    Status.SetStatus($"Файл открыт: {Path.GetFileName(filePath)}");
                }
                finally
                {
                    _internalChanging = false;
                    _stateProvider.RemoveState<GlslFileLoadingState>();
                }
            }
            catch (Exception ex)
            {
                _internalChanging = false;
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
                DebLogger.Debug($"Save {_currentFilePath}");

                // Получаем текст без включенных файлов для сохранения
                string textToSave = _includeManager.GetTextWithoutIncludes();

                File.WriteAllText(_currentFilePath, textToSave);
                Status.SetStatus($"Файл сохранен: {Path.GetFileName(_currentFilePath)}");
            }
            catch (Exception ex)
            {
                Status.SetStatus($"Ошибка при сохранении файла: {ex.Message}");
            }
        }

        private void OnTextChanged(object sender, EventArgs e)
        {
            // Если изменение внутреннее или идет загрузка файла, игнорируем
            if (_internalChanging || _stateProvider.HasState<GlslFileLoadingState>())
                return;

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (!string.IsNullOrEmpty(_currentFilePath))
                {
                    ClearErrorMarkers();
                    _glslAnalyzer.Analyze(_textEditor.Document.Text, _currentFilePath);
                }
            }, DispatcherPriority.Background);
        }

        private void OnErrorFound(object? sender, GlslErrorEventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    var line = _textEditor.Document.GetLineByNumber(e.Line);
                    int column = Math.Min(e.Column, line.Length + 1);
                    int startOffset = line.Offset + column - 1;
                    int length = e.Length > 0 ? e.Length : 1;

                    // Создаем маркер ошибки используя TextMarker
                    var errorMarker = _textMarkerService.Create(startOffset, length);
                    errorMarker.MarkerType = TextMarkerType.SquigglyUnderline;
                    errorMarker.MarkerColor = Color.Parse("#FF2222");
                    errorMarker.ToolTip = e.Message;
                    errorMarker.Redraw();

                    _errorMarkers.Add(errorMarker);
                }
                catch (Exception ex)
                {
                    Status.SetStatus($"Ошибка при создании маркера: {ex.Message}");
                }
            });
        }

        private void OnIncludeFound(object? sender, GlslIncludeEventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    // Делегируем обработку директивы include менеджеру включений
                    _includeManager.ProcessIncludeDirective(e.Line, e.IncludePath, e.StartOffset, e.Length);
                }
                catch (Exception ex)
                {
                    Status.SetStatus($"Ошибка при обработке включения: {ex.Message}");
                }
            });
        }

        private void OnIncludeFileOpen(string sourceFile, string includeFile)
        {
            // Открываем файл в текущем или новом экземпляре редактора
            // в зависимости от архитектуры приложения

            // Для простоты просто открываем файл в текущем редакторе
            OpenFile(includeFile);

            // Если нужно открыть в новом окне, здесь можно вызвать соответствующий метод
            // EditorManager.OpenFile(includeFile);
        }

        private void ClearErrorMarkers()
        {
            foreach (var marker in _errorMarkers)
            {
                marker.Delete();
            }
            _errorMarkers.Clear();
        }

        private void RemoveUnderlineAndStrikethroughTransformer()
        {
            for (int i = _textEditor.TextArea.TextView.LineTransformers.Count - 1; i >= 0; i--)
            {
                if (_textEditor.TextArea.TextView.LineTransformers[i] is UnderlineAndStrikeThroughTransformer)
                {
                    _textEditor.TextArea.TextView.LineTransformers.RemoveAt(i);
                }
            }
        }

        private void TextMateInstallationOnAppliedTheme(object sender, TextMate.Installation e)
        {
            ApplyThemeColorsToEditor(e);
            ApplyThemeColorsToWindow(e);
        }

        void ApplyThemeColorsToEditor(TextMate.Installation e)
        {
            ApplyBrushAction(e, "editor.background", brush => _textEditor.Background = brush);
            ApplyBrushAction(e, "editor.foreground", brush => _textEditor.Foreground = brush);

            if (!ApplyBrushAction(e, "editor.selectionBackground",
                    brush => _textEditor.TextArea.SelectionBrush = brush))
            {
                //if (Application.Current!.TryGetResource("TextAreaSelectionBrush", out var resourceObject))
                //{
                //    if (resourceObject is IBrush brush)
                //    {
                //        _textEditor.TextArea.SelectionBrush = brush;
                //    }
                //}
            }

            if (!ApplyBrushAction(e, "editor.lineHighlightBackground",
                    brush =>
                    {
                        _textEditor.TextArea.TextView.CurrentLineBackground = brush;
                        _textEditor.TextArea.TextView.CurrentLineBorder = new Pen(brush); // Todo: VS Code didn't seem to have a border but it might be nice to have that option. For now just make it the same..
                    }))
            {
                _textEditor.TextArea.TextView.SetDefaultHighlightLineColors();
            }

            //Todo: looks like the margin doesn't have a active line highlight, would be a nice addition
            if (!ApplyBrushAction(e, "editorLineNumber.foreground",
                    brush => _textEditor.LineNumbersForeground = brush))
            {
                _textEditor.LineNumbersForeground = _textEditor.Foreground;
            }
        }

        private void ApplyThemeColorsToWindow(TextMate.Installation e)
        {
            var panel = this.Find<StackPanel>("StatusBar");
            if (panel == null)
            {
                return;
            }

            ApplyBrushAction(e, "editor.background", brush => Background = brush);
            //ApplyBrushAction(e, "editor.foreground", brush => Foreground = brush);
        }

        bool ApplyBrushAction(TextMate.Installation e, string colorKeyNameFromJson, Action<Avalonia.Media.IBrush> applyColorAction)
        {
            if (!e.TryGetThemeColor(colorKeyNameFromJson, out var colorString))
                return false;

            if (!Color.TryParse(colorString, out Color color))
                return false;

            var colorBrush = new SolidColorBrush(color);
            applyColorAction(colorBrush);
            return true;
        }

        private void Caret_PositionChanged(object? sender, EventArgs e)
        {
            Status.SetStatus(string.Format("Line {0} Column {1}",
                _textEditor.TextArea.Caret.Line,
                _textEditor.TextArea.Caret.Column));
        }

        public void Open()
        {
        }

        public void Close()
        {
            OnClose?.Invoke(this);
        }

        public void Dispose()
        {
            _includeManager.Dispose();
            _textEditor.TextChanged -= OnTextChanged;
            _glslAnalyzer.ErrorFound -= OnErrorFound;
            _glslAnalyzer.IncludeFound -= OnIncludeFound;
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
                _internalChanging = true;
                try
                {
                    _currentFilePath = null;
                    _textEditor.Document.Text = string.Empty;
                    ClearErrorMarkers();
                    _includeManager.Reset();
                }
                finally
                {
                    _internalChanging = false;
                }
            }));
        }
    }

    public class IncludeFileInfo
    {
        public int DirectiveLine { get; set; }
        public string FilePath { get; set; }
        public string RelativePath { get; set; }
        public string Content { get; set; }
        public int DirectiveOffset { get; set; }
        public int DirectiveLength { get; set; }
        public int ContentSectionStart { get; set; }
        public int ContentSectionEnd { get; set; }
        public bool HasError { get; set; }
    }

    public class SimpleTextMarker
    {
        public int Line { get; set; }
        public int Column { get; set; }
        public int StartOffset { get; set; }
        public int Length { get; set; }
        public string Message { get; set; }
        public IBrush BackgroundColor { get; set; }
        public TextMarkerType MarkerType { get; set; } = TextMarkerType.None;
    }



    public class ReadOnlySectionEditingBehavior : IReadOnlySectionProvider
    {
        private readonly TextEditor _editor;
        private readonly Func<int, bool> _isReadOnlyFunc;

        public ReadOnlySectionEditingBehavior(TextEditor editor, Func<int, bool> isReadOnlyFunc)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            _isReadOnlyFunc = isReadOnlyFunc ?? throw new ArgumentNullException(nameof(isReadOnlyFunc));

            _editor.TextArea.ReadOnlySectionProvider = this;
            _editor.TextArea.TextEntering += TextArea_TextEntering;
        }

        private void TextArea_TextEntering(object sender, TextInputEventArgs e)
        {
            int offset = _editor.CaretOffset;
            if (_isReadOnlyFunc(offset))
            {
                e.Handled = true; 
            }
        }

        public bool CanInsert(int offset)
        {
            return !_isReadOnlyFunc(offset);
        }

        public bool IsReadOnly(int offset)
        {
            return _isReadOnlyFunc(offset);
        }

        public void Detach()
        {
            _editor.TextArea.ReadOnlySectionProvider = null;
            _editor.TextArea.TextEntering -= TextArea_TextEntering;
        }

        public IEnumerable<ISegment> GetDeletableSegments(ISegment segment)
        {
            int startOffset = segment.Offset;
            int endOffset = segment.EndOffset;

            if (!_isReadOnlyFunc(startOffset) && !_isReadOnlyFunc(endOffset - 1))
            {
                bool hasReadOnlyParts = false;
                for (int i = startOffset; i < endOffset; i++)
                {
                    if (_isReadOnlyFunc(i))
                    {
                        hasReadOnlyParts = true;
                        break;
                    }
                }

                if (!hasReadOnlyParts)
                {
                    yield return segment;
                    yield break;
                }
            }

            int currentStart = -1;
            for (int i = startOffset; i < endOffset; i++)
            {
                if (!_isReadOnlyFunc(i))
                {
                    if (currentStart == -1)
                        currentStart = i;
                }
                else
                {
                    if (currentStart != -1)
                    {
                        yield return new TextSegment { StartOffset = currentStart, EndOffset = i };
                        currentStart = -1;
                    }
                }
            }

            if (currentStart != -1)
            {
                yield return new TextSegment { StartOffset = currentStart, EndOffset = endOffset };
            }
        }
    }
    public class ReadOnlySections
    {
        public List<(int Start, int End)> Sections = new List<(int Start, int End)>();

        public void AddSection(int startOffset, int endOffset)
        {
            Sections.Add((startOffset, endOffset));
        }

        public void RemoveSection(int startOffset, int endOffset)
        {
            for (int i = Sections.Count - 1; i >= 0; i--)
            {
                if (Sections[i].Start == startOffset && Sections[i].End == endOffset)
                {
                    Sections.RemoveAt(i);
                    return;
                }
            }

            for (int i = Sections.Count - 1; i >= 0; i--)
            {
                if (DoSectionsOverlap(Sections[i].Start, Sections[i].End, startOffset, endOffset))
                {
                    Sections.RemoveAt(i);
                }
            }
        }

        private bool DoSectionsOverlap(int start1, int end1, int start2, int end2)
        {
            return (start1 <= end2 && start2 <= end1);
        }

        public void ClearSections()
        {
            Sections.Clear();
        }

        public bool IsReadOnly(int offset)
        {
            foreach (var section in Sections)
            {
                if (offset >= section.Start && offset < section.End)
                {
                    return true;
                }
            }
            return false;
        }

        public IEnumerable<(int Start, int End)> GetSections()
        {
            return Sections;
        }
    }




    public class GlslIncludeManager
    {
        private static readonly Regex IncludeRegex = new Regex(@"#include\s+""([^""]+)""", RegexOptions.Compiled);

        private readonly TextEditor _textEditor;
        private readonly TextMarkerService _textMarkerService;
        private readonly ReadOnlySections _readOnlySections;
        private readonly GlslEditorStateProvider _stateProvider;
        private readonly List<IncludeFileInfo> _includedFiles = new List<IncludeFileInfo>();
        private readonly List<TextMarker> _includeMarkers = new List<TextMarker>();
        private readonly HashSet<int> _dirtyLines = new HashSet<int>();
        private DispatcherTimer _changesProcessingTimer;

        private string _currentFilePath;
        private bool _internalChanging = false;

        public event Action<bool> OnDocumentChanging;
        public event Action<string, string> OnIncludeFileOpen;

        public GlslIncludeManager(TextEditor textEditor, TextMarkerService textMarkerService,
            ReadOnlySections readOnlySections, GlslEditorStateProvider stateProvider)
        {
            _textEditor = textEditor ?? throw new ArgumentNullException(nameof(textEditor));
            _textMarkerService = textMarkerService ?? throw new ArgumentNullException(nameof(textMarkerService));
            _readOnlySections = readOnlySections ?? throw new ArgumentNullException(nameof(readOnlySections));
            _stateProvider = stateProvider ?? throw new ArgumentNullException(nameof(stateProvider));

            _textEditor.Document.Changed += Document_Changed;
        }

        private void SetInternalChanging(bool value)
        {
            _internalChanging = value;
            OnDocumentChanging?.Invoke(value);
        }

        private void Document_Changed(object sender, DocumentChangeEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentFilePath) || _internalChanging)
                return;

            // Если изменения производятся в рамках обработки директив include, игнорируем их
            if (_stateProvider.HasState<GlslIncludeProcessingState>())
            {
                var state = _stateProvider.GetState<GlslIncludeProcessingState>();

                // Проверяем, затрагивает ли изменение обрабатываемые строки
                int startLine = _textEditor.Document.GetLineByOffset(e.Offset).LineNumber;
                int endLine = _textEditor.Document.GetLineByOffset(
                    Math.Min(e.Offset + Math.Max(e.InsertionLength, 1), _textEditor.Document.TextLength - 1)
                ).LineNumber;

                bool affectsProcessingLines = false;
                for (int line = startLine; line <= endLine; line++)
                {
                    if (state.ProcessingLines.Contains(line))
                    {
                        affectsProcessingLines = true;
                        break;
                    }
                }

                // Если изменение затрагивает обрабатываемые строки, игнорируем
                if (affectsProcessingLines)
                    return;
            }

            // Определяем затронутые строки и помечаем "грязными" те, которые содержат директивы include
            MarkDirtyLines(e);

            // Если есть "грязные" строки, запускаем отложенную обработку
            if (_dirtyLines.Count > 0)
            {
                ResetAndStartProcessingTimer();
            }
        }

        private void RemoveIncludedContent(IncludeFileInfo include)
        {
            // Проверяем, что у нас есть информация о начале и конце включенного содержимого
            if (include.ContentSectionStart > 0 && include.ContentSectionEnd > 0)
            {
                try
                {
                    // Получаем строки, соответствующие включенному содержимому
                    var startLine = _textEditor.Document.GetLineByOffset(include.ContentSectionStart);
                    var endLine = _textEditor.Document.GetLineByOffset(include.ContentSectionEnd);

                    // Удаляем нередактируемую секцию
                    _readOnlySections.RemoveSection(include.ContentSectionStart, include.ContentSectionEnd);

                    // Находим и удаляем соответствующие маркеры
                    for (int i = _includeMarkers.Count - 1; i >= 0; i--)
                    {
                        var marker = _includeMarkers[i];
                        if (marker.StartOffset >= include.ContentSectionStart &&
                            marker.StartOffset < include.ContentSectionEnd)
                        {
                            marker.Delete();
                            _includeMarkers.RemoveAt(i);
                        }
                    }

                    // Устанавливаем флаг внутреннего изменения перед изменением документа
                    SetInternalChanging(true);
                    try
                    {
                        // Удаляем текст из документа
                        int removeLength = include.ContentSectionEnd - include.ContentSectionStart;
                        if (removeLength > 0)
                        {
                            _textEditor.Document.Remove(include.ContentSectionStart, removeLength);
                        }
                    }
                    finally
                    {
                        // Сбрасываем флаг внутреннего изменения
                        SetInternalChanging(false);
                    }
                }
                catch (Exception ex)
                {
                    Status.SetStatus($"Ошибка при удалении включенного содержимого: {ex.Message}");
                    SetInternalChanging(false); // на всякий случай сбрасываем флаг при ошибке
                }
            }
        }
        private const string FILE_NOT_FOUND = "File not found: ";
        private void InsertFileNotFoundMessage(int line, string path)
        {
            try
            {
                var document = _textEditor.Document;
                var lineObj = document.GetLineByNumber(line);

                int insertPosition = lineObj.EndOffset;
                string errorMessage = $"\n{FILE_NOT_FOUND}" + path;

                // Проверяем следующую строку
                if (line < document.LineCount)
                {
                    var currentLine = document.GetLineByNumber(line);
                    string currentLineText = document.GetText(currentLine.Offset, currentLine.Length);

                    var nextLine = document.GetLineByNumber(line + 1);
                    string nextLineText = document.GetText(nextLine.Offset, nextLine.Length);

                    DebLogger.Debug($"Текущая строка: '{currentLine}', Длина: {currentLine.Length}");
                    DebLogger.Debug($"Следующая строка: '{nextLineText}', Длина: {nextLineText.Length}");

                    if (nextLineText.TrimStart().StartsWith($"{FILE_NOT_FOUND}"))
                    {
                        SetInternalChanging(true);
                        try
                        {
                            document.Replace(nextLine.Offset, nextLine.Length, $"{FILE_NOT_FOUND}" + path);
                        }
                        finally
                        {
                            SetInternalChanging(false);
                        }

                        // Обновляем информацию в списке
                        var includeInfo = _includedFiles.FirstOrDefault(f => f.DirectiveLine == line);
                        if (includeInfo != null)
                        {
                            includeInfo.ContentSectionStart = nextLine.Offset;
                            includeInfo.ContentSectionEnd = nextLine.EndOffset;
                        }

                        // Обновляем нередактируемую секцию
                        _readOnlySections.RemoveSection(nextLine.Offset, nextLine.EndOffset);
                        _readOnlySections.AddSection(nextLine.Offset, nextLine.EndOffset);

                        // Обновляем маркер
                        foreach (var marker_ in _includeMarkers.ToList())
                        {
                            if (marker_.StartOffset == nextLine.Offset)
                            {
                                marker_.Delete();
                                _includeMarkers.Remove(marker_);
                                break;
                            }
                        }

                        var newMarker = _textMarkerService.Create(nextLine.Offset, nextLine.Length);
                        newMarker.BackgroundColor = new SolidColorBrush(Color.Parse("#3A2222"));
                        newMarker.MarkerType = TextMarkerType.Highlight;
                        //newMarker.ToolTip = "Включаемый файл не найден";
                        newMarker.Redraw();

                        _includeMarkers.Add(newMarker);

                        return;
                    }
                }

                // Вставляем новое сообщение
                SetInternalChanging(true);
                try
                {
                    document.Insert(insertPosition, errorMessage);
                }
                finally
                {
                    SetInternalChanging(false);
                }

                DocumentLine errorLine = document.GetLineByNumber(line + 1);
                int startOffset = errorLine.Offset;
                int endOffset = errorLine.EndOffset;

                // Создаем маркер для выделения сообщения об ошибке
                var marker = _textMarkerService.Create(startOffset, endOffset - startOffset);
                marker.BackgroundColor = new SolidColorBrush(Color.Parse("#3A2222"));
                marker.MarkerType = TextMarkerType.Highlight;
                marker.ToolTip = "Включаемый файл не найден";
                marker.Redraw();

                _includeMarkers.Add(marker);

                // Добавляем нередактируемую секцию
                _readOnlySections.AddSection(startOffset, endOffset);

                // Обновляем информацию о секции в списке включенных файлов
                var includeInfoNew = _includedFiles.FirstOrDefault(f => f.DirectiveLine == line);
                if (includeInfoNew != null)
                {
                    includeInfoNew.ContentSectionStart = startOffset;
                    includeInfoNew.ContentSectionEnd = endOffset;
                }
            }
            catch (Exception ex)
            {
                Status.SetStatus($"Ошибка при вставке сообщения о ненайденном файле: {ex.Message}");
                SetInternalChanging(false); // на всякий случай сбрасываем флаг при ошибке
            }
        }

        private void InsertIncludeContent(int line, string filePath, string relativePath, string content)
        {
            try
            {
                var document = _textEditor.Document;

                // Получаем строку с директивой include
                var lineObj = document.GetLineByNumber(line);
                int insertPosition = lineObj.EndOffset;
                string includedContent = $"\n{content}";

                // Проверяем следующую строку
                if (line < document.LineCount)
                {
                    var nextLine = document.GetLineByNumber(line + 1);
                    string nextLineText = document.GetText(nextLine);

                    // Если следующая строка содержит сообщение об ошибке или включенный контент
                    if (nextLineText.TrimStart().StartsWith("// Файл не найден:") ||
                        IsLinePartOfIncludedContent(line + 1))
                    {
                        // Находим все строки, которые нужно заменить
                        int existingStartLine = line + 1;
                        int existingEndLine = FindEndOfIncludedContent(existingStartLine);

                        // Удаляем старый контент
                        int existingStartOffset = document.GetLineByNumber(existingStartLine).Offset;
                        int existingEndOffset = document.GetLineByNumber(existingEndLine).EndOffset;

                        // Удаляем нередактируемые секции для этих строк
                        _readOnlySections.RemoveSection(existingStartOffset, existingEndOffset);

                        // Удаляем соответствующие маркеры
                        for (int i = _includeMarkers.Count - 1; i >= 0; i--)
                        {
                            var existingMarker = _includeMarkers[i];
                            if (existingMarker.StartOffset >= existingStartOffset && existingMarker.StartOffset < existingEndOffset)
                            {
                                existingMarker.Delete();
                                _includeMarkers.RemoveAt(i);
                            }
                        }

                        // Устанавливаем флаг внутреннего изменения перед изменениями документа
                        SetInternalChanging(true);
                        try
                        {
                            // Удаляем текст из документа
                            document.Remove(existingStartOffset, existingEndOffset - existingStartOffset);

                            // Вставляем новый контент
                            document.Insert(existingStartOffset, content);
                        }
                        finally
                        {
                            SetInternalChanging(false);
                        }

                        // Обновляем информацию о секции
                        int newContentLines = content.Split('\n').Length;
                        int newEndLine_ = existingStartLine + newContentLines - 1;
                        int newEndOffset = document.GetLineByNumber(newEndLine_).EndOffset;

                        // Добавляем новую нередактируемую секцию
                        _readOnlySections.AddSection(existingStartOffset, newEndOffset);

                        // Создаем маркер для выделения включенного содержимого
                        var newMarker = _textMarkerService.Create(existingStartOffset, newEndOffset - existingStartOffset);
                        newMarker.BackgroundColor = new SolidColorBrush(Color.Parse("#1C2834"));
                        newMarker.MarkerType = TextMarkerType.Highlight;
                        newMarker.ToolTip = $"Включенный файл: {Path.GetFileName(filePath)}";
                        newMarker.Redraw();

                        _includeMarkers.Add(newMarker);

                        // Обновляем информацию в списке включенных файлов
                        var existingIncludeInfo = _includedFiles.FirstOrDefault(f => f.DirectiveLine == line);
                        if (existingIncludeInfo != null)
                        {
                            existingIncludeInfo.ContentSectionStart = existingStartOffset;
                            existingIncludeInfo.ContentSectionEnd = newEndOffset;
                        }

                        return;
                    }
                }

                // Если это новое включение, просто вставляем контент
                SetInternalChanging(true);
                try
                {
                    document.Insert(insertPosition, includedContent);
                }
                finally
                {
                    SetInternalChanging(false);
                }

                DocumentLine newStartLine = document.GetLineByNumber(line + 1);
                int insertedContentLines = content.Split('\n').Length;
                DocumentLine newEndLine = document.GetLineByNumber(line + insertedContentLines);

                int insertedStartOffset = newStartLine.Offset;
                int insertedEndOffset = newEndLine.EndOffset;

                // Добавляем нередактируемую секцию
                _readOnlySections.AddSection(insertedStartOffset, insertedEndOffset);

                // Создаем маркер для выделения включенного содержимого
                var insertedMarker = _textMarkerService.Create(insertedStartOffset, insertedEndOffset - insertedStartOffset);
                insertedMarker.BackgroundColor = new SolidColorBrush(Color.Parse("#1C2834"));
                insertedMarker.MarkerType = TextMarkerType.Highlight;
                insertedMarker.ToolTip = $"Включенный файл: {Path.GetFileName(filePath)}";
                insertedMarker.Redraw();

                _includeMarkers.Add(insertedMarker);

                // Обновляем информацию о секции контента в списке включенных файлов
                var newIncludeInfo = _includedFiles.FirstOrDefault(f => f.DirectiveLine == line);
                if (newIncludeInfo != null)
                {
                    newIncludeInfo.ContentSectionStart = insertedStartOffset;
                    newIncludeInfo.ContentSectionEnd = insertedEndOffset;
                }
            }
            catch (Exception ex)
            {
                Status.SetStatus($"Ошибка при вставке включаемого содержимого: {ex.Message}");
                SetInternalChanging(false); // на всякий случай сбрасываем флаг при ошибке
            }
        }

        public void SetCurrentFile(string filePath)
        {
            _currentFilePath = filePath;
            Reset();
        }

        public void Reset()
        {
            SetInternalChanging(true);
            try
            {
                ClearAllIncludes();
                _dirtyLines.Clear();

                StopProcessingTimer();
            }
            finally
            {
                SetInternalChanging(false);
            }
        }

        private void MarkDirtyLines(DocumentChangeEventArgs e)
        {
            try
            {
                // Определяем начальную и конечную строки изменения
                int startLine = _textEditor.Document.GetLineByOffset(e.Offset).LineNumber;
                int endLine = _textEditor.Document.GetLineByOffset(
                    Math.Min(e.Offset + Math.Max(e.InsertionLength, 1), _textEditor.Document.TextLength - 1)
                ).LineNumber;

                // Проверяем каждую строку в диапазоне изменений
                for (int line = startLine; line <= endLine; line++)
                {
                    string lineText = _textEditor.Document.GetText(_textEditor.Document.GetLineByNumber(line));

                    // Если строка содержит директиву include, помечаем ее как "грязную"
                    if (IncludeRegex.IsMatch(lineText))
                    {
                        _dirtyLines.Add(line);
                    }
                }

                // Также проверяем строки с существующими директивами include
                foreach (var include in _includedFiles.ToList())
                {
                    // Если строка с директивой include попадает в диапазон или рядом с ним,
                    // помечаем ее как "грязную"
                    if (include.DirectiveLine >= startLine - 1 && include.DirectiveLine <= endLine + 1)
                    {
                        _dirtyLines.Add(include.DirectiveLine);
                    }
                }
            }
            catch (Exception ex)
            {
                Status.SetStatus($"Ошибка при анализе изменений: {ex.Message}");
            }
        }

        private void ResetAndStartProcessingTimer()
        {
            // Останавливаем таймер, если он запущен
            StopProcessingTimer();

            // Создаем и запускаем новый таймер
            _changesProcessingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _changesProcessingTimer.Tick += ProcessDelayedChanges;
            _changesProcessingTimer.Start();
        }

        private void StopProcessingTimer()
        {
            if (_changesProcessingTimer != null && _changesProcessingTimer.IsEnabled)
            {
                _changesProcessingTimer.Stop();
                _changesProcessingTimer.Tick -= ProcessDelayedChanges;
            }
        }

        private void ProcessDelayedChanges(object sender, EventArgs e)
        {
            // Останавливаем таймер
            StopProcessingTimer();

            // Если нет "грязных" строк, выходим
            if (_dirtyLines.Count == 0)
                return;

            try
            {
                // Обрабатываем каждую "грязную" строку
                foreach (int line in _dirtyLines.ToList())
                {
                    // Если строка содержит директиву include, обрабатываем ее
                    if (line <= _textEditor.Document.LineCount)
                    {
                        var lineObj = _textEditor.Document.GetLineByNumber(line);
                        string lineText = _textEditor.Document.GetText(lineObj);

                        var match = IncludeRegex.Match(lineText);
                        if (match.Success)
                        {
                            string includePath = match.Groups[1].Value;
                            int offset = lineObj.Offset + match.Index;
                            int length = match.Length;

                            // Обрабатываем директиву include
                            ProcessIncludeDirectiveWithState(line, includePath, offset, length);
                        }
                        else
                        {
                            // Если строка больше не содержит директиву include, удаляем связанное содержимое
                            var existingInclude = _includedFiles.FirstOrDefault(f => f.DirectiveLine == line);
                            if (existingInclude != null)
                            {
                                ProcessIncludeRemoval(existingInclude, line);
                            }
                        }
                    }
                }
            }
            finally
            {
                // Очищаем список "грязных" строк
                _dirtyLines.Clear();
            }
        }

        private void ProcessIncludeRemoval(IncludeFileInfo includeInfo, int line)
        {
            // Создаем состояние обработки для этой строки
            var processingState = new GlslIncludeProcessingState();
            processingState.ProcessingLines.Add(line);

            // Добавляем все строки включенного содержимого
            if (includeInfo.ContentSectionStart > 0 && includeInfo.ContentSectionEnd > 0)
            {
                var startLine = _textEditor.Document.GetLineByOffset(includeInfo.ContentSectionStart).LineNumber;
                var endLine = _textEditor.Document.GetLineByOffset(includeInfo.ContentSectionEnd).LineNumber;

                for (int i = startLine; i <= endLine; i++)
                {
                    processingState.ProcessingLines.Add(i);
                }
            }

            // Регистрируем состояние
            _stateProvider.RegisterState(processingState);

            try
            {
                // Удаляем включенное содержимое
                RemoveIncludedContent(includeInfo);
                _includedFiles.Remove(includeInfo);
            }
            finally
            {
                // Удаляем состояние обработки
                _stateProvider.RemoveState<GlslIncludeProcessingState>();
            }
        }

        private bool IsLinePartOfIncludedContent(int lineNumber)
        {
            // Проверяем, является ли строка частью включенного содержимого
            var line = _textEditor.Document.GetLineByNumber(lineNumber);

            foreach (var include in _includedFiles)
            {
                if (include.ContentSectionStart <= line.Offset && include.ContentSectionEnd >= line.EndOffset)
                {
                    return true;
                }
            }

            return false;
        }

        private int FindEndOfIncludedContent(int startLineNumber)
        {
            // Находим последнюю строку включенного содержимого
            foreach (var include in _includedFiles)
            {
                var startLine = _textEditor.Document.GetLineByOffset(include.ContentSectionStart);
                if (startLine.LineNumber == startLineNumber)
                {
                    var endLine = _textEditor.Document.GetLineByOffset(include.ContentSectionEnd);
                    return endLine.LineNumber;
                }
            }

            return startLineNumber;
        }

        private string GetAbsoluteIncludePath(string relativePath)
        {
            // Обработка относительных путей
            if (string.IsNullOrEmpty(_currentFilePath))
                return relativePath;

            string baseDirectory = Path.GetDirectoryName(_currentFilePath);
            return Path.GetFullPath(Path.Combine(baseDirectory, relativePath));
        }

        private void AddOpenIncludeButton(int line, string filePath)
        {
            // В будущем можно реализовать визуальную кнопку для открытия файла
            // Сейчас просто добавим маркер с подсказкой
            try
            {
                var lineObj = _textEditor.Document.GetLineByNumber(line);
                var text = _textEditor.Document.GetText(lineObj);

                // Находим директиву #include
                var match = IncludeRegex.Match(text);
                if (match.Success)
                {
                    int offset = lineObj.Offset + match.Index + match.Length;
                    int length = 1;

                    // Добавляем маркер с подсказкой
                    var marker = _textMarkerService.Create(offset, length);
                    marker.Tag = filePath;
                    marker.ToolTip = $"→ Открыть файл: {Path.GetFileName(filePath)}";
                    marker.BackgroundColor = new SolidColorBrush(Color.Parse("#3465A4"));
                    marker.MarkerType = TextMarkerType.None;
                    marker.Redraw();

                    _includeMarkers.Add(marker);

                    // Добавляем обработчик клика по маркеру
                    _textEditor.TextArea.PointerPressed += (s, e) => {
                        var position = e.GetPosition(_textEditor.TextArea);
                        var pos = _textEditor.GetPositionFromPoint(position);
                        if (pos.HasValue)
                        {
                            int clickOffset = _textEditor.Document.GetOffset(pos.Value.Line, pos.Value.Column);
                            if (clickOffset >= offset && clickOffset <= offset + 1)
                            {
                                OpenIncludeFile(filePath);
                                e.Handled = true;
                            }
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                Status.SetStatus($"Ошибка при добавлении кнопки открытия файла: {ex.Message}");
            }
        }

        private void OpenIncludeFile(string filePath)
        {
            // Проверяем, существует ли файл
            if (!File.Exists(filePath))
            {
                Status.SetStatus($"Файл не существует: {filePath}");
                return;
            }

            // Вызываем событие для открытия файла
            OnIncludeFileOpen?.Invoke(_currentFilePath, filePath);
        }

        public void ProcessIncludeDirective(int line, string relativePath, int startOffset, int length)
        {
            // Делегируем обработку методу с управлением состоянием
            ProcessIncludeDirectiveWithState(line, relativePath, startOffset, length);
        }

        private void ProcessIncludeDirectiveWithState(int line, string relativePath, int startOffset, int length)
        {
            // Создаем состояние обработки для этой строки
            var processingState = new GlslIncludeProcessingState();
            processingState.ProcessingLines.Add(line);

            // Также добавляем следующую строку, где будет содержимое
            if (line < _textEditor.Document.LineCount)
            {
                processingState.ProcessingLines.Add(line + 1);
            }

            // Регистрируем состояние
            _stateProvider.RegisterState(processingState);

            try
            {
                // Получаем полный путь к включаемому файлу
                string includeFilePath = GetAbsoluteIncludePath(relativePath);

                // Проверяем, существует ли файл
                if (File.Exists(includeFilePath))
                {
                    string includeContent = File.ReadAllText(includeFilePath);

                    // Проверяем, есть ли уже этот включенный файл в списке
                    var existingInclude = _includedFiles.FirstOrDefault(
                        f => f.DirectiveLine == line && f.FilePath == includeFilePath);

                    if (existingInclude != null)
                    {
                        // Обновляем содержимое, если оно изменилось
                        if (existingInclude.Content != includeContent)
                        {
                            // Удаляем старый контент и вставляем новый
                            RemoveIncludedContent(existingInclude);
                            InsertIncludeContent(line, includeFilePath, relativePath, includeContent);

                            // Обновляем информацию в списке
                            existingInclude.Content = includeContent;
                            existingInclude.HasError = false;
                        }
                    }
                    else
                    {
                        // Добавляем новый включенный файл в список
                        var includeInfo = new IncludeFileInfo
                        {
                            DirectiveLine = line,
                            FilePath = includeFilePath,
                            RelativePath = relativePath,
                            Content = includeContent,
                            DirectiveOffset = startOffset,
                            DirectiveLength = length,
                            HasError = false
                        };
                        _includedFiles.Add(includeInfo);

                        // Вставляем содержимое в документ
                        InsertIncludeContent(line, includeFilePath, relativePath, includeContent);
                    }

                    // Добавляем кнопку для открытия файла
                    AddOpenIncludeButton(line, includeFilePath);
                }
                else
                {
                    // Файл не найден - показываем сообщение об ошибке
                    var existingInclude = _includedFiles.FirstOrDefault(f => f.DirectiveLine == line);
                    if (existingInclude != null)
                    {
                        // Если уже был включенный файл - удаляем его содержимое
                        RemoveIncludedContent(existingInclude);

                        // Обновляем информацию
                        existingInclude.FilePath = includeFilePath;
                        existingInclude.RelativePath = relativePath;
                        existingInclude.Content = null;
                        existingInclude.HasError = true;
                    }
                    else
                    {
                        // Добавляем новую запись с ошибкой
                        var includeInfo = new IncludeFileInfo
                        {
                            DirectiveLine = line,
                            FilePath = includeFilePath,
                            RelativePath = relativePath,
                            Content = null,
                            DirectiveOffset = startOffset,
                            DirectiveLength = length,
                            HasError = true
                        };
                        _includedFiles.Add(includeInfo);
                    }

                    // Вставляем сообщение об ошибке
                    InsertFileNotFoundMessage(line, relativePath);
                }
            }
            catch (Exception ex)
            {
                Status.SetStatus($"Ошибка при обработке директивы include: {ex.Message}");
            }
            finally
            {
                // Удаляем состояние обработки
                _stateProvider.RemoveState<GlslIncludeProcessingState>();
            }
        }

        public void ClearAllIncludes()
        {
            // Устанавливаем флаг внутреннего изменения
            SetInternalChanging(true);
            try
            {
                // Удаляем все включенные содержимые и маркеры
                foreach (var include in _includedFiles.ToList())
                {
                    RemoveIncludedContent(include);
                }

                _includedFiles.Clear();

                // Удаляем все оставшиеся маркеры
                foreach (var marker in _includeMarkers.ToList())
                {
                    marker.Delete();
                }

                _includeMarkers.Clear();
            }
            finally
            {
                SetInternalChanging(false);
            }
        }

        public string GetTextWithoutIncludes()
        {
            // Возвращает текст документа без включенных содержимых файлов
            string fullText = _textEditor.Document.Text;

            // Если нет включенных файлов, возвращаем весь текст
            if (_includedFiles.Count == 0)
                return fullText;

            // Создаем новый объект StringBuilder для формирования результата
            var result = new System.Text.StringBuilder();

            int currentPos = 0;

            // Сортируем включенные файлы по позиции их директивы в документе
            var sortedIncludes = _includedFiles
                .Where(f => f.ContentSectionStart > 0 && f.ContentSectionEnd > 0)
                .OrderBy(f => f.DirectiveLine)
                .ToList();

            foreach (var include in sortedIncludes)
            {
                // Находим строку с директивой include
                DocumentLine directiveLine = _textEditor.Document.GetLineByNumber(include.DirectiveLine);

                // Добавляем текст до директивы включения (включая саму директиву)
                result.Append(fullText.Substring(currentPos, directiveLine.EndOffset - currentPos + 1));

                // Переходим после включенного контента
                currentPos = include.ContentSectionEnd;
            }

            // Добавляем оставшийся текст после последнего включения
            if (currentPos < fullText.Length)
            {
                result.Append(fullText.Substring(currentPos));
            }

            return result.ToString();
        }

        public void Dispose()
        {
            StopProcessingTimer();
            _textEditor.Document.Changed -= Document_Changed;
            ClearAllIncludes();
        }


    }


    public class UnderlineAndStrikeThroughTransformer : DocumentColorizingTransformer
    {
        protected override void ColorizeLine(DocumentLine line)
        {
            if (line.LineNumber == 2)
            {
                string lineText = this.CurrentContext.Document.GetText(line);

                int indexOfUnderline = lineText.IndexOf("underline");
                int indexOfStrikeThrough = lineText.IndexOf("strikethrough");

                if (indexOfUnderline != -1)
                {
                    ChangeLinePart(
                        line.Offset + indexOfUnderline,
                        line.Offset + indexOfUnderline + "underline".Length,
                        visualLine =>
                        {
                            if (visualLine.TextRunProperties.TextDecorations != null)
                            {
                                var textDecorations = new TextDecorationCollection(visualLine.TextRunProperties.TextDecorations) { TextDecorations.Underline[0] };

                                visualLine.TextRunProperties.SetTextDecorations(textDecorations);
                            }
                            else
                            {
                                visualLine.TextRunProperties.SetTextDecorations(TextDecorations.Underline);
                            }
                        }
                    );
                }

                if (indexOfStrikeThrough != -1)
                {
                    ChangeLinePart(
                        line.Offset + indexOfStrikeThrough,
                        line.Offset + indexOfStrikeThrough + "strikethrough".Length,
                        visualLine =>
                        {
                            if (visualLine.TextRunProperties.TextDecorations != null)
                            {
                                var textDecorations = new TextDecorationCollection(visualLine.TextRunProperties.TextDecorations) { TextDecorations.Strikethrough[0] };

                                visualLine.TextRunProperties.SetTextDecorations(textDecorations);
                            }
                            else
                            {
                                visualLine.TextRunProperties.SetTextDecorations(TextDecorations.Strikethrough);
                            }
                        }
                    );
                }
            }
        }
    }



    public interface IGlslEditorDocumentState
    {
        string Description { get; } // Для диагностики
    }

    public class GlslIncludeProcessingState : IGlslEditorDocumentState
    {
        public string Description => "Обработка директивы include";
        public HashSet<int> ProcessingLines { get; } = new HashSet<int>();
    }

    public class GlslFileLoadingState : IGlslEditorDocumentState
    {
        public string Description => "Загрузка файла";
    }

    public class GlslEditorStateProvider
    {
        private readonly Dictionary<Type, IGlslEditorDocumentState> _states = new Dictionary<Type, IGlslEditorDocumentState>();

        public void RegisterState<T>(T state) where T : IGlslEditorDocumentState
        {
            _states[typeof(T)] = state;
        }

        public T GetState<T>() where T : IGlslEditorDocumentState
        {
            if (_states.TryGetValue(typeof(T), out var state))
            {
                return (T)state;
            }
            return default;
        }

        public bool HasState<T>() where T : IGlslEditorDocumentState
        {
            return _states.ContainsKey(typeof(T));
        }

        public void RemoveState<T>() where T : IGlslEditorDocumentState
        {
            _states.Remove(typeof(T));
        }
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

        private string _currentFileContent;
        private string _currentFilePath;
        private string _baseDirectory;
        private HashSet<string> _processedIncludes = new HashSet<string>();

        public void Analyze(string content, string filePath)
        {
            _currentFilePath = filePath;
            _baseDirectory = Path.GetDirectoryName(filePath);
            _processedIncludes.Clear();
            _currentFileContent = content;

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

                // Вычисляем смещение и длину директивы
                var lineObj = new TextSegment
                {
                    StartOffset = GetLineOffset(lineNumber),
                    Length = line.Length
                };
                int startOffset = lineObj.StartOffset + match.Index;
                int length = match.Length;

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
                    FullPath = fullPath,
                    StartOffset = startOffset,
                    EndOffset = startOffset + length
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

        private int GetLineOffset(int lineNumber)
        {
            // Учитываем, что линии в документе нумеруются с 1
            int offset = 0;
            string[] lines = _currentFileContent.Split('\n');

            for (int i = 0; i < Math.Min(lineNumber - 1, lines.Length); i++)
            {
                offset += lines[i].Length + 1; // +1 для символа новой строки
            }

            return offset;
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
        public string IncludePath { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;

        public int StartOffset { get; set; }
        public int EndOffset { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals(obj as GlslIncludeEventArgs);
        }

        public bool Equals(GlslIncludeEventArgs? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return IncludePath == other.IncludePath &&
                   FullPath == other.FullPath;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + IncludePath.GetHashCode();
                hash = hash * 23 + FullPath.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(GlslIncludeEventArgs? left, GlslIncludeEventArgs? right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(GlslIncludeEventArgs? left, GlslIncludeEventArgs? right) =>
            !(left == right);
    }











    public class GlslFoldingStrategy : DefaultIndentationStrategy
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

        private void DocumentChanged(object? sender, DocumentChangeEventArgs e)
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
