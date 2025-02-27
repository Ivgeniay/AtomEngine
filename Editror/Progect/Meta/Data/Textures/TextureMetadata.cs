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
}
