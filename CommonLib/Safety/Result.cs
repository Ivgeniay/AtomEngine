

public readonly struct Result<ResultT, ErrorT> where ErrorT : Error
{
    private readonly ResultT _value;
    private readonly ErrorT _error;
    private readonly bool _hasValue;

    public Result(ResultT value)
    {
        _value = value;
        _error = default!;
        _hasValue = true;
    }

    public Result(ErrorT error)
    {
        _value = default!;
        _error = error;
        _hasValue = false;
    }

    public static Result<ResultT, ErrorT> Ok(ResultT value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value), "Cannot create Ok with null value");
        return new Result<ResultT, ErrorT>(value);
    }

    public static Result<ResultT, ErrorT> Err(ErrorT error)
    {
        if (error == null)
            throw new ArgumentNullException(nameof(error), "Cannot create Err with null error");
        return new Result<ResultT, ErrorT>(error);
    }

    public ResultT Unwrap()
    {
        if (!_hasValue)
        {
            throw _error!;
        }
        return _value;
    }

    public ResultT Expect(string message)
    {
        if (!_hasValue)
        {
            ErrorT er = (ErrorT)Activator.CreateInstance(typeof(ErrorT), message)!;
            throw er;
        }
        return _value;
    }

    public ResultT UnwrapOrElse(Func<ResultT> action) => _hasValue ? _value : action();
    public ResultT UnwrapOr(ResultT default_v) => _hasValue ? _value : default_v; 

    public ResultT? UnwrapOrNone()
    {
        return _hasValue ? _value : default;
    }

    public Option<ResultT> Ok() => _hasValue ? Option<ResultT>.Some(_value) : Option<ResultT>.None();
    public Option<ErrorT> Err() => _hasValue ? Option<ErrorT>.None() : Option<ErrorT>.Some(_error);

    public Result<TNew, ErrorT> Map<TNew>(Func<ResultT, TNew> mapper)
    {
        if (!_hasValue)
            return Result<TNew, ErrorT>.Err(_error);
        return Result<TNew, ErrorT>.Ok(mapper(_value));
    }
    public Result<ResultT, TNewError> MapErr<TNewError>(Func<ErrorT, TNewError> mapper)
        where TNewError : Error
    {
        if (_hasValue)
            return Result<ResultT, TNewError>.Ok(_value);
        return Result<ResultT, TNewError>.Err(mapper(_error));
    }

    public bool IsOk() => _hasValue;
    public bool IsErr() => !_hasValue;

    public override bool Equals(object? obj)
    {
        if (obj is Result<ResultT, ErrorT> other)
        {
            if (_hasValue && other._hasValue)
                return EqualityComparer<ResultT>.Default.Equals(_value, other._value);
            if (!_hasValue && !other._hasValue)
                return EqualityComparer<ErrorT>.Default.Equals(_error, other._error);
        }
        return false;
    }
    public override int GetHashCode()
    {
        if (_hasValue)
            return _value?.GetHashCode() ?? 0;
        return _error?.GetHashCode() ?? 0;
    }
    public override string ToString() =>
        _hasValue ? $"Ok({_value})" : $"Err({_error})";
}



