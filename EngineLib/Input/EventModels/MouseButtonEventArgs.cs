using System.Numerics;

namespace AtomEngine
{
    public class MouseButtonEventArgs : EventArgs
    {
        public MouseButton Button { get; }
        public Vector2 Position { get; }
        public int ClickCount { get; }
        public ModifierKeys Modifiers { get; }

        public MouseButtonEventArgs(MouseButton button, Vector2 position, int clickCount, ModifierKeys modifiers)
        {
            Button = button;
            Position = position;
            ClickCount = clickCount;
            Modifiers = modifiers;
        }
    }
}
