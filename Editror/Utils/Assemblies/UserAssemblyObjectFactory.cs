using System.Linq;
using AtomEngine;
using System;

namespace Editor
{
    internal class UserAssemblyObjectFactory
    {
        private static EditorAssemblyManager _amInstance;
        private static EditorAssemblyManager _assemblyManager
        {
            get
            {
                if (_amInstance == null) _amInstance = ServiceHub.Get<EditorAssemblyManager>();
                return _amInstance;
            }
        }

        public static T CreateInstance<T>(Type type) where T : class
        {
            try
            {
                bool isUserType = ValidateUserAssemblyScript(type);
                if (!isUserType)
                {
                    throw new TypeError($"Type {type.Name} is not user type");
                }
                return Instantiate(type) as T;
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при создании объекта типа {type.FullName}: {ex.Message}");
                return null;
            }
        }

        public static object CreateInstance(Type type)
        {
            try
            {
                bool isUserType = ValidateUserAssemblyScript(type);
                if (!isUserType)
                {
                    throw new TypeError($"Type {type.Name} is not user type");
                }

                return Instantiate(type);
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при создании объекта типа {type.FullName}: {ex.Message}");
                return null;
            }
        }

        public static object CreateInstance(string typeFullName)
        {
            Type type = _assemblyManager.FindType(typeFullName, true);

            bool isUserType = ValidateUserAssemblyScript(type);
            if (!isUserType)
            {
                throw new TypeError($"Type {type.Name} is not user type");
            }

            return Instantiate(type);
        }

        private static object Instantiate(Type type)
        {
            var instance = Activator.CreateInstance(type);
            UserAssemblyObjectTracker.TrackObject(instance);
            return instance;
        }

        private static bool ValidateUserAssemblyScript(Type type)
        {
            return _assemblyManager.GetAssembly(TAssembly.UserScript).GetTypes().Any(e => e == type);
        }
    }
}
