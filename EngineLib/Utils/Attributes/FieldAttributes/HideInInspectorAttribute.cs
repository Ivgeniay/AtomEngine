namespace AtomEngine
{
    /// <summary>
    /// Dont show this field in inspector
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class HideInInspectorAttribute : Attribute
    {
    }

    /// <summary>
    /// Show private field in inspector
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ShowInInspectorAttribute : Attribute
    {
    }
}
