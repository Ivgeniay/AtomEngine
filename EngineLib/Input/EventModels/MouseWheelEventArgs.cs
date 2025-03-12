using System.Numerics;

namespace AtomEngine
{
    public class MouseWheelEventArgs : EventArgs
    {
        public Vector2 Position { get; }
        public float Delta { get; }
        public MouseWheelDirection Direction { get; }
        public ModifierKeys Modifiers { get; }

        public MouseWheelEventArgs(Vector2 position, float delta, MouseWheelDirection direction, ModifierKeys modifiers)
        {
            Position = position;
            Delta = delta;
            Direction = direction;
            Modifiers = modifiers;
        }
    }
}
