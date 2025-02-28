using Silk.NET.OpenGL;

namespace Editor
{
    internal class TextureMetadata : AssetMetadata
    {
        public TextureMetadata()
        {
            AssetType = MetadataType.Texture;
        }

        public bool GenerateMipmaps { get; set; } = true;
        public bool sRGB { get; set; } = true;
        public int MaxSize { get; set; } = 2048;
        public TextureMinFilter MinFilter { get; set; } = TextureMinFilter.Nearest;
        public TextureMagFilter MagFilter { get; set; } = TextureMagFilter.Linear;
        public int AnisoLevel { get; set; } = 1;
        public Silk.NET.OpenGL.TextureWrapMode WrapMode { get; set; } = Silk.NET.OpenGL.TextureWrapMode.Repeat;
        public InternalFormat CompressionFormat { get; set; } = InternalFormat.Rgba8;
        public TextureTarget TextureTarget { get; set; } = TextureTarget.Texture2D;
        public Silk.NET.Assimp.TextureType TextureType { get; set; } = Silk.NET.Assimp.TextureType.Diffuse;
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
}
