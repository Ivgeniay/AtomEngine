using System.Text.Json.Nodes;

namespace AtomEngine
{
    public sealed class MeshRendererComponent : BaseComponent
    {
        public MeshRendererComponent() : base()
        {
        }

        public override void OnDeserialize(JsonObject json)
        { 
        }

        public override JsonObject OnSerialize()
        {
            return new JsonObject();
        }
    }
}
