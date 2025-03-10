using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using System;

namespace Editor.NodeSpace
{
    public class NodeGraph
    {
        public List<Node> Nodes { get; private set; } = new List<Node>();
        public HashSet<NodeConnection> Connections { get; private set; } = new HashSet<NodeConnection>();

        public NodeFactory NodeFactory { get; private set; }

        private Node _selectedNode;
        private NodePort _selectedPort;
        private List<Node> _selectedNodes = new List<Node>();
        private bool _isDragging = false;
        private Vector2 _lastMousePosition;
        private Vector2 _dragOffset;

        private bool _isCreatingConnection = false;
        private NodePort _connectionSource;
        private NodePort _draggedPortSource;

        public Vector2 ViewOffset { get; set; } = Vector2.Zero;
        public float ViewScale { get; set; } = 1.0f;
        private bool _isPanning = false;
        private Vector2 _panStartPosition;

        public event Action<Node> NodeSelected;
        public event Action<Node> NodeCreated;
        public event Action<Node> NodeDeleted;
        public event Action<NodeConnection> ConnectionCreated;
        public event Action<NodeConnection> ConnectionDeleted;

        public NodeGraph()
        {
            NodeFactory = new NodeFactory();
            RegisterDefaultNodeTypes();
        }

        private void RegisterDefaultNodeTypes()
        {
            NodeFactory.RegisterNodeType("Math", config =>
            {
                config.DefaultTitle = "Math Operation";
                config.DefaultSize = new Vector2(180, 120);
                config.InputPorts.Add(new NodeFactory.NodeType.PortDefinition { Name = "A", Type = "float" });
                config.InputPorts.Add(new NodeFactory.NodeType.PortDefinition { Name = "B", Type = "float" });
                config.OutputPorts.Add(new NodeFactory.NodeType.PortDefinition { Name = "Result", Type = "float" });
                config.DefaultData["operation"] = "add";
            });

            // Нода ввода числа
            NodeFactory.RegisterNodeType("Number", config =>
            {
                config.DefaultTitle = "Number";
                config.DefaultSize = new Vector2(150, 80);
                config.OutputPorts.Add(new NodeFactory.NodeType.PortDefinition { Name = "Value", Type = "float" });
                config.DefaultData["value"] = 0.0f;
            });

            // Нода вывода
            NodeFactory.RegisterNodeType("Output", config =>
            {
                config.DefaultTitle = "Output";
                config.DefaultSize = new Vector2(150, 80);
                config.InputPorts.Add(new NodeFactory.NodeType.PortDefinition { Name = "Value", Type = "any", AcceptAnyType = true });
            });

            // Нода переменной
            NodeFactory.RegisterNodeType("Variable", config =>
            {
                config.DefaultTitle = "Variable";
                config.DefaultSize = new Vector2(150, 100);
                config.InputPorts.Add(new NodeFactory.NodeType.PortDefinition { Name = "Set", Type = "any", AcceptAnyType = true });
                config.OutputPorts.Add(new NodeFactory.NodeType.PortDefinition { Name = "Get", Type = "any", AcceptAnyType = true });
                config.DefaultData["name"] = "var";
                config.DefaultData["value"] = 0;
            });
        }

        public Node CreateNode(string type, string title = null, Vector2 position = default)
        {
            var node = NodeFactory.CreateNode(type, title);
            if (node != null)
            {
                node.Position = position;
                node.ZIndex = GetMaxZIndex() + 1;
                Nodes.Add(node);
                NodeCreated?.Invoke(node);
            }
            return node;
        }
        public void RemoveNode(Node node)
        {
            RemoveNodeConnections(node);

            _selectedNodes.Remove(node);
            if (_selectedNode == node)
                _selectedNode = null;

            Nodes.Remove(node);
            NodeDeleted?.Invoke(node);
        }
        public Node DuplicateNode(Node sourceNode, Vector2 offset = default)
        {
            if (offset == default)
                offset = new Vector2(20, 20);

            var newNode = CreateNode(sourceNode.Type, sourceNode.Title, sourceNode.Position + offset);

            foreach (var kvp in sourceNode.Data)
            {
                newNode.Data[kvp.Key] = kvp.Value;
            }

            return newNode;
        }
        public void SelectNode(Node node, bool addToSelection = false)
        {
            if (!addToSelection)
            {
                foreach (var n in _selectedNodes)
                {
                    n.IsSelected = false;
                }
                _selectedNodes.Clear();
            }

            if (node != null)
            {
                if (!_selectedNodes.Contains(node))
                {
                    _selectedNodes.Add(node);
                    node.IsSelected = true;
                    _selectedNode = node;

                    node.ZIndex = GetMaxZIndex() + 1;
                    NodeSelected?.Invoke(node);
                }
            }
        }
        public void UnselectAllNodes()
        {
            foreach (var node in _selectedNodes)
            {
                node.IsSelected = false;
            }
            _selectedNodes.Clear();
            _selectedNode = null;
        }
        public bool CanConnect(NodePort outputPort, NodePort inputPort)
        {
            // Проверка направлений портов
            if (outputPort.IsInput || !inputPort.IsInput)
                return false;

            // Проверка на самосоединение
            if (outputPort.ParentNode == inputPort.ParentNode)
                return false;

            // Проверка типов
            if (outputPort.Type != inputPort.Type && !outputPort.AcceptAnyType && !inputPort.AcceptAnyType)
                return false;

            // Проверка на несколько соединений
            if (!inputPort.AllowMultipleConnections && inputPort.Connections.Count > 0)
                return false;

            return true;
        }
        public NodeConnection CreateConnection(NodePort outputPort, NodePort inputPort)
        {
            if (!CanConnect(outputPort, inputPort))
                return null;

            // Если входной порт не поддерживает несколько соединений, удаляем существующие
            if (!inputPort.AllowMultipleConnections && inputPort.Connections.Count > 0)
            {
                RemovePortConnections(inputPort);
            }

            if (outputPort.Connect(inputPort))
            {
                NodeConnection newConnection = null;
                foreach (var conn in outputPort.Connections)
                {
                    if (conn.InputPort == inputPort)
                    {
                        newConnection = conn;
                        break;
                    }
                }

                if (newConnection != null)
                {
                    Connections.Add(newConnection);
                    ConnectionCreated?.Invoke(newConnection);
                    return newConnection;
                }
            }

            return null;
        }
        public void RemoveConnection(NodeConnection connection)
        {
            if (connection == null)
                return;

            connection.OutputPort.Disconnect(connection.InputPort);
            Connections.Remove(connection);

            ConnectionDeleted?.Invoke(connection);
        }
        public void RemovePortConnections(NodePort port)
        {
            var connectionsToRemove = new List<NodeConnection>(port.Connections);

            foreach (var connection in connectionsToRemove)
            {
                RemoveConnection(connection);
            }
        }
        public void RemoveNodeConnections(Node node)
        {
            var connectionsToRemove = new List<NodeConnection>();

            foreach (var port in node.InputPorts)
            {
                connectionsToRemove.AddRange(port.Connections);
            }

            foreach (var port in node.OutputPorts)
            {
                connectionsToRemove.AddRange(port.Connections);
            }

            foreach (var connection in connectionsToRemove)
            {
                RemoveConnection(connection);
            }
        }
        public void RemoveSelectedNodes()
        {
            foreach (var node in new List<Node>(_selectedNodes))
            {
                RemoveNode(node);
            }
        }
        public void UpdateConnections()
        {
            foreach (var connection in Connections)
            {
                connection.CalculateBezierPoints();
            }
        }
        public Node GetNodeAtPosition(Vector2 position)
        {
            for (int i = Nodes.Count - 1; i >= 0; i--)
            {
                if (Nodes[i].ContainsPoint(position))
                {
                    return Nodes[i];
                }
            }

            return null;
        }
        public NodePort GetPortAtPosition(Vector2 position, float tolerance = 10.0f)
        {
            foreach (var node in Nodes)
            {
                NodePort port = node.GetPortAtPosition(position, tolerance);
                if (port != null)
                {
                    return port;
                }
            }

            return null;
        }
        public int GetMaxZIndex()
        {
            int maxZ = 0;
            foreach (var node in Nodes)
            {
                maxZ = Math.Max(maxZ, node.ZIndex);
            }
            return maxZ;
        }


        public void HandleMouseDown(Vector2 position, bool isLeftButton, bool isMultiSelect)
        {
            _lastMousePosition = position;

            // Преобразование позиции с учетом смещения и масштаба вида
            Vector2 graphPosition = ScreenToGraphPosition(position);

            // Проверяем, попали ли в порт для создания соединения
            NodePort hitPort = GetPortAtPosition(graphPosition);
            if (hitPort != null)
            {
                _selectedPort = hitPort;

                if (isLeftButton)
                {
                    _isCreatingConnection = true;
                    _connectionSource = hitPort;
                    return;
                }
            }

            // Проверяем, попали ли в ноду
            Node hitNode = GetNodeAtPosition(graphPosition);
            if (hitNode != null)
            {
                if (isLeftButton)
                {
                    // Перетаскивание или выбор ноды
                    SelectNode(hitNode, isMultiSelect);
                    _isDragging = true;
                    _dragOffset = graphPosition - hitNode.Position;
                }
            }
            else
            {
                // Клик на пустом месте
                if (isLeftButton && !isMultiSelect)
                {
                    UnselectAllNodes();

                    // Начинаем панорамирование вида
                    _isPanning = true;
                    _panStartPosition = graphPosition;
                }
            }
        }
        public void HandleMouseMove(Vector2 position)
        {
            // Преобразование позиции с учетом смещения и масштаба вида
            Vector2 graphPosition = ScreenToGraphPosition(position);

            if (_isCreatingConnection && _connectionSource != null)
            {
                // Обработка создания соединения
                _draggedPortSource = GetPortAtPosition(graphPosition);
            }
            else if (_isDragging && _selectedNode != null)
            {
                // Перетаскивание выбранной ноды
                Vector2 newPos = graphPosition - _dragOffset;

                // Если выбрано несколько нод, перемещаем их все
                if (_selectedNodes.Count > 1)
                {
                    Vector2 delta = newPos - _selectedNode.Position;

                    foreach (var node in _selectedNodes)
                    {
                        if (node != _selectedNode)
                        {
                            node.MoveTo(node.Position + delta);
                        }
                    }
                }

                _selectedNode.MoveTo(newPos);

                // Обновляем соединения
                UpdateConnections();
            }
            else if (_isPanning)
            {
                // Панорамирование вида
                Vector2 delta = graphPosition - _panStartPosition;
                ViewOffset -= delta;
            }

            _lastMousePosition = position;
        }
        public void HandleMouseUp(Vector2 position)
        {
            // Преобразование позиции с учетом смещения и масштаба вида
            Vector2 graphPosition = ScreenToGraphPosition(position);

            if (_isCreatingConnection && _connectionSource != null)
            {
                // Завершение создания соединения
                NodePort targetPort = GetPortAtPosition(graphPosition);

                if (targetPort != null && targetPort != _connectionSource)
                {
                    // Определяем выходной и входной порты
                    NodePort outputPort = null;
                    NodePort inputPort = null;

                    if (_connectionSource.IsInput && !targetPort.IsInput)
                    {
                        inputPort = _connectionSource;
                        outputPort = targetPort;
                    }
                    else if (!_connectionSource.IsInput && targetPort.IsInput)
                    {
                        outputPort = _connectionSource;
                        inputPort = targetPort;
                    }

                    // Создаем соединение
                    if (outputPort != null && inputPort != null)
                    {
                        CreateConnection(outputPort, inputPort);
                    }
                }
            }

            _isCreatingConnection = false;
            _connectionSource = null;
            _draggedPortSource = null;
            _isDragging = false;
            _isPanning = false;
        }
        public void HandleMouseWheel(Vector2 position, float delta)
        {
            float zoomDelta = delta * 0.1f;

            // Ограничиваем масштаб
            float newScale = Math.Clamp(ViewScale + zoomDelta, 0.1f, 2.0f);

            if (Math.Abs(newScale - ViewScale) > 0.001f)
            {
                // Преобразование позиции с учетом текущего смещения и масштаба
                Vector2 mousePos = ScreenToGraphPosition(position);

                // Новый масштаб
                ViewScale = newScale;

                // Корректируем смещение, чтобы точка под курсором осталась неподвижной
                Vector2 newMousePos = ScreenToGraphPosition(position);
                ViewOffset += (newMousePos - mousePos);
            }
        }

        public Vector2 ScreenToGraphPosition(Vector2 screenPosition)
        {
            return screenPosition / ViewScale + ViewOffset;
        }
        public Vector2 GraphToScreenPosition(Vector2 graphPosition)
        {
            return (graphPosition - ViewOffset) * ViewScale;
        }
        public void CenterViewOnNode(Node node)
        {
            if (node != null)
            {
                ViewOffset = node.Position + node.Size / 2;
            }
        }
        public void CenterViewOnAllNodes()
        {
            if (Nodes.Count == 0)
                return;

            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);

            foreach (var node in Nodes)
            {
                min.X = Math.Min(min.X, node.Position.X);
                min.Y = Math.Min(min.Y, node.Position.Y);
                max.X = Math.Max(max.X, node.Position.X + node.Size.X);
                max.Y = Math.Max(max.Y, node.Position.Y + node.Size.Y);
            }

            Vector2 center = (min + max) / 2;
            ViewOffset = center;
        }
        public void ResetZoom()
        {
            ViewScale = 1.0f;
        }

        public Dictionary<string, object> ToJson()
        {
            var nodesData = new List<Dictionary<string, object>>();
            var connectionsData = new List<Dictionary<string, object>>();

            foreach (var node in Nodes)
            {
                var nodeData = new Dictionary<string, object>
                {
                    { "id", node.Id },
                    { "type", node.Type },
                    { "title", node.Title },
                    { "position", new float[] { node.Position.X, node.Position.Y } },
                    { "size", new float[] { node.Size.X, node.Size.Y } },
                    { "zIndex", node.ZIndex },
                    { "data", node.Data }
                };

                nodesData.Add(nodeData);
            }

            foreach (var connection in Connections)
            {
                var connectionData = new Dictionary<string, object>
                {
                    { "outputNodeId", connection.OutputPort.ParentNode.Id },
                    { "outputPortId", connection.OutputPort.Id },
                    { "inputNodeId", connection.InputPort.ParentNode.Id },
                    { "inputPortId", connection.InputPort.Id }
                };

                connectionsData.Add(connectionData);
            }

            return new Dictionary<string, object>
            {
                { "nodes", nodesData },
                { "connections", connectionsData },
                { "viewOffset", new float[] { ViewOffset.X, ViewOffset.Y } },
                { "viewScale", ViewScale }
            };
        }
        public void FromJson(Dictionary<string, object> jsonData)
        {
            ClearGraph();

            if (jsonData.TryGetValue("viewOffset", out var viewOffsetObj) && viewOffsetObj is float[] viewOffsetArray && viewOffsetArray.Length == 2)
            {
                ViewOffset = new Vector2(viewOffsetArray[0], viewOffsetArray[1]);
            }

            if (jsonData.TryGetValue("viewScale", out var viewScaleObj) && viewScaleObj is float viewScale)
            {
                ViewScale = viewScale;
            }

            Dictionary<string, Node> loadedNodes = new Dictionary<string, Node>();

            if (jsonData.TryGetValue("nodes", out var nodesObj) && nodesObj is List<Dictionary<string, object>> nodesData)
            {
                foreach (var nodeData in nodesData)
                {
                    string type = nodeData["type"] as string;
                    if (!NodeFactory.HasNodeType(type))
                    {
                        Console.WriteLine($"Неизвестный тип ноды: {type}");
                        continue;
                    }

                    Node node = NodeFactory.CreateNode(type, nodeData["title"] as string);

                    typeof(Node).GetProperty("Id").SetValue(node, nodeData["id"]);

                    if (nodeData["position"] is float[] posArray && posArray.Length == 2)
                    {
                        node.Position = new Vector2(posArray[0], posArray[1]);
                    }

                    if (nodeData["size"] is float[] sizeArray && sizeArray.Length == 2)
                    {
                        node.Size = new Vector2(sizeArray[0], sizeArray[1]);
                    }

                    if (nodeData["zIndex"] is int zIndex)
                    {
                        node.ZIndex = zIndex;
                    }

                    if (nodeData["data"] is Dictionary<string, object> dataDict)
                    {
                        foreach (var kvp in dataDict)
                        {
                            node.Data[kvp.Key] = kvp.Value;
                        }
                    }

                    node.UpdatePortPositions();

                    Nodes.Add(node);
                    loadedNodes[node.Id] = node;
                }
            }

            if (jsonData.TryGetValue("connections", out var connectionsObj) && connectionsObj is List<Dictionary<string, object>> connectionsData)
            {
                foreach (var connectionData in connectionsData)
                {
                    string outputNodeId = connectionData["outputNodeId"] as string;
                    string outputPortId = connectionData["outputPortId"] as string;
                    string inputNodeId = connectionData["inputNodeId"] as string;
                    string inputPortId = connectionData["inputPortId"] as string;

                    if (!loadedNodes.TryGetValue(outputNodeId, out Node outputNode) ||
                        !loadedNodes.TryGetValue(inputNodeId, out Node inputNode))
                    {
                        continue;
                    }

                    NodePort outputPort = outputNode.OutputPorts.FirstOrDefault(p => p.Id == outputPortId);
                    NodePort inputPort = inputNode.InputPorts.FirstOrDefault(p => p.Id == inputPortId);

                    if (outputPort != null && inputPort != null)
                    {
                        CreateConnection(outputPort, inputPort);
                    }
                }
            }

            UpdateConnections();
        }

        public void ClearGraph()
        {
            foreach (var node in new List<Node>(Nodes))
            {
                RemoveNode(node);
            }

            Nodes.Clear();
            Connections.Clear();
            _selectedNodes.Clear();
            _selectedNode = null;
        }
    }
}
