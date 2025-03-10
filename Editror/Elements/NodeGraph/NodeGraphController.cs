using System.Collections.Generic;
using Avalonia.Controls.Shapes;
using Avalonia.Collections;
using Avalonia.Threading;
using Avalonia.Controls;
using Editor.NodeSpace;
using System.Numerics;
using Avalonia.Media;
using Avalonia.Input;
using System.Linq;
using Avalonia;
using System;

namespace Editor
{
    public class NodeGraphController : ContentControl, IWindowed
    {
        public Action<object> OnClose { get; set; }

        private NodeGraph _nodeGraph;
        private Canvas _canvas;
        private Grid _mainGrid;
        private Border _toolbarBorder;
        private bool _isOpen = false;
        private DispatcherTimer _renderTimer;

        private List<NodeVisual> _nodeVisuals = new List<NodeVisual>();
        private List<ConnectionVisual> _connectionVisuals = new List<ConnectionVisual>();

        private bool _isLeftMouseDown;
        private bool _isRightMouseDown;
        private Point _lastMousePosition;
        private Node _dragNode;
        private NodePort _dragPort;
        private bool _isDraggingConnection;
        private Point _dragConnectionEndPoint;

        public NodeGraphController()
        {
            _nodeGraph = new NodeGraph();
            InitializeUI();
            InitializeEvents();
        }

        private void InitializeUI()
        {
            _mainGrid = new Grid();
            _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });

            _toolbarBorder = new Border
            {
                Classes = { "toolbarBackground" }
            };

            var toolbarPanel = new StackPanel
            {
                Classes = { "toolbarPanel" },
            };

            var addMathNodeButton = new Button { Content = "Add Math Node", Classes = { "toolButton" } };
            var addNumberNodeButton = new Button { Content = "Add Number Node", Classes = { "toolButton" } };
            var addOutputNodeButton = new Button { Content = "Add Output Node", Classes = { "toolButton" } };
            var centerViewButton = new Button { Content = "Center View", Classes = { "toolButton" } };
            var resetZoomButton = new Button { Content = "Reset Zoom", Classes = { "toolButton" } };

            addMathNodeButton.Click += (s, e) => AddNode("Math", new Vector2(100, 100));
            addNumberNodeButton.Click += (s, e) => AddNode("Number", new Vector2(100, 200));
            addOutputNodeButton.Click += (s, e) => AddNode("Output", new Vector2(300, 150));
            centerViewButton.Click += (s, e) => CenterView();
            resetZoomButton.Click += (s, e) => ResetZoom();

            toolbarPanel.Children.Add(addMathNodeButton);
            toolbarPanel.Children.Add(addNumberNodeButton);
            toolbarPanel.Children.Add(addOutputNodeButton);
            toolbarPanel.Children.Add(centerViewButton);
            toolbarPanel.Children.Add(resetZoomButton);

            _toolbarBorder.Child = toolbarPanel;

            _canvas = new Canvas
            {
                Classes = { "mainCanvas"},
            };

            _mainGrid.Children.Add(_toolbarBorder);
            _mainGrid.Children.Add(_canvas);

            Grid.SetRow(_toolbarBorder, 0);
            Grid.SetRow(_canvas, 1);

            this.Content = _mainGrid;
        }

        private void InitializeEvents()
        {
            _canvas.PointerPressed += OnCanvasPointerPressed;
            _canvas.PointerMoved += OnCanvasPointerMoved;
            _canvas.PointerReleased += OnCanvasPointerReleased;
            _canvas.PointerWheelChanged += OnCanvasPointerWheelChanged;

            _nodeGraph.NodeCreated += OnNodeCreated;
            _nodeGraph.NodeDeleted += OnNodeDeleted;
            _nodeGraph.ConnectionCreated += OnConnectionCreated;
            _nodeGraph.ConnectionDeleted += OnConnectionDeleted;

            _renderTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            _renderTimer.Tick += (sender, args) => Render();
        }

        private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var point = e.GetPosition(_canvas);
            _lastMousePosition = point;

            if (e.GetCurrentPoint(_canvas).Properties.IsLeftButtonPressed)
            {
                _isLeftMouseDown = true;
                var mousePos = new Vector2((float)point.X, (float)point.Y);
                foreach (var nodeVisual in _nodeVisuals)
                {
                    foreach (var portVisual in nodeVisual.PortVisuals)
                    {
                        if (IsPointInEllipse(point, portVisual.Position, 10))
                        {
                            _dragPort = portVisual.Port;
                            _isDraggingConnection = true;
                            _dragConnectionEndPoint = point;
                            e.Handled = true;
                            return;
                        }
                    }
                }

                // Проверяем, нажали ли на ноду для перетаскивания
                foreach (var nodeVisual in _nodeVisuals)
                {
                    if (IsPointInRectangle(point, nodeVisual.Position, nodeVisual.Size))
                    {
                        _dragNode = nodeVisual.Node;
                        _nodeGraph.SelectNode(_dragNode);
                        e.Handled = true;
                        return;
                    }
                }

                // Нажали на пустую область - готовимся к панорамированию
                _nodeGraph.HandleMouseDown(mousePos, true, e.KeyModifiers.HasFlag(KeyModifiers.Control));
            }
            else if (e.GetCurrentPoint(_canvas).Properties.IsRightButtonPressed)
            {
                _isRightMouseDown = true;
                // TODO: Показать контекстное меню
            }

            e.Handled = true;
        }

        private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
        {
            var point = e.GetPosition(_canvas);
            var delta = point - _lastMousePosition;
            var mousePos = new Vector2((float)point.X, (float)point.Y);

            if (_isDraggingConnection && _dragPort != null)
            {
                _dragConnectionEndPoint = point;
                Render();
            }
            else if (_dragNode != null)
            {
                _dragNode.Position += new Vector2((float)delta.X, (float)delta.Y);
                _nodeGraph.UpdateConnections();
                UpdateVisuals();
                Render();
            }
            else if (_isLeftMouseDown)
            {
                _nodeGraph.HandleMouseMove(mousePos);
                UpdateVisuals();
                Render();
            }

            _lastMousePosition = point;
            e.Handled = true;
        }

        private void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            var point = e.GetPosition(_canvas);
            var mousePos = new Vector2((float)point.X, (float)point.Y);

            if (_isDraggingConnection && _dragPort != null)
            {
                // Проверяем, находится ли курсор над портом
                bool connectionCreated = false;
                foreach (var nodeVisual in _nodeVisuals)
                {
                    foreach (var portVisual in nodeVisual.PortVisuals)
                    {
                        if (IsPointInEllipse(point, portVisual.Position, 15))
                        {
                            if (_dragPort.IsInput != portVisual.Port.IsInput)
                            {
                                // Создаем соединение
                                NodePort outputPort = !_dragPort.IsInput ? _dragPort : portVisual.Port;
                                NodePort inputPort = _dragPort.IsInput ? _dragPort : portVisual.Port;

                                var connection = _nodeGraph.CreateConnection(outputPort, inputPort);
                                if (connection != null)
                                {
                                    _nodeGraph.UpdateConnections();
                                    UpdateConnectionVisuals();
                                    connectionCreated = true;
                                }
                            }
                            break;
                        }
                    }
                    if (connectionCreated) break;
                }

                _isDraggingConnection = false;
                _dragPort = null;
                Render(); // Перерисовываем для удаления временного соединения
            }
            else if (_dragNode != null)
            {
                _dragNode = null;
            }

            _isLeftMouseDown = false;
            _isRightMouseDown = false;

            _nodeGraph.HandleMouseUp(mousePos);
            UpdateVisuals();
            Render(); // Добавляем явный вызов рендера

            e.Handled = true;
        }

        private void OnCanvasPointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            var point = e.GetPosition(_canvas);
            var mousePos = new Vector2((float)point.X, (float)point.Y);
            var delta = (float)e.Delta.Y;

            _nodeGraph.HandleMouseWheel(mousePos, delta);
            UpdateVisuals();

            e.Handled = true;
        }

        private void OnNodeCreated(Node node)
        {
            CreateNodeVisual(node);
            UpdateVisuals();
        }

        private void OnNodeDeleted(Node node)
        {
            // Удаляем визуальное представление ноды
            _nodeVisuals.RemoveAll(nv => nv.Node == node);
            UpdateVisuals();
        }

        private void OnConnectionCreated(NodeConnection connection)
        {
            CreateConnectionVisual(connection);
            UpdateVisuals();
        }

        private void OnConnectionDeleted(NodeConnection connection)
        {
            // Удаляем визуальное представление соединения
            _connectionVisuals.RemoveAll(cv => cv.Connection == connection);
            UpdateVisuals();
        }

        private Node AddNode(string type, Vector2 position)
        {
            Node node = _nodeGraph.CreateNode(type, null, position);

            if (node != null)
            {
                CreateNodeVisual(node);
                UpdateVisuals();
                Render();
            }
            return node;
        }

        private void CenterView()
        {
            _nodeGraph.CenterViewOnAllNodes();
            UpdateVisuals();
        }

        private void ResetZoom()
        {
            _nodeGraph.ResetZoom();
            UpdateVisuals();
        }

        private void CreateNodeVisual(Node node)
        {
            var nodeVisual = new NodeVisual(node);
            _nodeVisuals.Add(nodeVisual);

            foreach (var port in node.InputPorts)
            {
                var portVisual = new PortVisual(port);
                nodeVisual.PortVisuals.Add(portVisual);
            }

            foreach (var port in node.OutputPorts)
            {
                var portVisual = new PortVisual(port);
                nodeVisual.PortVisuals.Add(portVisual);
            }
        }

        private void CreateConnectionVisual(NodeConnection connection)
        {
            var connectionVisual = new ConnectionVisual(connection);
            _connectionVisuals.Add(connectionVisual);
        }

        private void UpdateVisuals()
        {
            foreach (var nodeVisual in _nodeVisuals)
            {
                nodeVisual.Update();
            }

            UpdateConnectionVisuals();
        }

        private void UpdateConnectionVisuals()
        {
            foreach (var connectionVisual in _connectionVisuals)
            {
                connectionVisual.Update();
            }
        }

        private void Render()
        {
            _canvas.Children.Clear();

            // Рисуем сетку (по желанию)

            // Рисуем ноды (теперь ноды идут первыми, соединения - поверх)
            foreach (var nodeVisual in _nodeVisuals)
            {
                DrawNode(nodeVisual);
            }

            // Рисуем соединения (поверх нод)
            foreach (var connectionVisual in _connectionVisuals)
            {
                DrawConnection(connectionVisual);
            }

            // Рисуем временное соединение при перетаскивании
            if (_isDraggingConnection && _dragPort != null)
            {
                DrawTemporaryConnection();
            }
        }

        private void DrawNode(NodeVisual nodeVisual)
        {
            var nodeBorder = new Border
            {
                Classes = { "nodeBorder" },
                Background = new SolidColorBrush(nodeVisual.BackgroundColor),
                BorderBrush = new SolidColorBrush(nodeVisual.Node.IsSelected ? Colors.DodgerBlue : nodeVisual.BorderColor),
                BorderThickness = new Thickness(nodeVisual.Node.IsSelected ? 2 : 1),
                Width = nodeVisual.Size.X,
                Height = nodeVisual.Size.Y
            };

            var titleBlock = new TextBlock
            {
                Classes = { "nodeTitle" },
                Text = nodeVisual.Node.Title,
            };

            var contentPanel = new StackPanel();
            contentPanel.Children.Add(titleBlock);

            nodeVisual.PortVisuals.Clear();

            for (int i = 0; i < nodeVisual.Node.InputPorts.Count; i++)
            {
                var port = nodeVisual.Node.InputPorts[i];
                var portVisual = new PortVisual(port);
                float spacing = nodeVisual.Size.Y / (nodeVisual.Node.InputPorts.Count + 1);
                portVisual.RelativePosition = new Vector2(0, (i + 1) * spacing);
                nodeVisual.PortVisuals.Add(portVisual);
            }

            for (int i = 0; i < nodeVisual.Node.OutputPorts.Count; i++)
            {
                var port = nodeVisual.Node.OutputPorts[i];
                var portVisual = new PortVisual(port);
                float spacing = nodeVisual.Size.Y / (nodeVisual.Node.OutputPorts.Count + 1);
                portVisual.RelativePosition = new Vector2(nodeVisual.Size.X, (i + 1) * spacing);
                nodeVisual.PortVisuals.Add(portVisual);
            }

            foreach (var portVisual in nodeVisual.PortVisuals)
            {
                var portPanel = new Grid
                {
                    Margin = new Thickness(5, 2)
                };

                portPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                portPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                portPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var portCircleContainer = new Border
                {
                    Classes = { "portCircleContainer" },
                };

                var portCircle = new Ellipse
                {
                    Classes = { "portCircle" },
                    Fill = new SolidColorBrush(portVisual.Color),
                    Stroke = Brushes.White,
                };

                portCircleContainer.Child = portCircle;

                portCircleContainer.PointerEntered += (s, e) =>
                {
                    portCircle.Width = 12;
                    portCircle.Height = 12;
                    portCircle.Stroke = Brushes.LightBlue;
                    portCircle.StrokeThickness = 2;
                };

                portCircleContainer.PointerExited += (s, e) =>
                {
                    portCircle.Width = 10;
                    portCircle.Height = 10;
                    portCircle.Stroke = Brushes.White;
                    portCircle.StrokeThickness = 1;
                };

                portCircleContainer.PointerPressed += (s, e) =>
                {
                    if (e.GetCurrentPoint(portCircleContainer).Properties.IsLeftButtonPressed)
                    {
                        _dragPort = portVisual.Port;
                        _isDraggingConnection = true;
                        _dragConnectionEndPoint = e.GetPosition(_canvas);
                        e.Handled = true;
                        Render();
                    }
                };

                var portLabel = new TextBlock
                {
                    Classes = { "portLabel" },
                    Text = portVisual.Port.Name,
                };

                if (portVisual.Port.IsInput)
                {
                    Grid.SetColumn(portCircleContainer, 0);
                    Grid.SetColumn(portLabel, 1);
                    portLabel.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;

                    portVisual.Position = new Point(
                        nodeVisual.Position.X,
                        nodeVisual.Position.Y + portVisual.RelativePosition.Y);
                }
                else
                {
                    Grid.SetColumn(portCircleContainer, 2);
                    Grid.SetColumn(portLabel, 1);
                    portLabel.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right;

                    portVisual.Position = new Point(
                        nodeVisual.Position.X + nodeVisual.Size.X,
                        nodeVisual.Position.Y + portVisual.RelativePosition.Y);
                }

                portPanel.Children.Add(portCircleContainer);
                portPanel.Children.Add(portLabel);
                contentPanel.Children.Add(portPanel);
            }

            nodeBorder.Child = contentPanel;

            Canvas.SetLeft(nodeBorder, nodeVisual.Position.X);
            Canvas.SetTop(nodeBorder, nodeVisual.Position.Y);
            _canvas.Children.Add(nodeBorder);
            nodeBorder.ZIndex = nodeVisual.Node.ZIndex;
        }

        private void DrawConnection(ConnectionVisual connectionVisual)
        {
            // Создаем контейнер для соединения с большей областью для интерактивности
            var connectionContainer = new Canvas
            {
                Width = _canvas.Width,
                Height = _canvas.Height,
                Background = Brushes.Transparent,
                ZIndex = 100 // Поверх нод
            };

            var path = new PathFigure
            {
                StartPoint = connectionVisual.StartPoint,
                IsClosed = false
            };

            var bezierSegment = new BezierSegment
            {
                Point1 = connectionVisual.ControlPoint1,
                Point2 = connectionVisual.ControlPoint2,
                Point3 = connectionVisual.EndPoint
            };

            path.Segments = new PathSegments { bezierSegment };

            var pathGeometry = new PathGeometry
            {
                Figures = new PathFigures { path }
            };

            // Создаем две линии: широкую прозрачную для hover и тонкую видимую
            var invisiblePath = new Path
            {
                Stroke = Brushes.Transparent,
                StrokeThickness = 15, // Широкая область для взаимодействия
                Data = pathGeometry,
                ZIndex = 1,
                Tag = connectionVisual.Connection // Для определения соединения при клике
            };

            var visiblePath = new Path
            {
                Stroke = new SolidColorBrush(connectionVisual.Color),
                StrokeThickness = connectionVisual.Thickness,
                Data = pathGeometry,
                StrokeDashArray = connectionVisual.IsDashed ? new AvaloniaList<double> { 4, 2 } : null,
                ZIndex = 2
            };

            // Добавляем обработчики событий
            invisiblePath.PointerEntered += (s, e) =>
            {
                visiblePath.StrokeThickness = connectionVisual.Thickness + 1.5;
                visiblePath.Stroke = new SolidColorBrush(Colors.LightBlue);
            };

            invisiblePath.PointerExited += (s, e) =>
            {
                visiblePath.StrokeThickness = connectionVisual.Thickness;
                visiblePath.Stroke = new SolidColorBrush(connectionVisual.Color);
            };

            invisiblePath.PointerPressed += (s, e) =>
            {
                if (e.GetCurrentPoint(invisiblePath).Properties.IsRightButtonPressed)
                {
                    // Удаляем соединение при нажатии правой кнопкой мыши
                    var connection = (s as Path).Tag as NodeConnection;
                    if (connection != null)
                    {
                        _nodeGraph.RemoveConnection(connection);
                        UpdateConnectionVisuals();
                        Render();
                    }
                    e.Handled = true;
                }
            };

            connectionContainer.Children.Add(invisiblePath);
            connectionContainer.Children.Add(visiblePath);
            _canvas.Children.Add(connectionContainer);
        }

        private void DrawTemporaryConnection()
        {
            if (_dragPort == null) return;

            Point startPoint;
            Point endPoint = _dragConnectionEndPoint;

            // Определяем начальную точку в зависимости от типа перетаскиваемого порта
            if (_dragPort.IsInput)
            {
                startPoint = GetPortPosition(_dragPort);
                // Для входного порта конечная точка - это точка под курсором
            }
            else
            {
                // Для выходного порта начальная точка - это позиция порта
                startPoint = GetPortPosition(_dragPort);
                // Конечная точка - это точка под курсором
            }

            // Создаем кривую Безье
            float dx = (float)Math.Abs(endPoint.X - startPoint.X);
            float tangentOffset = Math.Max(dx * 0.5f, 50f);

            Point controlPoint1 = new Point(startPoint.X + tangentOffset, startPoint.Y);
            Point controlPoint2 = new Point(endPoint.X - tangentOffset, endPoint.Y);

            var path = new PathFigure
            {
                StartPoint = startPoint,
                IsClosed = false
            };

            var bezierSegment = new BezierSegment
            {
                Point1 = controlPoint1,
                Point2 = controlPoint2,
                Point3 = endPoint
            };

            path.Segments = new PathSegments { bezierSegment };

            var pathGeometry = new PathGeometry
            {
                Figures = new PathFigures { path }
            };

            var drawing = new Path
            {
                Stroke = new SolidColorBrush(Colors.Orange),
                StrokeThickness = 2,
                Data = pathGeometry,
                StrokeDashArray = new AvaloniaList<double> { 4, 2 },
                ZIndex = 1000 // Самый верхний слой
            };

            _canvas.Children.Add(drawing);
        }

        private Point GetPortPosition(NodePort port)
        {
            foreach (var nodeVisual in _nodeVisuals)
            {
                if (nodeVisual.Node == port.ParentNode)
                {
                    foreach (var portVisual in nodeVisual.PortVisuals)
                    {
                        if (portVisual.Port == port)
                        {
                            return portVisual.Position;
                        }
                    }
                }
            }

            return new Point(0, 0);
        }

        private bool IsPointInRectangle(Point point, Vector2 rectPosition, Vector2 rectSize)
        {
            return point.X >= rectPosition.X && point.X <= rectPosition.X + rectSize.X &&
                   point.Y >= rectPosition.Y && point.Y <= rectPosition.Y + rectSize.Y;
        }

        private bool IsPointInEllipse(Point point, Point ellipseCenter, double radius)
        {
            double dx = point.X - ellipseCenter.X;
            double dy = point.Y - ellipseCenter.Y;
            return Math.Sqrt(dx * dx + dy * dy) <= radius;
        }

        public void Open()
        {
            _isOpen = true;
            _renderTimer.Start();
        }

        public void Close()
        {
            _isOpen = false;
            _renderTimer.Stop();
            OnClose?.Invoke(this);
        }

        public void Dispose()
        {
            _renderTimer.Stop();
            _canvas.PointerPressed -= OnCanvasPointerPressed;
            _canvas.PointerMoved -= OnCanvasPointerMoved;
            _canvas.PointerReleased -= OnCanvasPointerReleased;
            _canvas.PointerWheelChanged -= OnCanvasPointerWheelChanged;
        }

        public void Redraw()
        {
            Render();
        }

    }
    public class NodeVisual
    {
        public Node Node { get; }
        public Vector2 Position { get; private set; }
        public Vector2 Size { get; private set; }
        public Color BackgroundColor { get; private set; } = new Color(51, 51, 51, 255);
        public Color BorderColor { get; private set; } = new Color(75, 75, 75, 255);
        public Color HeaderColor { get; set; } = new Color(60, 60, 60, 255);
        public Color SelectedBorderColor { get; set; } = new Color(0, 125, 255, 255);

        public List<PortVisual> PortVisuals { get; } = new List<PortVisual>();

        public NodeVisual(Node node)
        {
            Node = node;
            Update();
        }

        public void Update()
        {
            Position = Node.Position;
            Size = Node.Size;

            float inputPortSpacing = Size.Y / (Node.InputPorts.Count + 1);
            float outputPortSpacing = Size.Y / (Node.OutputPorts.Count + 1);

            for (int i = 0; i < Node.InputPorts.Count; i++)
            {
                if (i < PortVisuals.Count && PortVisuals[i].Port == Node.InputPorts[i])
                {
                    PortVisuals[i].RelativePosition = new Vector2(0, (i + 1) * inputPortSpacing);
                }
            }

            for (int i = 0; i < Node.OutputPorts.Count; i++)
            {
                int index = Node.InputPorts.Count + i;
                if (index < PortVisuals.Count && PortVisuals[index].Port == Node.OutputPorts[i])
                {
                    PortVisuals[index].RelativePosition = new Vector2(Size.X, (i + 1) * outputPortSpacing);
                }
            }
        }
    }
    public class PortVisual
    {
        public NodePort Port { get; }
        public Vector2 RelativePosition { get; set; }
        public Point Position { get; set; }
        public Color Color { get; private set; }

        public PortVisual(NodePort port)
        {
            Port = port;
            Color = GetPortColor();
        }

        private Color GetPortColor()
        {
            switch (Port.Type)
            {
                case "float": return Colors.LightBlue;
                case "int": return Colors.LightGreen;
                case "string": return Colors.Orange;
                case "bool": return Colors.Red;
                case "any": return Colors.Purple;
                default: return Colors.Gray;
            }
        }
    }
    public class ConnectionVisual
        {
            public NodeConnection Connection { get; }
            public Point StartPoint { get; private set; }
            public Point EndPoint { get; private set; }
            public Point ControlPoint1 { get; private set; }
            public Point ControlPoint2 { get; private set; }
            public Color Color { get; private set; }
            public double Thickness { get; } = 2.0;
            public bool IsDashed { get; } = false;

            public ConnectionVisual(NodeConnection connection)
            {
                Connection = connection;
                Update();

                Color = new Color(
                    (byte)(Connection.Color.W * 255),
                    (byte)(Connection.Color.X * 255),
                    (byte)(Connection.Color.Y * 255),
                    (byte)(Connection.Color.Z * 255));
            }

            public void Update()
            {
                Vector2 start = Connection.GetStartPosition();
                Vector2 end = Connection.GetEndPosition();

                StartPoint = new Point(start.X, start.Y);
                EndPoint = new Point(end.X, end.Y);

                Connection.CalculateBezierPoints();

                ControlPoint1 = new Point(Connection.StartTangent.X, Connection.StartTangent.Y);
                ControlPoint2 = new Point(Connection.EndTangent.X, Connection.EndTangent.Y);
            }
        }
}
