namespace AtomEngine
{
    public class GamepadButtonEventArgs : EventArgs
    {
        public int GamepadIndex { get; }
        public GamepadButton Button { get; }
        public float Value { get; }

        public GamepadButtonEventArgs(int gamepadIndex, GamepadButton button, float value)
        {
            GamepadIndex = gamepadIndex;
            Button = button;
            Value = value;
        }
    }
}
