using System.Collections.Generic;
using System.Numerics;
using System;

namespace Editor.NodeSpace
{
    public class NodeFactory
    {
        private Dictionary<string, NodeType> _nodeTypes = new Dictionary<string, NodeType>();
        private int _nextNodeId = 1;
        public class NodeType
        {
            public string Type { get; set; }
            public string DefaultTitle { get; set; }
            public Vector2 DefaultSize { get; set; } = new Vector2(200, 100);
            public List<PortDefinition> InputPorts { get; set; } = new List<PortDefinition>();
            public List<PortDefinition> OutputPorts { get; set; } = new List<PortDefinition>();
            public Dictionary<string, object> DefaultData { get; set; } = new Dictionary<string, object>();
            public Action<Node> Initializer { get; set; }

            public class PortDefinition
            {
                public string Name { get; set; }
                public string Type { get; set; }
                public bool AllowMultipleConnections { get; set; }
                public bool AcceptAnyType { get; set; }
            }
        }

        public void RegisterNodeType(string type, Action<NodeType> configureAction)
        {
            var nodeType = new NodeType { Type = type };
            configureAction(nodeType);
            _nodeTypes[type] = nodeType;
        }

        public Node CreateNode(string type, string title = null)
        {
            if (!_nodeTypes.TryGetValue(type, out var nodeTypeInfo))
            {
                Console.WriteLine($"Неизвестный тип ноды: {type}");
                return null;
            }

            string id = $"node_{_nextNodeId++}";
            var node = new Node(id, title ?? nodeTypeInfo.DefaultTitle ?? type, type);

            node.Size = nodeTypeInfo.DefaultSize;

            foreach (var inputPortDef in nodeTypeInfo.InputPorts)
            {
                var port = node.AddInputPort(inputPortDef.Name, inputPortDef.Type);
                port.AllowMultipleConnections = inputPortDef.AllowMultipleConnections;
                port.AcceptAnyType = inputPortDef.AcceptAnyType;
            }

            foreach (var outputPortDef in nodeTypeInfo.OutputPorts)
            {
                var port = node.AddOutputPort(outputPortDef.Name, outputPortDef.Type);
                port.AllowMultipleConnections = outputPortDef.AllowMultipleConnections;
                port.AcceptAnyType = outputPortDef.AcceptAnyType;
            }

            foreach (var kvp in nodeTypeInfo.DefaultData)
            {
                node.Data[kvp.Key] = kvp.Value;
            }

            nodeTypeInfo.Initializer?.Invoke(node);

            return node;
        }

        public List<string> GetAvailableNodeTypes()
        {
            return new List<string>(_nodeTypes.Keys);
        }

        public bool HasNodeType(string type)
        {
            return _nodeTypes.ContainsKey(type);
        }
    }
}
