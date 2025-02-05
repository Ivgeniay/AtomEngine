using System.Numerics;
using EngineLib;



public class ArchetypePoolTests 
{
    
}

public static class SpanAssert
{
    public static void Single<T>(ReadOnlySpan<T> span)
    {
        Assert.Equal(1, span.Length);
    }

    public static void Empty<T>(ReadOnlySpan<T> span)
    {
        Assert.Equal(0, span.Length);
    }

    public static void Count<T>(ReadOnlySpan<T> span, int expected)
    {
        Assert.Equal(expected, span.Length);
    }
}