using Avalonia.Collections;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.VisualTree;
using System.Collections.ObjectModel;

namespace Editor
{
    internal class NodeGraphController : ContentControl, IWindowed
    {
        public Action<object> OnClose { get; set; }

        private Canvas _canvas;
        private Grid _gridContainer;
        private Border _toolbarBorder;
        private ScrollViewer _scrollViewer;
        private Border _contextMenuBorder;
        private ObservableCollection<NodeElement> _nodes;
        private ObservableCollection<NodeConnection> _connections;

        private Point _lastMousePosition;
        private Point _panStartPosition;
        private bool _isPanning;
        private double _zoom = 1.0;
        private const double MIN_ZOOM = 0.25;
        private const double MAX_ZOOM = 2.0;
        private const double ZOOM_SPEED = 0.1;

        private NodeElement _selectedNode;
        private NodePin _selectedPin;
        private NodeConnection _dragConnection;
        private Point _dragConnectionPoint;

        private bool _isOpen = false;

        // События для внешнего взаимодействия
        public event EventHandler<NodeElement> NodeSelected;
        public event EventHandler<NodeElement> NodeAdded;
        public event EventHandler<NodeElement> NodeRemoved;
        public event EventHandler<NodeConnection> ConnectionAdded;
        public event EventHandler<NodeConnection> ConnectionRemoved;

        public NodeGraphController()
        {
            _nodes = new ObservableCollection<NodeElement>();
            _connections = new ObservableCollection<NodeConnection>();

            InitializeUI();
        }

        private void InitializeUI()
        {
            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });

            // Создаем верхнюю панель инструментов
            _toolbarBorder = new Border
            {
                Height = 32,
                Classes = { "toolbarBackground" }
            };

            var toolbarPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                Margin = new Thickness(5)
            };

            var addNodeButton = new Button { Content = "Добавить ноду", Classes = { "toolButton" } };
            var centerViewButton = new Button { Content = "Центрировать", Classes = { "toolButton" } };
            var resetZoomButton = new Button { Content = "Сбросить масштаб", Classes = { "toolButton" } };

            addNodeButton.Click += (s, e) => ShowNodeAddMenu();
            centerViewButton.Click += (s, e) => CenterView();
            resetZoomButton.Click += (s, e) => ResetZoom();

            toolbarPanel.Children.Add(addNodeButton);
            toolbarPanel.Children.Add(centerViewButton);
            toolbarPanel.Children.Add(resetZoomButton);

            _toolbarBorder.Child = toolbarPanel;

            // Создаем контейнер для графа
            _gridContainer = new Grid
            {
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30))
            };

            _scrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                AllowAutoHide = false
            };

            _canvas = new Canvas
            {
                Background = new SolidColorBrush(Color.FromRgb(20, 20, 20)),
                Width = 3000,
                Height = 3000
            };

            _scrollViewer.Content = _canvas;
            _gridContainer.Children.Add(_scrollViewer);

            // Создаем контекстное меню (скрытое изначально)
            _contextMenuBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(37, 37, 38)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(63, 63, 70)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(2),
                IsVisible = false,
                ZIndex = 1000
            };

            // Добавляем все в главный грид
            Grid.SetRow(_toolbarBorder, 0);
            Grid.SetRow(_gridContainer, 1);

            mainGrid.Children.Add(_toolbarBorder);
            mainGrid.Children.Add(_gridContainer);
            mainGrid.Children.Add(_contextMenuBorder);

            // Настраиваем обработчики событий
            _canvas.PointerPressed += OnCanvasPointerPressed;
            _canvas.PointerMoved += OnCanvasPointerMoved;
            _canvas.PointerReleased += OnCanvasPointerReleased;
            _canvas.PointerWheelChanged += OnCanvasPointerWheelChanged;

            this.Content = mainGrid;
        }


        private void OnCanvasPointerPressed(object sender, PointerPressedEventArgs e)
        {
            _lastMousePosition = e.GetPosition(_canvas);

            var point = e.GetPosition(_canvas);
            var hitNode = HitTestNodes(point);

            if (e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
            {
                if (hitNode != null)
                {
                    // Проверка попадания в пин
                    var pin = HitTestNodePins(hitNode, point);
                    if (pin != null)
                    {
                        _selectedPin = pin;
                        _dragConnectionPoint = point;

                        // Если это выходной пин, начинаем создание соединения
                        if (pin.Direction == PinDirection.Output)
                        {
                            _dragConnection = new NodeConnection
                            {
                                StartPin = pin,
                                StartPosition = GetPinPosition(pin),
                                EndPosition = point
                            };
                            _canvas.Children.Add(_dragConnection);
                        }
                        // Если это входной пин, проверяем можно ли начать соединение от него
                        else if (pin.Direction == PinDirection.Input && pin.AllowStartConnection)
                        {
                            _dragConnection = new NodeConnection
                            {
                                EndPin = pin,
                                EndPosition = GetPinPosition(pin),
                                StartPosition = point
                            };
                            _canvas.Children.Add(_dragConnection);
                        }
                    }
                    else
                    {
                        // Выбор ноды
                        SelectNode(hitNode);
                        _selectedNode = hitNode;
                    }
                }
                else
                {
                    // Начинаем перемещение холста
                    _isPanning = true;
                    _panStartPosition = point;
                    this.Cursor = new Cursor(StandardCursorType.Hand);

                    // Снимаем выделение с ноды
                    UnselectAllNodes();
                }
            }
            else if (e.GetCurrentPoint(null).Properties.IsRightButtonPressed)
            {
                // Показываем контекстное меню
                if (hitNode != null)
                {
                    ShowNodeContextMenu(hitNode, point);
                }
                else
                {
                    ShowCanvasContextMenu(point);
                }
            }

            e.Handled = true;
        }

        private void OnCanvasPointerMoved(object sender, PointerEventArgs e)
        {
            var point = e.GetPosition(_canvas);

            if (_isPanning)
            {
                // Перемещение холста
                var deltaX = point.X - _panStartPosition.X;
                var deltaY = point.Y - _panStartPosition.Y;

                var scrollPosition = _scrollViewer.Offset;
                _scrollViewer.Offset = new Vector(scrollPosition.X - deltaX, scrollPosition.Y - deltaY);

                _panStartPosition = point;
            }
            else if (_selectedNode != null)
            {
                // Перемещение выбранной ноды
                var deltaX = point.X - _lastMousePosition.X;
                var deltaY = point.Y - _lastMousePosition.Y;

                MoveNode(_selectedNode, deltaX, deltaY);
                UpdateConnections();
            }
            else if (_dragConnection != null)
            {
                // Обновление позиции соединения при перетаскивании
                if (_dragConnection.StartPin != null)
                {
                    _dragConnection.EndPosition = point;
                }
                else if (_dragConnection.EndPin != null)
                {
                    _dragConnection.StartPosition = point;
                }

                _dragConnection.Update();
            }

            _lastMousePosition = point;
            e.Handled = true;
        }

        private void OnCanvasPointerReleased(object sender, PointerReleasedEventArgs e)
        {
            var point = e.GetPosition(_canvas);

            if (_isPanning)
            {
                _isPanning = false;
                this.Cursor = new Cursor(StandardCursorType.Arrow);
            }
            else if (_selectedNode != null)
            {
                _selectedNode = null;
            }
            else if (_dragConnection != null)
            {
                // Завершение создания соединения
                if (_dragConnection.StartPin != null)
                {
                    // Ищем входной пин под курсором
                    var hitNode = HitTestNodes(point);
                    if (hitNode != null)
                    {
                        var pin = HitTestNodePins(hitNode, point);
                        if (pin != null && pin.Direction == PinDirection.Input)
                        {
                            // Создаем соединение
                            if (CanConnect(_dragConnection.StartPin, pin))
                            {
                                CreateConnection(_dragConnection.StartPin, pin);
                            }
                        }
                    }
                }
                else if (_dragConnection.EndPin != null)
                {
                    // Ищем выходной пин под курсором
                    var hitNode = HitTestNodes(point);
                    if (hitNode != null)
                    {
                        var pin = HitTestNodePins(hitNode, point);
                        if (pin != null && pin.Direction == PinDirection.Output)
                        {
                            // Создаем соединение
                            if (CanConnect(pin, _dragConnection.EndPin))
                            {
                                CreateConnection(pin, _dragConnection.EndPin);
                            }
                        }
                    }
                }

                // Удаляем временное соединение
                _canvas.Children.Remove(_dragConnection);
                _dragConnection = null;
                _selectedPin = null;
            }

            e.Handled = true;
        }

        private void OnCanvasPointerWheelChanged(object sender, PointerWheelEventArgs e)
        {
            // Масштабирование
            var delta = e.Delta.Y > 0 ? ZOOM_SPEED : -ZOOM_SPEED;
            var newZoom = _zoom + delta;

            if (newZoom >= MIN_ZOOM && newZoom <= MAX_ZOOM)
            {
                _zoom = newZoom;

                var scaleTransform = new ScaleTransform(_zoom, _zoom);
                _canvas.RenderTransform = scaleTransform;

                // Обновляем размеры холста в зависимости от масштаба
                _canvas.Width = 3000 / _zoom;
                _canvas.Height = 3000 / _zoom;
            }

            e.Handled = true;
        }

      
        public NodeElement CreateNode(NodeType type, string title, Point position)
        {
            var node = new NodeElement
            {
                NodeType = type,
                Title = title,
                Position = position
            };

            // Добавляем ноду в коллекцию и на холст
            _nodes.Add(node);
            _canvas.Children.Add(node);

            // Устанавливаем позицию ноды на холсте
            Canvas.SetLeft(node, position.X);
            Canvas.SetTop(node, position.Y);

            // Настраиваем обработчики событий для ноды
            node.PointerPressed += OnNodePointerPressed;
            node.PointerMoved += OnNodePointerMoved;
            node.PointerReleased += OnNodePointerReleased;

            // Вызываем событие добавления ноды
            NodeAdded?.Invoke(this, node);

            return node;
        }

        public void RemoveNode(NodeElement node)
        {
            // Удаляем все соединения, связанные с этой нодой
            RemoveNodeConnections(node);

            // Удаляем ноду из коллекции и с холста
            _nodes.Remove(node);
            _canvas.Children.Remove(node);

            // Вызываем событие удаления ноды
            NodeRemoved?.Invoke(this, node);
        }

        public void MoveNode(NodeElement node, double deltaX, double deltaY)
        {
            var x = Canvas.GetLeft(node) + deltaX;
            var y = Canvas.GetTop(node) + deltaY;

            Canvas.SetLeft(node, x);
            Canvas.SetTop(node, y);

            node.Position = new Point(x, y);
        }

        public void SelectNode(NodeElement node)
        {
            // Снимаем выделение со всех нод
            UnselectAllNodes();

            // Выделяем указанную ноду
            node.IsSelected = true;

            // Вызываем событие выбора ноды
            NodeSelected?.Invoke(this, node);
        }

        public void UnselectAllNodes()
        {
            foreach (var node in _nodes)
            {
                node.IsSelected = false;
            }
        }

        public bool CanConnect(NodePin outputPin, NodePin inputPin)
        {
            // Проверяем направление пинов
            if (outputPin.Direction != PinDirection.Output || inputPin.Direction != PinDirection.Input)
                return false;

            // Проверяем типы данных пинов
            if (outputPin.DataType != inputPin.DataType && !inputPin.AcceptAnyType)
                return false;

            // Проверяем, что пины принадлежат разным нодам
            if (outputPin.ParentNode == inputPin.ParentNode)
                return false;

            // Проверяем, что у входного пина еще нет соединения, если он не поддерживает множественные соединения
            if (!inputPin.AllowMultipleConnections)
            {
                foreach (var connection in _connections)
                {
                    if (connection.EndPin == inputPin)
                        return false;
                }
            }

            return true;
        }

        public NodeConnection CreateConnection(NodePin outputPin, NodePin inputPin)
        {
            if (!CanConnect(outputPin, inputPin))
                return null;

            var connection = new NodeConnection
            {
                StartPin = outputPin,
                EndPin = inputPin,
                StartPosition = GetPinPosition(outputPin),
                EndPosition = GetPinPosition(inputPin)
            };

            // Добавляем соединение в коллекцию и на холст
            _connections.Add(connection);
            _canvas.Children.Add(connection);

            // Обновляем соединение
            connection.Update();

            // Вызываем событие добавления соединения
            ConnectionAdded?.Invoke(this, connection);

            return connection;
        }

        public void RemoveConnection(NodeConnection connection)
        {
            _connections.Remove(connection);
            _canvas.Children.Remove(connection);

            // Вызываем событие удаления соединения
            ConnectionRemoved?.Invoke(this, connection);
        }

        public void RemoveNodeConnections(NodeElement node)
        {
            var connectionsToRemove = new List<NodeConnection>();

            foreach (var connection in _connections)
            {
                if (connection.StartPin.ParentNode == node || connection.EndPin.ParentNode == node)
                {
                    connectionsToRemove.Add(connection);
                }
            }

            foreach (var connection in connectionsToRemove)
            {
                RemoveConnection(connection);
            }
        }

        public void UpdateConnections()
        {
            foreach (var connection in _connections)
            {
                connection.StartPosition = GetPinPosition(connection.StartPin);
                connection.EndPosition = GetPinPosition(connection.EndPin);
                connection.Update();
            }
        }

        public Point GetPinPosition(NodePin pin)
        {
            var nodePosition = pin.ParentNode.Position;
            var pinPosition = pin.Position;

            return new Point(
                nodePosition.X + pinPosition.X,
                nodePosition.Y + pinPosition.Y
            );
        }

        private NodeElement HitTestNodes(Point point)
        {
            // Проверяем в обратном порядке, чтобы выбирать верхнюю ноду
            for (int i = _nodes.Count - 1; i >= 0; i--)
            {
                var node = _nodes[i];
                var nodePosition = node.Position;
                var nodeRect = new Rect(nodePosition.X, nodePosition.Y, node.Width, node.Height);

                if (nodeRect.Contains(point))
                {
                    return node;
                }
            }

            return null;
        }

        private NodePin HitTestNodePins(NodeElement node, Point point)
        {
            foreach (var pin in node.InputPins.Concat(node.OutputPins))
            {
                var pinPosition = GetPinPosition(pin);
                var pinRect = new Rect(pinPosition.X - 5, pinPosition.Y - 5, 10, 10);

                if (pinRect.Contains(point))
                {
                    return pin;
                }
            }

            return null;
        }


        private void OnNodePointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (sender is NodeElement node)
            {
                SelectNode(node);
                _selectedNode = node;
                _lastMousePosition = e.GetPosition(_canvas);

                e.Handled = true;
            }
        }

        private void OnNodePointerMoved(object sender, PointerEventArgs e)
        {
            if (_selectedNode != null)
            {
                var point = e.GetPosition(_canvas);
                var deltaX = point.X - _lastMousePosition.X;
                var deltaY = point.Y - _lastMousePosition.Y;

                MoveNode(_selectedNode, deltaX, deltaY);
                UpdateConnections();

                _lastMousePosition = point;
                e.Handled = true;
            }
        }

        private void OnNodePointerReleased(object sender, PointerReleasedEventArgs e)
        {
            _selectedNode = null;
            e.Handled = true;
        }


        private void ShowNodeContextMenu(NodeElement node, Point position)
        {
            var menu = new StackPanel
            {
                Spacing = 2
            };

            var duplicateButton = new Button
            {
                Content = "Дублировать",
                Classes = { "menuItem" },
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left
            };

            var deleteButton = new Button
            {
                Content = "Удалить",
                Classes = { "menuItem" },
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left
            };

            duplicateButton.Click += (s, e) =>
            {
                DuplicateNode(node);
                CloseContextMenu();
            };

            deleteButton.Click += (s, e) =>
            {
                RemoveNode(node);
                CloseContextMenu();
            };

            menu.Children.Add(duplicateButton);
            menu.Children.Add(deleteButton);

            ShowContextMenu(menu, position);
        }

        private void ShowCanvasContextMenu(Point position)
        {
            var menu = new StackPanel
            {
                Spacing = 2
            };

            var addNodeButton = new Button
            {
                Content = "Добавить ноду",
                Classes = { "menuItem" },
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left
            };

            var centerViewButton = new Button
            {
                Content = "Центрировать вид",
                Classes = { "menuItem" },
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left
            };

            var resetZoomButton = new Button
            {
                Content = "Сбросить масштаб",
                Classes = { "menuItem" },
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left
            };

            addNodeButton.Click += (s, e) =>
            {
                CloseContextMenu();
                ShowNodeAddMenu(position);
            };

            centerViewButton.Click += (s, e) =>
            {
                CenterView();
                CloseContextMenu();
            };

            resetZoomButton.Click += (s, e) =>
            {
                ResetZoom();
                CloseContextMenu();
            };

            menu.Children.Add(addNodeButton);
            menu.Children.Add(centerViewButton);
            menu.Children.Add(resetZoomButton);

            ShowContextMenu(menu, position);
        }

        private void ShowNodeAddMenu(Point? position = null)
        {
            var menu = new StackPanel
            {
                Spacing = 2
            };

            var title = new TextBlock
            {
                Text = "Выберите тип ноды",
                Foreground = new SolidColorBrush(Colors.White),
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(5)
            };

            menu.Children.Add(title);

            // Добавляем кнопки для разных типов нод
            var nodeTypes = Enum.GetValues(typeof(NodeType)).Cast<NodeType>();

            foreach (var nodeType in nodeTypes)
            {
                var button = new Button
                {
                    Content = GetNodeTypeDisplayName(nodeType),
                    Classes = { "menuItem" },
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Tag = nodeType
                };

                button.Click += (s, e) =>
                {
                    var clickedButton = s as Button;
                    var selectedNodeType = (NodeType)clickedButton.Tag;

                    // Создаем ноду в указанной позиции или в центре видимой области
                    var nodePosition = position ?? GetCanvasCenterPosition();

                    var node = CreateNode(
                        selectedNodeType,
                        GetNodeTypeDisplayName(selectedNodeType),
                        nodePosition
                    );

                    // Добавляем пины в зависимости от типа ноды
                    AddPinsForNodeType(node, selectedNodeType);

                    CloseContextMenu();
                };

                menu.Children.Add(button);
            }

            var pos = position ?? new Point(_canvas.Width / 2, _canvas.Height / 2);
            ShowContextMenu(menu, pos);
        }

        private void ShowContextMenu(Control content, Point position)
        {
            // Очищаем старое содержимое
            _contextMenuBorder.Child = null;

            // Устанавливаем новое содержимое
            _contextMenuBorder.Child = content;

            // Устанавливаем позицию меню
            var pos = position;

            // Преобразуем координаты из координат холста в координаты родительского грида
            var canvasPos = _canvas.TranslatePoint(position, _gridContainer);

            if (canvasPos.HasValue)
            {
                pos = canvasPos.Value;
            }

            Canvas.SetLeft(_contextMenuBorder, pos.X);
            Canvas.SetTop(_contextMenuBorder, pos.Y);

            // Показываем меню
            _contextMenuBorder.IsVisible = true;

            // Добавляем обработчик для закрытия меню по клику вне его
            var root = this.GetVisualRoot() as Window;
            if (root != null)
            {
                root.PointerPressed += OnRootPointerPressed;
            }
        }

        private void CloseContextMenu()
        {
            _contextMenuBorder.IsVisible = false;

            // Удаляем обработчик закрытия
            var root = this.GetVisualRoot() as Window;
            if (root != null)
            {
                root.PointerPressed -= OnRootPointerPressed;
            }
        }

        private void OnRootPointerPressed(object sender, PointerPressedEventArgs e)
        {
            // Если клик был вне контекстного меню, закрываем его
            var point = e.GetPosition(_contextMenuBorder);

            if (point.X < 0 || point.Y < 0 ||
                point.X > _contextMenuBorder.Bounds.Width ||
                point.Y > _contextMenuBorder.Bounds.Height)
            {
                CloseContextMenu();

                // Удаляем обработчик
                var root = this.GetVisualRoot() as Window;
                if (root != null)
                {
                    root.PointerPressed -= OnRootPointerPressed;
                }
            }
        }


        private string GetNodeTypeDisplayName(NodeType nodeType)
        {
            switch (nodeType)
            {
                case NodeType.Input:
                    return "Входная нода";
                case NodeType.Output:
                    return "Выходная нода";
                case NodeType.Math:
                    return "Математическая операция";
                case NodeType.Variable:
                    return "Переменная";
                case NodeType.Function:
                    return "Функция";
                case NodeType.Constant:
                    return "Константа";
                case NodeType.Logic:
                    return "Логическая операция";
                case NodeType.Vector:
                    return "Вектор";
                case NodeType.Color:
                    return "Цвет";
                case NodeType.Texture:
                    return "Текстура";
                default:
                    return nodeType.ToString();
            }
        }

        private void AddPinsForNodeType(NodeElement node, NodeType nodeType)
        {
            switch (nodeType)
            {
                case NodeType.Input:
                    node.AddOutputPin("Выход", DataType.Float);
                    break;

                case NodeType.Output:
                    node.AddInputPin("Вход", DataType.Float);
                    break;

                case NodeType.Math:
                    node.AddInputPin("A", DataType.Float);
                    node.AddInputPin("B", DataType.Float);
                    node.AddOutputPin("Результат", DataType.Float);
                    break;

                case NodeType.Variable:
                    node.AddInputPin("Значение", DataType.Any, true);
                    node.AddOutputPin("Значение", DataType.Any);
                    break;

                case NodeType.Function:
                    node.AddInputPin("Параметр 1", DataType.Any, true);
                    node.AddInputPin("Параметр 2", DataType.Any, true);
                    node.AddOutputPin("Результат", DataType.Any);
                    break;

                case NodeType.Constant:
                    node.AddOutputPin("Значение", DataType.Float);
                    break;

                case NodeType.Logic:
                    node.AddInputPin("A", DataType.Boolean);
                    node.AddInputPin("B", DataType.Boolean);
                    node.AddOutputPin("Результат", DataType.Boolean);
                    break;

                case NodeType.Vector:
                    node.AddInputPin("X", DataType.Float);
                    node.AddInputPin("Y", DataType.Float);
                    node.AddInputPin("Z", DataType.Float);
                    node.AddOutputPin("Вектор", DataType.Vector);
                    break;

                case NodeType.Color:
                    node.AddInputPin("R", DataType.Float);
                    node.AddInputPin("G", DataType.Float);
                    node.AddInputPin("B", DataType.Float);
                    node.AddInputPin("A", DataType.Float);
                    node.AddOutputPin("Цвет", DataType.Color);
                    break;

                case NodeType.Texture:
                    node.AddInputPin("UV", DataType.Vector);
                    node.AddOutputPin("RGBA", DataType.Color);
                    node.AddOutputPin("R", DataType.Float);
                    node.AddOutputPin("G", DataType.Float);
                    node.AddOutputPin("B", DataType.Float);
                    node.AddOutputPin("A", DataType.Float);
                    break;
            }
        }

        private NodeElement DuplicateNode(NodeElement original)
        {
            // Создаем новую ноду того же типа, но смещенную вниз и вправо
            var offsetPosition = new Point(original.Position.X + 30, original.Position.Y + 30);

            var duplicate = CreateNode(original.NodeType, original.Title, offsetPosition);

            // Копируем пины
            foreach (var pin in original.InputPins)
            {
                duplicate.AddInputPin(pin.Title, pin.DataType, pin.AcceptAnyType, pin.AllowMultipleConnections);
            }

            foreach (var pin in original.OutputPins)
            {
                duplicate.AddOutputPin(pin.Title, pin.DataType, pin.AllowMultipleConnections);
            }

            return duplicate;
        }

        private void CenterView()
        {
            if (_nodes.Count == 0)
                return;

            // Находим границы всех нод
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;

            foreach (var node in _nodes)
            {
                minX = Math.Min(minX, node.Position.X);
                minY = Math.Min(minY, node.Position.Y);
                maxX = Math.Max(maxX, node.Position.X + node.Width);
                maxY = Math.Max(maxY, node.Position.Y + node.Height);
            }

            // Находим центр области с нодами
            double centerX = (minX + maxX) / 2;
            double centerY = (minY + maxY) / 2;

            // Находим центр видимой области скроллвьювера
            //double viewportCenterX = _scrollViewer.ViewportWidth / 2;
            //double viewportCenterY = _scrollViewer.ViewportHeight / 2;
            double viewportCenterX = _scrollViewer.Width / 2;
            double viewportCenterY = _scrollViewer.Height / 2;

            // Рассчитываем новое смещение
            double newOffsetX = centerX - viewportCenterX;
            double newOffsetY = centerY - viewportCenterY;

            // Устанавливаем новое смещение
            _scrollViewer.Offset = new Vector(newOffsetX, newOffsetY);
        }

        private void ResetZoom()
        {
            _zoom = 1.0;

            var scaleTransform = new ScaleTransform(_zoom, _zoom);
            _canvas.RenderTransform = scaleTransform;

            // Обновляем размеры холста
            _canvas.Width = 3000;
            _canvas.Height = 3000;
        }

        private Point GetCanvasCenterPosition()
        {
            //double centerX = _scrollViewer.Offset.X + _scrollViewer.ViewportWidth / 2;
            //double centerY = _scrollViewer.Offset.Y + _scrollViewer.ViewportHeight / 2;
            double centerX = _scrollViewer.Offset.X + _scrollViewer.Width / 2;
            double centerY = _scrollViewer.Offset.Y + _scrollViewer.Height / 2;

            return new Point(centerX, centerY);
        }

        public void Clear()
        {
            foreach (var connection in _connections.ToList())
            {
                RemoveConnection(connection);
            }

            foreach (var node in _nodes.ToList())
            {
                RemoveNode(node);
            }

            _connections.Clear();
            _nodes.Clear();
        }


        public void Open()
        {
            _isOpen = true;
        }

        public void Close()
        {
            _isOpen = false;
            OnClose?.Invoke(this);
        }

        public void Dispose()
        {
            // Очищаем ресурсы
            Clear();

            // Отписываемся от событий
            _canvas.PointerPressed -= OnCanvasPointerPressed;
            _canvas.PointerMoved -= OnCanvasPointerMoved;
            _canvas.PointerReleased -= OnCanvasPointerReleased;
            _canvas.PointerWheelChanged -= OnCanvasPointerWheelChanged;

            foreach (var node in _nodes)
            {
                node.PointerPressed -= OnNodePointerPressed;
                node.PointerMoved -= OnNodePointerMoved;
                node.PointerReleased -= OnNodePointerReleased;
            }
        }

        public void Redraw()
        {
            // Перерисовываем соединения
            UpdateConnections();

            // Запрашиваем перерисовку холста
            _canvas.InvalidateVisual();
        }

    }


    public enum NodeType
    {
        Input,
        Output,
        Math,
        Variable,
        Function,
        Constant,
        Logic,
        Vector,
        Color,
        Texture
    }

    public enum DataType
    {
        Any,
        Boolean,
        Integer,
        Float,
        Vector,
        Color,
        String,
        Object
    }

    public enum PinDirection
    {
        Input,
        Output
    }

    public class NodeElement : ContentControl
    {
        public NodeType NodeType { get; set; }
        public string Title { get; set; }
        public Point Position { get; set; }
        public bool IsSelected { get; set; }

        public List<NodePin> InputPins { get; private set; } = new List<NodePin>();
        public List<NodePin> OutputPins { get; private set; } = new List<NodePin>();

        private Border _border;
        private TextBlock _titleText;
        private StackPanel _inputPinsPanel;
        private StackPanel _outputPinsPanel;

        public NodeElement()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            var mainGrid = new Grid();

            _border = new Border
            {
                Classes = { "nodeBackground" },
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(8),
                MinWidth = 160,
                MinHeight = 50
            };

            var mainStackPanel = new StackPanel
            {
                Spacing = 10
            };

            _titleText = new TextBlock
            {
                Text = Title ?? "Нода",
                Classes = { "nodeTitle" },
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var pinsGrid = new Grid();
            pinsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            pinsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

            _inputPinsPanel = new StackPanel
            {
                Spacing = 5,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            _outputPinsPanel = new StackPanel
            {
                Spacing = 5,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            Grid.SetColumn(_inputPinsPanel, 0);
            Grid.SetColumn(_outputPinsPanel, 1);

            pinsGrid.Children.Add(_inputPinsPanel);
            pinsGrid.Children.Add(_outputPinsPanel);

            mainStackPanel.Children.Add(_titleText);
            mainStackPanel.Children.Add(pinsGrid);

            _border.Child = mainStackPanel;
            mainGrid.Children.Add(_border);

            this.Content = mainGrid;

            // Подписываемся на изменение свойств
            this.PropertyChanged += (s, e) =>
            {
                if (e.Property.Name == nameof(IsSelected))
                {
                    UpdateSelectedState();
                }
                else if (e.Property.Name == nameof(Title))
                {
                    _titleText.Text = Title ?? "Нода";
                }
            };
        }

        /// <summary>
        /// Обновляет внешний вид ноды в зависимости от состояния выделения
        /// </summary>
        private void UpdateSelectedState()
        {
            if (IsSelected)
            {
                _border.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 122, 204));
                _border.BorderThickness = new Thickness(2);
            }
            else
            {
                _border.BorderBrush = new SolidColorBrush(Color.FromRgb(63, 63, 70));
                _border.BorderThickness = new Thickness(1);
            }
        }

        /// <summary>
        /// Добавляет входной пин к ноде
        /// </summary>
        public NodePin AddInputPin(
            string title,
            DataType dataType,
            bool acceptAnyType = false,
            bool allowMultipleConnections = false)
        {
            var pin = new NodePin
            {
                Title = title,
                DataType = dataType,
                Direction = PinDirection.Input,
                ParentNode = this,
                AcceptAnyType = acceptAnyType,
                AllowMultipleConnections = allowMultipleConnections
            };

            // Создаем UI для пина
            var pinControl = CreatePinControl(pin);
            _inputPinsPanel.Children.Add(pinControl);

            // Добавляем пин в список
            InputPins.Add(pin);

            // Обновляем позицию пина
            UpdatePinPosition(pin, pinControl);

            return pin;
        }

        /// <summary>
        /// Добавляет выходной пин к ноде
        /// </summary>
        public NodePin AddOutputPin(
            string title,
            DataType dataType,
            bool allowMultipleConnections = true)
        {
            var pin = new NodePin
            {
                Title = title,
                DataType = dataType,
                Direction = PinDirection.Output,
                ParentNode = this,
                AllowMultipleConnections = allowMultipleConnections,
                AllowStartConnection = true
            };

            // Создаем UI для пина
            var pinControl = CreatePinControl(pin);
            _outputPinsPanel.Children.Add(pinControl);

            // Добавляем пин в список
            OutputPins.Add(pin);

            // Обновляем позицию пина
            UpdatePinPosition(pin, pinControl);

            return pin;
        }

        /// <summary>
        /// Создает UI для пина
        /// </summary>
        private Grid CreatePinControl(NodePin pin)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

            // Создаем визуальное представление пина (круг)
            var pinShape = new Border
            {
                Width = 10,
                Height = 10,
                CornerRadius = new CornerRadius(5),
                Background = GetPinColorBrush(pin.DataType),
                Tag = pin, // Сохраняем ссылку на модель пина
                VerticalAlignment = VerticalAlignment.Center
            };

            // Создаем текст для названия пина
            var pinTitle = new TextBlock
            {
                Text = pin.Title,
                Classes = { "pinTitle" },
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0)
            };

            // Размещаем элементы в гриде в зависимости от направления пина
            if (pin.Direction == PinDirection.Input)
            {
                Grid.SetColumn(pinShape, 0);
                Grid.SetColumn(pinTitle, 1);
            }
            else
            {
                grid.ColumnDefinitions[0].Width = GridLength.Star;
                grid.ColumnDefinitions[1].Width = GridLength.Auto;

                Grid.SetColumn(pinShape, 1);
                Grid.SetColumn(pinTitle, 0);

                pinTitle.HorizontalAlignment = HorizontalAlignment.Right;
            }

            grid.Children.Add(pinShape);
            grid.Children.Add(pinTitle);

            // Подписываемся на события UI элемента пина
            pinShape.Tag = pin;

            return grid;
        }

        /// <summary>
        /// Обновляет позицию пина на основе положения его UI элемента
        /// </summary>
        private void UpdatePinPosition(NodePin pin, Grid pinControl)
        {
            // Прямо сейчас не можем получить точную позицию, так как контрол еще не размещен
            // Поэтому используем событие загрузки
            this.AttachedToVisualTree += (s, e) =>
            {
                // Находим элемент пина в гриде
                var pinShape = pinControl.Children.OfType<Border>().FirstOrDefault();
                if (pinShape != null)
                {
                    // Получаем позицию относительно ноды
                    var pinPos = pinShape.TranslatePoint(new Point(pinShape.Width / 2, pinShape.Height / 2), this);
                    if (pinPos.HasValue)
                    {
                        // Сохраняем позицию в модели пина
                        pin.Position = pinPos.Value;
                    }
                }
            };

            // Также обновляем позицию при изменении размера ноды
            this.SizeChanged += (s, e) =>
            {
                var pinShape = pinControl.Children.OfType<Border>().FirstOrDefault();
                if (pinShape != null)
                {
                    var pinPos = pinShape.TranslatePoint(new Point(pinShape.Width / 2, pinShape.Height / 2), this);
                    if (pinPos.HasValue)
                    {
                        pin.Position = pinPos.Value;
                    }
                }
            };
        }

        /// <summary>
        /// Возвращает цвет для пина в зависимости от типа данных
        /// </summary>
        private IBrush GetPinColorBrush(DataType dataType)
        {
            switch (dataType)
            {
                case DataType.Boolean:
                    return new SolidColorBrush(Color.FromRgb(150, 0, 0));
                case DataType.Integer:
                    return new SolidColorBrush(Color.FromRgb(0, 150, 0));
                case DataType.Float:
                    return new SolidColorBrush(Color.FromRgb(0, 0, 150));
                case DataType.Vector:
                    return new SolidColorBrush(Color.FromRgb(150, 0, 150));
                case DataType.Color:
                    return new SolidColorBrush(Color.FromRgb(150, 150, 0));
                case DataType.String:
                    return new SolidColorBrush(Color.FromRgb(0, 150, 150));
                case DataType.Object:
                    return new SolidColorBrush(Color.FromRgb(100, 100, 100));
                case DataType.Any:
                default:
                    return new SolidColorBrush(Color.FromRgb(200, 200, 200));
            }
        }
    }

    public class NodePin
    {
        public string Title { get; set; }
        public DataType DataType { get; set; }
        public PinDirection Direction { get; set; }
        public Point Position { get; set; }
        public NodeElement ParentNode { get; set; }

        // Дополнительные свойства
        public bool AcceptAnyType { get; set; } = false; // Принимает любой тип данных
        public bool AllowMultipleConnections { get; set; } = false; // Разрешает несколько соединений
        public bool AllowStartConnection { get; set; } = false; // Разрешает начинать соединение от этого пина
    }

    public class NodeConnection : ContentControl
    {
        public NodePin StartPin { get; set; }
        public NodePin EndPin { get; set; }
        public Point StartPosition { get; set; }
        public Point EndPosition { get; set; }

        private Path _path;
        private const double CURVE_OFFSET = 80.0; // Смещение для кривой Безье

        public NodeConnection()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            var mainGrid = new Grid();

            _path = new Path
            {
                Stroke = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                StrokeThickness = 2,
                StrokeJoin = PenLineJoin.Round,
                StrokeLineCap = PenLineCap.Round
            };

            mainGrid.Children.Add(_path);

            this.Content = mainGrid;
        }

        /// <summary>
        /// Обновляет внешний вид соединения
        /// </summary>
        public void Update()
        {
            // Создаем геометрию пути с кривой Безье
            var geometry = new PathGeometry();
            var figure = new PathFigure();

            figure.StartPoint = StartPosition;

            // Вычисляем контрольные точки для кривой Безье
            double offsetX = CURVE_OFFSET;

            var bezierSegment = new BezierSegment();
            bezierSegment.Point1 = new Point(StartPosition.X + offsetX, StartPosition.Y);
            bezierSegment.Point2 = new Point(EndPosition.X - offsetX, EndPosition.Y);
            bezierSegment.Point3 = EndPosition;
            bezierSegment.IsStroked = true;

            //var bezierSegment = new BezierSegment(
            //    new Point(StartPosition.X + offsetX, StartPosition.Y),
            //    new Point(EndPosition.X - offsetX, EndPosition.Y),
            //    EndPosition,
            //    true
            //);

            figure.Segments.Add(bezierSegment);
            geometry.Figures.Add(figure);

            _path.Data = geometry;

            // Если у нас есть информация о типах данных, устанавливаем цвет соединения
            if (StartPin != null)
            {
                _path.Stroke = GetConnectionColorBrush(StartPin.DataType);
            }
            else if (EndPin != null)
            {
                _path.Stroke = GetConnectionColorBrush(EndPin.DataType);
            }
        }

        /// <summary>
        /// Возвращает цвет для соединения в зависимости от типа данных
        /// </summary>
        private IBrush GetConnectionColorBrush(DataType dataType)
        {
            switch (dataType)
            {
                case DataType.Boolean:
                    return new SolidColorBrush(Color.FromRgb(200, 50, 50));
                case DataType.Integer:
                    return new SolidColorBrush(Color.FromRgb(50, 200, 50));
                case DataType.Float:
                    return new SolidColorBrush(Color.FromRgb(50, 50, 200));
                case DataType.Vector:
                    return new SolidColorBrush(Color.FromRgb(200, 50, 200));
                case DataType.Color:
                    return new SolidColorBrush(Color.FromRgb(200, 200, 50));
                case DataType.String:
                    return new SolidColorBrush(Color.FromRgb(50, 200, 200));
                case DataType.Object:
                    return new SolidColorBrush(Color.FromRgb(150, 150, 150));
                case DataType.Any:
                default:
                    return new SolidColorBrush(Color.FromRgb(200, 200, 200));
            }
        }
    }

}
