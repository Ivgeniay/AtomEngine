namespace EngineLib
{
    /// <summary>
    /// Dont show this component in inspector
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public class HideToInspectorAttribute : Attribute
    {
    }

    /// <summary>
    /// Dont show this component in inpector search
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public class HideInspectorSearchAttribute : Attribute
    {
    }

    /// <summary>
    /// Dont show delete button for this componen
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public class HideCloseAttribute : Attribute
    {
    }
}
