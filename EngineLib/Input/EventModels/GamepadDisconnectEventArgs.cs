namespace AtomEngine
{
    public class GamepadDisconnectEventArgs : EventArgs
    {
        public int GamepadIndex { get; }

        public GamepadDisconnectEventArgs(int gamepadIndex)
        {
            GamepadIndex = gamepadIndex;
        }
    }
}
