namespace AtomEngine
{
    public class KeyEventArgs : EventArgs
    {
        public Key Key { get; }
        public bool IsRepeat { get; }
        public ModifierKeys Modifiers { get; }

        public KeyEventArgs(Key key, bool isRepeat, ModifierKeys modifiers)
        {
            Key = key;
            IsRepeat = isRepeat;
            Modifiers = modifiers;
        }
    }
}
