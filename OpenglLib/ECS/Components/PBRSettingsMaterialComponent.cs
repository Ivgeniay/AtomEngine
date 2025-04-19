using System.Numerics;
using System.Runtime.InteropServices;
using AtomEngine;

namespace OpenglLib
{
    public struct PBRSettingsMaterialComponent : IComponent
    {
        public Entity Owner { get; set; }

        [DefaultVector3(1.0f, 1.0f, 1.0f)]
        public Vector3 Albedo;

        [DefaultFloat(0.0f)]
        [Range(0.0f, 1.0f)]
        public float Metallic;

        [DefaultFloat(0.5f)]
        [Range(0.0f, 1.0f)]
        public float Roughness;

        [DefaultFloat(1.0f)]
        [Range(0.0f, 1.0f)]
        public float AmbientOcclusion;

        [DefaultFloat(1.0f)]
        [Range(0.0f, 1.0f)]
        public float Alpha;

        [DefaultBool(true)]
        public bool UseAlbedoMap;
        [DefaultBool(false)]
        public bool UseNormalMap;
        [DefaultBool(false)]
        public bool UseMetallicMap;
        [DefaultBool(false)]
        public bool UseRoughnessMap;
        [DefaultBool(false)]
        public bool UseAoMap;
        [DefaultBool(false)]
        public bool CalculateViewDirPerPixel;

        public bool IsDirty;

        public PBRSettingsMaterialComponent(Entity entity)
        {
            Owner = entity;
            Albedo = new Vector3(1.0f, 1.0f, 1.0f);
            Metallic = 0.5f;
            Roughness = 0.5f;
            AmbientOcclusion = 1.0f;
            Alpha = 1.0f;
            UseAlbedoMap = true;
            CalculateViewDirPerPixel = false;
            IsDirty = true;
        }

        public void MakeClean()
        {
            IsDirty = false;
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 28)]
    public struct PBRMaterialData
    {
        [FieldOffset(0)]
        public Vector3 Albedo;

        [FieldOffset(12)]
        public float Metallic;

        [FieldOffset(16)]
        public float Roughness;

        [FieldOffset(20)]
        public float Ao;

        [FieldOffset(24)]
        public float Alpha;
    }

    [StructLayout(LayoutKind.Explicit, Size = 64)]
    public struct MaterialUboData
    {
        [FieldOffset(0)]
        public PBRMaterialData Material;

        [FieldOffset(32)]
        public bool UseAlbedoMap;

        [FieldOffset(36)]
        public bool UseNormalMap;

        [FieldOffset(40)]
        public bool UseMetallicMap;

        [FieldOffset(44)]
        public bool UseRoughnessMap;

        [FieldOffset(48)]
        public bool UseAoMap;

        [FieldOffset(52)]
        public bool CalculateViewDirPerPixel;
    }
}
