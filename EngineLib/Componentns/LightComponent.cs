using System;
using System.Numerics;

namespace AtomEngine
{
    public partial struct LightComponent : IComponent
    {
        public Entity Owner { get ; set ; }

        //Directional
        public Vector3 Direction;

        //Point
        public float Radius;
        public float FalloffExponent;

        //Common
        public Vector3 Color;
        public float Intensity;
        public float CastShadows;
        public Matrix4x4 LightSpaceMatrix;
        public float Enabled;
    }

    public enum LightType
    {
        DirectionalLight,
        PointLight
    }
}
