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
        public string SystemGuid { get; set; } = System.Guid.NewGuid().ToString();
        public string SystemType { get; set; } = string.Empty;
    }

    internal class ComponentData
    {
        public string Type { get; set; } = string.Empty;
        public string Assembly {  get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
    }
}
