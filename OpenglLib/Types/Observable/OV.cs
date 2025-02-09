namespace OpenglLib
{
    public class OV<T>
    {
        public event Action<T>? OnValueChanged; 
        private T _value;
        public T Value
        {
            get => _value;
            set
            {
                if (!EqualityComparer<T>.Default.Equals(_value, value))
                {
                    _value = value;
                    OnValueChanged?.Invoke(value);
                }
            }
        }

        public OV() => _value = default; 
        public OV(Action<T> action) => OnValueChanged = action; 
        public OV(T value) => _value = value;
        public OV(Action<T> action, T value)
        {
            _value = value;
            OnValueChanged = action;
        }
        public ref T GetReference() => ref _value;
    }
}
