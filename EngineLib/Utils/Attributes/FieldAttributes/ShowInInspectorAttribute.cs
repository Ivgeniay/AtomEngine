namespace AtomEngine
{
    /// <summary>
    /// Show private field in inspector
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ShowInInspectorAttribute : Attribute
    {
    }
}
