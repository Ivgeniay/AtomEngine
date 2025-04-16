using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace AtomEngine
{
    public partial struct LightComponent : IComponent
    {
        public Entity Owner { get; set; }
        public LightType Type;

        [DefaultVector3(1,1,1)]
        public Vector3 Color;
        [DefaultVector3(1,1,1)]
        public float Intensity;
        [DefaultFloat(1f)]
        public float Enabled;
        [DefaultBool(true)]
        public bool CastShadows;
        public int LightId;

        public Matrix4x4 LightSpaceMatrix;

        [DefaultFloat(10f)]
        public float Radius;
        [DefaultFloat(10f)]
        public float FalloffExponent;

        public bool IsDirty;
        public LightComponent(Entity entity)
        {
            Owner = entity;
        }
        public void MakeClean()
        {
            IsDirty = false;
        }
    }

    public enum LightType
    {
        Directional = 0,
        Point = 1,
    }

    [StructLayout(LayoutKind.Explicit, Size = 128)]
    public struct DirectionalLightData
    {
        [FieldOffset(0)]
        public Vector3 Direction;

        [FieldOffset(16)]
        public Vector3 Color;

        [FieldOffset(28)]
        public float Intensity;

        [FieldOffset(32)]
        public float CastShadows;

        [FieldOffset(48)]
        public Matrix4x4 LightSpaceMatrix;

        [FieldOffset(112)]
        public float Enabled;

        [FieldOffset(116)]
        public int LightId;
    }

    [StructLayout(LayoutKind.Explicit, Size = 48)]
    public struct PointLightData
    {
        [FieldOffset(0)]
        public Vector3 Position;

        [FieldOffset(16)]
        public Vector3 Color;

        [FieldOffset(28)]
        public float Intensity;

        [FieldOffset(32)]
        public float Radius;

        [FieldOffset(36)]
        public float CastShadows;

        [FieldOffset(40)]
        public float FalloffExponent;

        [FieldOffset(44)]
        public float Enabled;
    }

    [StructLayout(LayoutKind.Explicit, Size = 944)]
    public struct LightsUboData
    {

        [FieldOffset(0)]
        public DirectionalLightData DirectionalLight0;

        [FieldOffset(128)]
        public DirectionalLightData DirectionalLight1;

        [FieldOffset(256)]
        public DirectionalLightData DirectionalLight2;

        [FieldOffset(384)]
        public DirectionalLightData DirectionalLight3;

        [FieldOffset(512)]
        public PointLightData PointLight0;

        [FieldOffset(560)]
        public PointLightData PointLight1;

        [FieldOffset(608)]
        public PointLightData PointLight2;

        [FieldOffset(656)]
        public PointLightData PointLight3;

        [FieldOffset(704)]
        public PointLightData PointLight4;

        [FieldOffset(752)]
        public PointLightData PointLight5;

        [FieldOffset(800)]
        public PointLightData PointLight6;

        [FieldOffset(848)]
        public PointLightData PointLight7;

        [FieldOffset(896)]
        public Vector3 AmbientColor;

        [FieldOffset(908)]
        public float AmbientIntensity;

        [FieldOffset(912)]
        public int NumDirectionalLights;

        [FieldOffset(916)]
        public int NumPointLights;

        [FieldOffset(920)]
        public float ShadowBias;

        [FieldOffset(924)]
        public int PcfKernelSize;

        [FieldOffset(928)]
        public float ShadowIntensity;
    }

}
