using System.Collections.Generic;
using System.Reflection;
using System;

namespace Editor
{
    internal class UserAssemblyObjectTracker
    {
        private static WeakReferenceList<object> _trackedObjects = new WeakReferenceList<object>();
        private static Dictionary<Assembly, WeakReferenceList<object>> _assemblyObjects =
            new Dictionary<Assembly, WeakReferenceList<object>>();

        public static void TrackObject(object obj)
        {
            if (obj == null) return;

            var assembly = obj.GetType().Assembly;
            if (!_assemblyObjects.TryGetValue(assembly, out var list))
            {
                list = new WeakReferenceList<object>();
                _assemblyObjects[assembly] = list;
            }

            list.Add(obj);
            _trackedObjects.Add(obj);
        }

        public static void ClearReferencesForAssembly(Assembly assembly)
        {
            if (_assemblyObjects.TryGetValue(assembly, out var list))
            {
                foreach (var weakRef in list.GetReferences())
                {
                    if (weakRef.Target is IDisposable disposable)
                    {
                        try { disposable.Dispose(); } catch { }
                    }
                    weakRef.Target = null;
                }
                _assemblyObjects.Remove(assembly);
            }
        }

        public class WeakReferenceList<T> where T : class
        {
            private List<WeakReference> _refs = new List<WeakReference>();

            public void Add(T obj)
            {
                _refs.Add(new WeakReference(obj));
            }

            public void Cleanup()
            {
                _refs.RemoveAll(r => !r.IsAlive);
            }

            public List<WeakReference> GetReferences()
            {
                return _refs;
            }
        }
    }
}
