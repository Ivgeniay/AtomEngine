using System.Collections.Generic;
using System;

namespace Editor
{
    internal class WorldReference
    {
        public string WorldGuid { get; set; } = string.Empty;
        public string WorldName { get; set; } = string.Empty;
    }

    internal class AssetDependency
    {
        public string AssetGuid { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty;
        public string AssetPath { get; set; } = string.Empty;
        public bool IsEmbedded { get; set; } = false;
    }

    internal class EntityReference
    {
        public string EntityGuid { get; set; } = string.Empty;
        public uint EntityId { get; set; } = 0;
        public uint EntityVersion { get; set; } = 0;
        public string EntityName { get; set; } = string.Empty;
    }

    internal class SystemReference
    {
        public string SystemGuid { get; set; } = Guid.NewGuid().ToString();
        public string SystemType { get; set; } = string.Empty;
    }

    internal class ComponentData
    {
        public string Type { get; set; } = string.Empty;
        public string Assembly {  get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
    }

    public class AssetMetadata
    {
        public string Guid { get; set; } = System.Guid.NewGuid().ToString();
        public MetadataType AssetType { get; set; } = MetadataType.Unknown;
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
        public int Version { get; set; } = 1;
        public List<string> Dependencies { get; set; } = new List<string>();
        public List<string> Tags { get; set; } = new List<string>();
        public Dictionary<string, object> ImportSettings { get; set; } = new Dictionary<string, object>();
        public string ContentHash { get; set; } = string.Empty;
    }

    public enum TextureFilterMode
    {
        Bilinear, Point, Trilinear
    }
    public enum TextureWrapMode
    {
        Repeat, Clamp, Mirror
    }
    public enum TextureCompressionFormat
    {
        None, DXT1, DXT5, BC7, ETC2, ASTC, Automatic
    }
    public class TextureMetadata : AssetMetadata
    {
        public TextureMetadata()
        {
            AssetType = MetadataType.Texture;
        }

        public bool GenerateMipmaps { get; set; } = true;
        public bool sRGB { get; set; } = true;
        public int MaxSize { get; set; } = 2048;
        public TextureFilterMode FilterMode { get; set; } = TextureFilterMode.Bilinear;
        public int AnisoLevel { get; set; } = 1;
        public TextureWrapMode WrapMode { get; set; } = TextureWrapMode.Repeat;
        public TextureCompressionFormat CompressionFormat { get; set; } = TextureCompressionFormat.Automatic;
        public bool CompressTexture { get; set; } = true;
        public float CompressionQuality { get; set; } = 50;
        public bool AlphaIsTransparency { get; set; } = false;

        // Текстуры нормалей
        public bool IsNormalMap { get; set; } = false;

        // Спрайты
        public bool IsSpriteSheet { get; set; } = false;
        public int SpritePixelsPerUnit { get; set; } = 100;
        public bool GenerateSpriteMesh { get; set; } = true;
    }

    public class ModelMetadata : AssetMetadata
    {
        public ModelMetadata()
        {
            AssetType = MetadataType.Model;
        }

        // Общие настройки импорта
        public float Scale { get; set; } = 1.0f;
        public string ImportBlendShapes { get; set; } = "All"; // None, All, Selected
        public bool ImportVisibility { get; set; } = true;
        public bool ImportCameras { get; set; } = true;
        public bool ImportLights { get; set; } = true;

        // Геометрия и меши
        public bool OptimizeMesh { get; set; } = true;
        public bool GenerateLightmapUVs { get; set; } = false;
        public bool WeldVertices { get; set; } = true;
        public bool CalculateNormals { get; set; } = true;
        public bool CalculateTangents { get; set; } = true;
        public bool SwapUVs { get; set; } = false;
        public bool FlipUVs { get; set; } = false;

        // Материалы
        public bool ImportMaterials { get; set; } = true;
        public string MaterialNamingMode { get; set; } = "FromModel"; // FromModel, Model_Material
        public string MaterialSearchMode { get; set; } = "Local"; // Local, RecursiveUp, All

        // Анимация
        public bool ImportAnimations { get; set; } = true;
        public bool ImportSkins { get; set; } = true;
        public bool ResampleCurves { get; set; } = true;
        public bool OptimizeAnimations { get; set; } = true;
        public float AnimationCompressionError { get; set; } = 0.5f;
        public string AnimationCompression { get; set; } = "Optimal"; // Off, KeyframeReduction, Optimal
    }

    public class AudioMetadata : AssetMetadata
    {
        public AudioMetadata()
        {
            AssetType = MetadataType.Audio;
        }

        // Общие настройки
        public bool ForceToMono { get; set; } = false;
        public bool Normalize { get; set; } = false;
        public bool LoadInMemory { get; set; } = false;
        public bool Preload { get; set; } = true;
        public bool AmbientSound { get; set; } = false;

        // Качество и сжатие
        public bool Compressed { get; set; } = true;
        public string CompressionFormat { get; set; } = "Vorbis"; // Vorbis, MP3, ADPCM, PCM
        public int Quality { get; set; } = 70;
        public int SampleRate { get; set; } = 44100;

        // 3D-звук
        public bool Enable3D { get; set; } = false;
        public float DopplerFactor { get; set; } = 1.0f;
        public float Rolloff
        {
            get; set;
        }
    }
}
