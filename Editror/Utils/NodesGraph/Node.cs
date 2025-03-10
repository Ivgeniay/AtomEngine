using System.Collections.Generic;
using System.Numerics;

namespace Editor.NodeSpace
{
    public class Node
    {
        public string Id { get; private set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public Vector2 Position { get; set; } = Vector2.Zero;
        public Vector2 Size { get; set; } = new Vector2(200, 100);
        public List<NodePort> InputPorts { get; private set; } = new List<NodePort>();
        public List<NodePort> OutputPorts { get; private set; } = new List<NodePort>();
        public bool IsSelected { get; set; } = false;
        public bool IsDragging { get; set; } = false;
        public int ZIndex { get; set; } = 0;

        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();

        public Node(string id, string title, string type)
        {
            Id = id;
            Title = title;
            Type = type;
        }

        public NodePort AddInputPort(string name, string type)
        {
            var port = new NodePort($"{Id}_in_{InputPorts.Count}", name, type, true, this);
            InputPorts.Add(port);
            UpdatePortPositions();
            return port;
        }

        public NodePort AddOutputPort(string name, string type)
        {
            var port = new NodePort($"{Id}_out_{OutputPorts.Count}", name, type, false, this);
            OutputPorts.Add(port);
            UpdatePortPositions();
            return port;
        }

        public void UpdatePortPositions()
        {
            float inputPortSpacing = Size.Y / (InputPorts.Count + 1);
            float outputPortSpacing = Size.Y / (OutputPorts.Count + 1);

            for (int i = 0; i < InputPorts.Count; i++)
            {
                InputPorts[i].UpdatePosition(new Vector2(0, (i + 1) * inputPortSpacing));
            }

            for (int i = 0; i < OutputPorts.Count; i++)
            {
                OutputPorts[i].UpdatePosition(new Vector2(Size.X, (i + 1) * outputPortSpacing));
            }
        }

        public void MoveTo(Vector2 position)
        {
            Position = position;
        }

        public void Resize(Vector2 size)
        {
            Size = size;
            UpdatePortPositions();
        }

        public bool ContainsPoint(Vector2 point)
        {
            return point.X >= Position.X &&
                   point.X <= Position.X + Size.X &&
                   point.Y >= Position.Y &&
                   point.Y <= Position.Y + Size.Y;
        }

        public NodePort GetPortAtPosition(Vector2 point, float tolerance = 10.0f)
        {
            foreach (var port in InputPorts)
            {
                Vector2 portPos = port.GetAbsolutePosition();
                float distance = Vector2.Distance(portPos, point);
                if (distance <= tolerance)
                {
                    return port;
                }
            }

            foreach (var port in OutputPorts)
            {
                Vector2 portPos = port.GetAbsolutePosition();
                float distance = Vector2.Distance(portPos, point);
                if (distance <= tolerance)
                {
                    return port;
                }
            }

            return null;
        }

        public bool RemovePort(NodePort port)
        {
            if (port.IsInput)
            {
                port.DisconnectAll();
                return InputPorts.Remove(port);
            }
            else
            {
                port.DisconnectAll();
                return OutputPorts.Remove(port);
            }
        }

        public void ClearPorts()
        {
            foreach (var port in InputPorts)
            {
                port.DisconnectAll();
            }

            foreach (var port in OutputPorts)
            {
                port.DisconnectAll();
            }

            InputPorts.Clear();
            OutputPorts.Clear();
        }
    }
}
