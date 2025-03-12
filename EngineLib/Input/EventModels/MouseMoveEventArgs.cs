using System.Numerics;

namespace AtomEngine
{
    public class MouseMoveEventArgs : EventArgs
    {
        public Vector2 Position { get; }
        public Vector2 Delta { get; }
        public ModifierKeys Modifiers { get; }

        public MouseMoveEventArgs(Vector2 position, Vector2 delta, ModifierKeys modifiers)
        {
            Position = position;
            Delta = delta;
            Modifiers = modifiers;
        }
    }
}
