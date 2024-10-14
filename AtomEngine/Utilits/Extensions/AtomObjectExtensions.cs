namespace AtomEngine
{
    public static class AtomObjectExtensions
    {
        public static T AddComponent<T>(this AtomObject atomObject) where T : BaseComponent, new()
        {
            T component = new T();
            component.AtomObject = atomObject;
            atomObject.AddComponent<T>(component);
            component.Awake();
            component.Start();
            return component;
        }

        public static T AddComponent<T>(this AtomObject atomObject, params object[] parameters) where T : BaseComponent
        {
            T component = (T)Activator.CreateInstance(typeof(T), parameters);
            component.AtomObject = atomObject;
            atomObject.AddComponent<T>(component);
            component.Awake();
            component.Start();
            return component;
        }
    }
}
