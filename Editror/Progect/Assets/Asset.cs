namespace Editor
{
    public abstract class Asset
    {
        public string Guid { get; set; } = System.Guid.NewGuid().ToString();
        public virtual string Name { get; set; } = "Asset";
    }
}
