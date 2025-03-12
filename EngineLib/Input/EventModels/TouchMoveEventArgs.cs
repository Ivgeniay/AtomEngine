using System.Numerics;

namespace AtomEngine
{
    public class TouchMoveEventArgs : EventArgs
    {
        public int TouchId { get; }
        public Vector2 Position { get; }
        public Vector2 Delta { get; }
        public float Pressure { get; }

        public TouchMoveEventArgs(int touchId, Vector2 position, Vector2 delta, float pressure)
        {
            TouchId = touchId;
            Position = position;
            Delta = delta;
            Pressure = pressure;
        }
    }
}
