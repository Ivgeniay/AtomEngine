using System.Text.Json.Nodes;
using AtomEngine.Serialize;
using AtomEngine.Scenes;
using AtomEngine.Math;

namespace AtomEngine
{
    public abstract class BaseComponent : ISerializable
    {
        public string ID { get; internal set; } = Guid.NewGuid().ToString();
        public AtomObject AtomObject { get; internal set; } 

        public BaseComponent() {}

        public virtual void Start() { }
        public virtual void Awake() { }
        public virtual void Update() { }
        public virtual void FixedUpdate() { }
        public virtual void OnDestroy() { }
        public virtual void OnEnable() { }
        public virtual void OnDisable() { }
        public virtual void Render() { }
        public virtual void OnUnload() { }
        public virtual void Dispose() { }
        public virtual void WindowResize(Vector2D<int> size) { }

        public T Instantiale<T>() where T : AtomObject => AtomObject.Instantiale<T>();

        public abstract JsonObject OnSerialize();
        public abstract void OnDeserialize(JsonObject json);
    }
}
