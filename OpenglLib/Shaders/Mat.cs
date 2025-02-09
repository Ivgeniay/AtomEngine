using System.Reflection;
using Newtonsoft.Json;
using Silk.NET.OpenGL;
using AtomEngine;
using Silk.NET.Maths;

namespace OpenglLib
{
    public class Mat : Shader
    { 

        public Mat(GL gl) : base(gl) {
        }

        protected void SetLocation()
        {

            var type = GetType();
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Default | BindingFlags.SetField);
            var filteredProps = props.Where(e => _uniformLocations.ContainsKey(e.Name)); 
            foreach (var item in filteredProps)
            {
                _uniformLocations.TryGetValue(item.Name, out int location);
                var locationField = type.GetProperty(item.Name + "Location", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.Public | BindingFlags.GetField | BindingFlags.SetField);
                if (locationField != null) locationField.SetValue(this, (int)location);
            }
            DebLogger.Info(JsonConvert.SerializeObject(this));
        }
    }
}
