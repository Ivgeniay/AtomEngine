using System.Numerics;

namespace AtomEngine
{
    public class TouchEventArgs : EventArgs
    {
        public int TouchId { get; }
        public Vector2 Position { get; }
        public float Pressure { get; }

        public TouchEventArgs(int touchId, Vector2 position, float pressure)
        {
            TouchId = touchId;
            Position = position;
            Pressure = pressure;
        }
    }
}
