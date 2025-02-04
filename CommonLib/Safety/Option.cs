public readonly struct Option<T>
{
    private readonly T _value;
    private readonly bool _hasValue;

    private Option(T value, bool hasValue)
    {
        _value = value;
        _hasValue = hasValue;
    }

    public static Option<T> Some(T value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value), "Cannot create Some with null value");
        return new Option<T>(value, true);
    }
    public static Option<T> None() => new Option<T>(default!, false);
    
    public T Unwrap()
    {
        if (!_hasValue)
            throw new InvalidOperationException("Cannot unwrap None value");
        return _value;
    }
    public T Expect(string message)
    {
        if (!_hasValue)
            throw new InvalidOperationException(message);
        return _value;
    }
    public T UnwrapOr(T defaultValue) => _hasValue ? _value : defaultValue;
    public T UnwrapOrElse(Func<T> provider) => _hasValue ? _value : provider();

    public bool IsSome() => _hasValue;
    public bool IsNone() => !_hasValue;

    // Преобразование значений
    public Option<TResult> Map<TResult>(Func<T, TResult> mapper) where TResult : notnull
    {
        if (!_hasValue)
            return Option<TResult>.None();
        return Option<TResult>.Some(mapper(_value));
    }

    // Операторы преобразования
    public static implicit operator Option<T>(T value) =>
        value == null ? None() : Some(value);

    // Методы для сравнения
    public override bool Equals(object? obj)
    {
        if (obj is Option<T> other)
        {
            if (!_hasValue && !other._hasValue)
                return true;
            if (_hasValue && other._hasValue)
                return EqualityComparer<T>.Default.Equals(_value, other._value);
        }
        return false;
    }

    public override int GetHashCode()
    {
        if (!_hasValue)
            return 0;
        return _value?.GetHashCode() ?? 0;
    }

    // Строковое представление
    public override string ToString() =>
        _hasValue ? $"Some({_value})" : "None";
}