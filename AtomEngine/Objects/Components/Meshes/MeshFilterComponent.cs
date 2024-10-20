using System.Text.Json.Nodes;

namespace AtomEngine
{
    public sealed class MeshFilterComponent : BaseComponent
    {
        public Mesh? Mesh;
        public MeshFilterComponent() : base() { }
        

        public override void OnDeserialize(JsonObject json)
        { 

        }

        public override JsonObject OnSerialize()
        {
            return new JsonObject();
        }
    }
}
