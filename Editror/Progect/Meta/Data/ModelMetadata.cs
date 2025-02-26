namespace Editor
{
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
}
