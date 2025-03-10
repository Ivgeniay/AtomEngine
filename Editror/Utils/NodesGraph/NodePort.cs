using System.Collections.Generic;
using System.Numerics;
using System;

namespace Editor.NodeSpace
{
    public class NodePort
    {
        public string Id { get; private set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsInput { get; private set; }
        public Node ParentNode { get; private set; }
        public List<NodeConnection> Connections { get; private set; } = new List<NodeConnection>();
        public Vector2 Position { get; private set; }


        public Vector4 Color { get; set; } = new Vector4(1, 1, 1, 1);
        public float Size { get; set; } = 10f;


        public bool AllowMultipleConnections { get; set; } = false;
        public bool AcceptAnyType { get; set; } = false;

        public NodePort(string id, string name, string type, bool isInput, Node parentNode)
        {
            Id = id;
            Name = name;
            Type = type;
            IsInput = isInput;
            ParentNode = parentNode;
        }

        public bool Connect(NodePort targetPort)
        {
            if (IsInput == targetPort.IsInput)
            {
                Console.WriteLine($"Нельзя соединить порты одинакового типа (оба входные или оба выходные)");
                return false;
            }

            if (Type != targetPort.Type && !AcceptAnyType && !targetPort.AcceptAnyType)
            {
                Console.WriteLine($"Несовместимые типы портов: {Type} и {targetPort.Type}");
                return false;
            }

            if (IsInput && !AllowMultipleConnections && Connections.Count > 0)
            {
                Console.WriteLine($"Входной порт не поддерживает несколько соединений");
                return false;
            }

            if (targetPort.IsInput && !targetPort.AllowMultipleConnections && targetPort.Connections.Count > 0)
            {
                Console.WriteLine($"Целевой входной порт не поддерживает несколько соединений");
                return false;
            }

            NodePort outputPort = IsInput ? targetPort : this;
            NodePort inputPort = IsInput ? this : targetPort;

            var connection = new NodeConnection
            {
                OutputPort = outputPort,
                InputPort = inputPort
            };

            outputPort.Connections.Add(connection);
            inputPort.Connections.Add(connection);

            return true;
        }

        public void Disconnect(NodePort targetPort)
        {
            for (int i = Connections.Count - 1; i >= 0; i--)
            {
                var connection = Connections[i];
                if (connection.OutputPort == targetPort || connection.InputPort == targetPort)
                {
                    // Удаляем соединение из обоих портов
                    if (connection.OutputPort != this)
                        connection.OutputPort.Connections.Remove(connection);

                    if (connection.InputPort != this)
                        connection.InputPort.Connections.Remove(connection);

                    Connections.RemoveAt(i);
                }
            }
        }

        public void DisconnectAll()
        {
            for (int i = Connections.Count - 1; i >= 0; i--)
            {
                var connection = Connections[i];

                if (connection.OutputPort != this)
                    connection.OutputPort.Connections.Remove(connection);

                if (connection.InputPort != this)
                    connection.InputPort.Connections.Remove(connection);
            }

            Connections.Clear();
        }

        public void UpdatePosition(Vector2 position)
        {
            Position = position;
        }

        public Vector2 GetAbsolutePosition()
        {
            return ParentNode.Position + Position;
        }
    }
}
