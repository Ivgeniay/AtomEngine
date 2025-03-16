namespace AtomEngine
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Field, AllowMultiple = false)]
    public class ReadOnlyAttribute : Attribute
    {
    }
}
