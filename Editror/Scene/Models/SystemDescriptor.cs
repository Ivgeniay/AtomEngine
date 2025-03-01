using AtomEngine;
using Newtonsoft.Json;
using System;

namespace Editor
{
    [JsonObject(ItemTypeNameHandling = TypeNameHandling.All)]
    internal class SystemDescriptor
    {
        private Type _systemType;

        public Type SystemType
        {
            get => _systemType;
            set
            {
                if (value != null && !typeof(ICommonSystem).IsAssignableFrom(value))
                    throw new ArgumentException("Type must implement ISystem interface");
                _systemType = value;
            }
        }
    }

}
