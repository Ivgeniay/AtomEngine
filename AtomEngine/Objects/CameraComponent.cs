using AtomEngine.Entities;
using AtomEngine.Math;
using System.Text.Json.Nodes;

namespace AtomEngine
{
    public class CameraComponent : BaseComponent
    {
        public static CameraComponent? Main { get; private set; }

        public Matrix4x4 SP;
        public Vector2D<int> Resolution { get; private set; }
        public bool IsReady { get; private set; }
        public float deathNear = 0.1f;
        public float deathFar = 100.0f;

        public CameraComponent() { }

        public void Initialize(Vector2D<int> resolution)
        {
            Main = this;

            SP = Matrix4x4.Identity();
            Resolution = resolution;

            IsReady = true;
        }



        public override JsonObject OnSerialize()
        {
            return new JsonObject();
        }

        public override void OnDeserialize(JsonObject json) { }
    }
}
