using System.Collections.Generic;
using Avalonia.Controls.Shapes;
using Avalonia.Collections;
using Avalonia.Threading;
using Avalonia.Controls;
using Editor.NodeSpace;
using System.Numerics;
using Avalonia.Media;
using Avalonia.Input;
using Avalonia;
using System;

namespace Editor
{
    public class NodeGraphController : ContentControl, IWindowed
    {
        public Action<object> OnClose { get; set; }
        public Canvas Canvas { get => _canvas; } 

        private NodeGraph _nodeGraph;
        private Canvas _canvas;
        private Grid _mainGrid;
        private Border _toolbarBorder;
        private bool _isOpen = false;
        private DispatcherTimer _renderTimer;

        public List<NodeVisual> _nodeVisuals = new List<NodeVisual>();
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
                    //if (IsPointInRectangle(point, nodeVisual.Position, nodeVisual.Size))
                    //{
                    //    _dragNode = nodeVisual.Node;
                    //    _nodeGraph.SelectNode(_dragNode);
                    //    e.Handled = true;
                    //    return;
                    //}
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
            for (int i = _nodeVisuals.Count - 1; i >= 0; i--)
            {
                if (_nodeVisuals[i].Node == node)
                {
                    var nodeVisual = _nodeVisuals[i];
                    _canvas.Children.Remove(nodeVisual);
                    _nodeVisuals.RemoveAt(i);
                }
            }

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

        private void CreateConnectionVisual(NodeConnection connection)
        {
            var connectionVisual = new ConnectionVisual(connection, this);
            _connectionVisuals.Add(connectionVisual);
            _canvas.Children.Add(connectionVisual);
        }

        public void UpdateVisuals()
        {
            foreach (var nodeVisual in _nodeVisuals)
            {
                //nodeVisual.UpdateVisual();
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
            RemoveTemporaryConnection();

            foreach (var nodeVisual in _nodeVisuals)
            {
                nodeVisual.Update();
            }

            foreach (var connectionVisual in _connectionVisuals)
            {
                connectionVisual.Update();
            }

            if (_isDraggingConnection && _dragPort != null)
            {
                DrawTemporaryConnection();
            }
        }

        private void RemoveTemporaryConnection()
        {
            for (int i = _canvas.Children.Count - 1; i >= 0; i--)
            {
                var child = _canvas.Children[i];
                if (child is Path path && path.Tag == null)
                {
                    _canvas.Children.RemoveAt(i);
                }
            }
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

        public void StartDraggingConnection(NodePort port, Point startPoint)
        {
            _dragPort = port;
            _isDraggingConnection = true;
            _dragConnectionEndPoint = startPoint;
            Render();
        }

        public void RemoveConnection(NodeConnection connection)
        {
            _nodeGraph.RemoveConnection(connection);
            UpdateConnectionVisuals();
            Render();
        }

        private void CreateNodeVisual(Node node)
        {
            var nodeVisual = new NodeVisual(node);
            _nodeVisuals.Add(nodeVisual);

            // Создаем порты для ноды
            foreach (var port in node.InputPorts)
            {
                var portVisual = new PortVisual(port, this);
                nodeVisual.AddPortVisual(portVisual);
            }

            foreach (var port in node.OutputPorts)
            {
                var portVisual = new PortVisual(port, this);
                nodeVisual.AddPortVisual(portVisual);
            }

            // Позиционируем порты
            nodeVisual.UpdatePortPositions();

            // Добавляем ноду на холст
            _canvas.Children.Add(nodeVisual);
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

    public class NodeVisual : Border
    {
        public Node Node { get; private set; }

        public Vector2 Size
        {
            get => new Vector2((float)Width, (float)Height);
            set
            {
                Width = value.X;
                Height = value.Y;
            }
        }

        public Color BackgroundColor { get; set; } = new Color(51, 51, 51, 255);
        public Color BorderColor { get; set; } = new Color(75, 75, 75, 255);
        public Color HeaderColor { get; set; } = new Color(60, 60, 60, 255);
        public Color SelectedBorderColor { get; set; } = new Color(0, 125, 255, 255);

        public List<PortVisual> PortVisuals { get; } = new List<PortVisual>();

        private TextBlock _titleBlock;
        private StackPanel _contentPanel;

        public NodeVisual(Node node)
        {
            Node = node;

            Classes.Add("nodeBorder");
            Background = new SolidColorBrush(BackgroundColor);
            BorderBrush = new SolidColorBrush(BorderColor);
            BorderThickness = new Thickness(1);
            CornerRadius = new CornerRadius(5);

            // Создаем содержимое
            InitializeUI();

            Size = Node.Size;
            Canvas.SetLeft(this, Node.Position.X);
            Canvas.SetTop(this, Node.Position.Y);
            ZIndex = Node.ZIndex;
        }

        private void InitializeUI()
        {
            _contentPanel = new StackPanel();

            _titleBlock = new TextBlock
            {
                Classes = { "nodeTitle" },
                Text = Node.Title,
                Margin = new Thickness(5, 5, 5, 10),
                //HorizontalAlignment = HorizontalAlignment.Center,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Colors.LightGray)
            };

            _contentPanel.Children.Add(_titleBlock);

            Child = _contentPanel;
        }

        public void AddPortVisual(PortVisual portVisual)
        {
            PortVisuals.Add(portVisual);

            _contentPanel.Children.Add(portVisual);
        }

        public void Update()
        {
            Size = Node.Size;
            Canvas.SetLeft(this, Node.Position.X);
            Canvas.SetTop(this, Node.Position.Y);

            _titleBlock.Text = Node.Title;

            Background = new SolidColorBrush(BackgroundColor);
            BorderBrush = new SolidColorBrush(Node.IsSelected ? SelectedBorderColor : BorderColor);
            BorderThickness = new Thickness(Node.IsSelected ? 2 : 1);

            ZIndex = Node.ZIndex;
            UpdatePortPositions();
        }

        public void UpdatePortPositions()
        {
            float inputPortSpacing = Node.Size.Y / (Node.InputPorts.Count + 1);
            float outputPortSpacing = Node.Size.Y / (Node.OutputPorts.Count + 1);

            int inputCount = 0;
            int outputCount = 0;

            foreach (var portVisual in PortVisuals)
            {
                if (portVisual.Port.IsInput)
                {
                    portVisual.RelativePosition = new Vector2(0, (inputCount + 1) * inputPortSpacing);
                    inputCount++;
                }
                else
                {
                    portVisual.RelativePosition = new Vector2(Node.Size.X, (outputCount + 1) * outputPortSpacing);
                    outputCount++;
                }

                portVisual.UpdatePosition();
            }
        }
    }



    public class PortVisual : Border
    {
        public NodePort Port { get; }
        public Vector2 RelativePosition { get; set; }
        public Point Position { get; set; }
        public Color Color { get; private set; }

        private Ellipse _portCircle;
        private TextBlock _portLabel;
        private NodeGraphController _controller;

        public PortVisual(NodePort port, NodeGraphController controller)
        {
            Port = port;
            _controller = controller;
            Color = GetPortColor();
            Classes.Add("portCircleContainer");

            InitializeUI();
        }

        private void InitializeUI()
        {
            Grid portGrid = new Grid();
            portGrid.Margin = new Thickness(5, 2);

            portGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            portGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            portGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _portCircle = new Ellipse
            {
                Classes = { "portCircle" },
                Fill = new SolidColorBrush(Color),
                Stroke = Brushes.White,
                Width = 10,
                Height = 10,
                StrokeThickness = 1
            };

            _portLabel = new TextBlock
            {
                Classes = { "portLabel" },
                Text = Port.Name,
            };

            if (Port.IsInput)
            {
                Grid.SetColumn(_portCircle, 0);
                Grid.SetColumn(_portLabel, 1);
                //_portLabel.HorizontalAlignment = HorizontalAlignment.Left;
            }
            else
            {
                Grid.SetColumn(_portCircle, 2);
                Grid.SetColumn(_portLabel, 1);
                //_portLabel.HorizontalAlignment = HorizontalAlignment.Right;
            }

            portGrid.Children.Add(_portCircle);
            portGrid.Children.Add(_portLabel);

            this.Child = portGrid;

            ConfigureEvents();
        }

        private void ConfigureEvents()
        {
            this.PointerEntered += (s, e) =>
            {
                _portCircle.Width = 12;
                _portCircle.Height = 12;
                _portCircle.Stroke = Brushes.LightBlue;
                _portCircle.StrokeThickness = 2;
            };

            this.PointerExited += (s, e) =>
            {
                _portCircle.Width = 10;
                _portCircle.Height = 10;
                _portCircle.Stroke = Brushes.White;
                _portCircle.StrokeThickness = 1;
            };

            this.PointerPressed += (s, e) =>
            {
                if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                {
                    _controller.StartDraggingConnection(Port, e.GetPosition(_controller.Canvas));
                    e.Handled = true;
                }
            };
        }

        public void UpdatePosition()
        {
            if (Port.ParentNode != null)
            {
                Position = new Point(
                    Port.ParentNode.Position.X + (Port.IsInput ? 0 : Port.ParentNode.Size.X),
                    Port.ParentNode.Position.Y + RelativePosition.Y);
            }
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

    public class ConnectionVisual : Canvas
    {
        public NodeConnection Connection { get; }
        public Point StartPoint { get; private set; }
        public Point EndPoint { get; private set; }
        public Point ControlPoint1 { get; private set; }
        public Point ControlPoint2 { get; private set; }
        public Color Color { get; private set; }
        public double Thickness { get; } = 2.0;
        public bool IsDashed { get; } = false;

        private Path _visiblePath;
        private Path _invisiblePath;
        private NodeGraphController _controller;

        public ConnectionVisual(NodeConnection connection, NodeGraphController controller)
        {
            Connection = connection;
            _controller = controller;

            Color = new Color(
                (byte)(Connection.Color.W * 255),
                (byte)(Connection.Color.X * 255),
                (byte)(Connection.Color.Y * 255),
                (byte)(Connection.Color.Z * 255));

            InitializeUI();
            Update();
        }

        private void InitializeUI()
        {
            Width = double.NaN;  
            Height = double.NaN; 
            Background = Brushes.Transparent;
            ZIndex = 100;

            _visiblePath = new Path
            {
                Stroke = new SolidColorBrush(Color),
                StrokeThickness = Thickness,
                StrokeDashArray = IsDashed ? new AvaloniaList<double> { 4, 2 } : null,
                ZIndex = 2
            };

            _invisiblePath = new Path
            {
                Stroke = Brushes.Transparent,
                StrokeThickness = 15,
                ZIndex = 1,
                Tag = Connection
            };

            _invisiblePath.PointerEntered += (s, e) =>
            {
                _visiblePath.StrokeThickness = Thickness + 1.5;
                _visiblePath.Stroke = new SolidColorBrush(Colors.LightBlue);
            };

            _invisiblePath.PointerExited += (s, e) =>
            {
                _visiblePath.StrokeThickness = Thickness;
                _visiblePath.Stroke = new SolidColorBrush(Color);
            };

            _invisiblePath.PointerPressed += (s, e) =>
            {
                if (e.GetCurrentPoint(_invisiblePath).Properties.IsRightButtonPressed)
                {
                    var connection = (s as Path).Tag as NodeConnection;
                    if (connection != null)
                    {
                        _controller.RemoveConnection(connection);
                        e.Handled = true;
                    }
                }
            };

            Children.Add(_invisiblePath);
            Children.Add(_visiblePath);
        }

        public void Update()
        {
            var start = Connection.GetStartPosition();
            var end = Connection.GetEndPosition();

            StartPoint = new Point(start.X, start.Y);
            EndPoint = new Point(end.X, end.Y);

            Connection.CalculateBezierPoints();

            ControlPoint1 = new Point(Connection.StartTangent.X, Connection.StartTangent.Y);
            ControlPoint2 = new Point(Connection.EndTangent.X, Connection.EndTangent.Y);

            UpdatePathGeometry();
        }

        private void UpdatePathGeometry()
        {
            var path = new PathFigure
            {
                StartPoint = StartPoint,
                IsClosed = false
            };

            var bezierSegment = new BezierSegment
            {
                Point1 = ControlPoint1,
                Point2 = ControlPoint2,
                Point3 = EndPoint
            };

            path.Segments = new PathSegments { bezierSegment };

            var pathGeometry = new PathGeometry
            {
                Figures = new PathFigures { path }
            };

            _visiblePath.Data = pathGeometry;
            _invisiblePath.Data = pathGeometry;
        }
    }


    public static class ControlExtensions
    {
        public static T FindAncestorOfType<T>(this Control control) where T : Control
        {
            var parent = control.Parent;

            while (parent != null)
            {
                if (parent is T typedParent)
                {
                    return typedParent;
                }

                parent = parent.Parent;
            }

            return null;
        }
    }
}
