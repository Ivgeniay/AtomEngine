namespace AtomEngine
{
    public class GamepadConnectEventArgs : EventArgs
    {
        public int GamepadIndex { get; }
        public string Name { get; }

        public GamepadConnectEventArgs(int gamepadIndex, string name)
        {
            GamepadIndex = gamepadIndex;
            Name = name;
        }
    }
}
