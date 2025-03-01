using AtomEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SmokeTesting
{
    public partial struct SampleComponent : IComponent
    {
        // This will be ignored (primitive type)
        public float Scale { get; set; }

        // This will get a corresponding GUID field (Vector3 is not a primitive)
        public Vector3 Position { get; set; }

        // This will get a corresponding GUID field
        public Vector3 Rotation { get; set; }

        // This will be ignored (string type)
        public string Name { get; set; }

        // This will get a corresponding GUID field
        public Matrix4x4 WorldMatrix;

        // This property will be ignored because it doesn't have both getter and setter
        public bool IsVisible { get; }

        // This will be ignored (static member)
        public static int InstanceCount;

        // IComponent implementation
        public Entity Owner { get; set; }
    }
}
