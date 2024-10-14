using AtomEngine.Scenes;
using AtomEngine.Serialize;
using System.Text.Json.Nodes;

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

        public T Instantiale<T>(params object[] constructParams) where T : AtomObject =>
            AtomObject.Instantiale<T>(constructParams);

        public abstract JsonObject OnSerialize();
        public abstract void OnDeserialize(JsonObject json);
    }
}
