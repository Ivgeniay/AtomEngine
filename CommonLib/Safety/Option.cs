public struct Option<TValue>
{
    private readonly Some<TValue>? Some;
    private readonly None? None; 

    public Option(TValue? value)
    {
        if (value == null) 
            None = new None();
        else
        {
            Some = new Some<TValue>(value);
        }
    }

    public Option(None none)
    {
        None = none;
    }

    public Option(Some<TValue> value)
    {
        if (value == null)
            throw new NullValue("Some value is null");

        Some = value;
    }

    public Option(Some value)
    {
        if (value == null)
            throw new NullValue("Some value is null");
        try
        {
            Some = new Some<TValue>((TValue)value.Value);
        }
        catch (Exception e)
        {
            throw new InvalidType($"Some value has invalid type.\n{e}");
        }
        finally
        {
            Some = null;
            None = new None();
        }
    }

    public TValue Unwrap()
    {
        if (Some == null) throw new NullValue("Option is None");
        return Some.Value;
    }

    public TValue Expect(string message)
    {
        if (Some == null)
        {
            throw new NullValue(message);
        }
        return Some.Value;
    }

    public TValue UnwrapOrElse(Func<TValue> action)
    {
        if (Some == null)
        {
            return action();
        }
        return Some.Value;
    }

    public TValue UnwrapOr(TValue default_v)
    {
        if (Some == null)
        {
            return default_v;
        }
        return Some.Value;
    }

    public bool IsSome()
    {
        return Some != null;
    }

    public bool IsNone()
    {
        return None != null;
    }
}

public class Some
{
    public readonly object Value;
    public Some(object v)
    {
        Value = v;
    }
}

public class Some<T>
{
    public readonly T Value;
    public Some(T v)
    {
        Value = v;
    }
}

public class None{}