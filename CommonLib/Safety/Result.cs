

public struct Result<ResultT, ErrorT> where ErrorT : Error
{
    private readonly ResultT? _value;
    private readonly ErrorT? _error;
    private readonly bool _hasValue;

    public Result(ResultT value)
    {
        _value = value;
        _error = null;
        _hasValue = true;
    }

    public Result(ErrorT error)
    {
        _value = default!;
        _error = error;
        _hasValue = false;
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

    public ResultT UnwrapOrElse(Func<ResultT> action)
    {
        if (!_hasValue)
        {
            return action();
        }
        return _value;
    }
    public ResultT UnwrapOr(ResultT default_v)
    {
        if (!_hasValue)
        {
            return default_v;
        }
        return _value;
    }

    public ResultT? UnwrapOrNone()
    {
        return _hasValue ? _value : default;
    }

    public bool IsOk()
    {
        return _hasValue;
    }
}



