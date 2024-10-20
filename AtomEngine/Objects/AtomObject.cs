using System.Text.Json.Nodes;
using AtomEngine.Serialize; 
using AtomEngine.Scenes;
using System.Text.Json;
using AtomEngine.Math;
using AtomEngine.Services;
using AtomEngine.Diagnostic;
using Microsoft.Extensions.DependencyInjection;

namespace AtomEngine
{
    public class AtomObject : ISerializable, IDisposable
    {
        public string ID { get; internal set; } = Guid.NewGuid().ToString();
        public TransformComponent Transform { get
            {
                if (_transform == null)
                {
                    _transform = this.GetComponent<TransformComponent>();
                    if (_transform == null) _transform = AddComponent(new TransformComponent()); 
                }
                return _transform;
            }
        } 
        private TransformComponent _transform;
        internal int Length => componentsStorage.Count;
        protected List<Diction> componentsStorage = new List<Diction>();
        protected readonly AtomObjectContainer _diContainer;
        protected readonly ILogger? _logger;
        protected readonly Scene _scene;

        public AtomObject(SceneDIContainer sceneDIContainer, ILogger logger = null) 
        { 
            _diContainer = new AtomObjectContainer(sceneDIContainer);
            ContainerSetup(_diContainer.GetServiceCollection());
            _diContainer.BuildContainer();

            _scene = _diContainer.GetService<Scene>();
            _transform = AddComponent(new TransformComponent());
        } 

        private void ContainerSetup(IServiceCollection sceneServicesCollection)
        {
            sceneServicesCollection
                .AddSingleton<AtomObjectContainer>(this._diContainer)
                .AddSingleton<AtomObject, AtomObject>(options => this);
        }

        public virtual void Start() => componentsStorage.ForEach(e => e?.Component?.Start());
        public virtual void Awake() => componentsStorage.ForEach(e => e?.Component?.Awake()); 
        public virtual void Update() => componentsStorage.ForEach(e => e?.Component?.Update()); 
        public virtual void FixedUpdate() => componentsStorage.ForEach(e => e?.Component?.FixedUpdate()); 
        public virtual void OnDestroy() => componentsStorage.ForEach(e => e?.Component?.OnDestroy()); 
        public virtual void OnEnable() => componentsStorage.ForEach(e => e?.Component?.OnEnable()); 
        public virtual void OnDisable() => componentsStorage.ForEach(e => e?.Component?.OnDisable()); 
        public virtual void Render() => componentsStorage.ForEach(e => e?.Component?.Render());
        public virtual void OnUnload() => componentsStorage.ForEach(e => e?.Component?.OnUnload());
        public virtual void WindowResize(Vector2D<int> size) => componentsStorage.ForEach(e => e?.Component?.WindowResize(size));
        public T Instantiale<T>() where T : AtomObject => _scene.Instantiate<T>();


        public T AddComponent<T>(T component) where T : BaseComponent
        {
            if (component == null) throw new ArgumentNullException(nameof(component));

            string typeStr = component.GetType().FullName;
            componentsStorage.Add(new Diction { Type = typeStr, Component = component });
            return component;
        }

        public T? GetAssignableComponent<T>() where T : BaseComponent
        {
            var result = componentsStorage
                .FirstOrDefault(e => typeof(T).IsAssignableFrom(e?.Component?.GetType()));
            return result == null ? null : (T)result.Component;
        }

        public T? GetComponent<T>() where T : BaseComponent
        { 
            var result = componentsStorage
                .FirstOrDefault(e => e.Type == typeof(T).FullName);
            return result == null ? null : (T)result.Component;
        }

        public IEnumerable<T> GetComponents<T>() where T : BaseComponent
        {
            return componentsStorage
                    .Where(e => e.Component is T)
                    .Select(e => (T)e.Component);
        }

        public void RemovedComponents<T>() where T : BaseComponent
        { 
            componentsStorage.RemoveAll(x => x.Type == typeof(T).FullName);
        }

        public void Dispose() {
            componentsStorage.ForEach(e => e.Component?.Dispose());
        }

        public IDisposable AsDisposable() => this;

        public JsonObject OnSerialize()
        {
            return new JsonObject
            {
                ["ID"] = ID,
                ["componentsStorage"] = new JsonArray(componentsStorage.Select(c => c.OnSerialize()).ToArray())
            };
        }

        public void OnDeserialize(JsonObject json)
        {
            ID = json["ID"].GetValue<string>();
            componentsStorage = json["componentsStorage"].AsArray()
                .Select(j => {
                    var diction = new Diction();
                    diction.OnDeserialize(j.AsObject());
                    return diction;
                })
                .ToList();
        }

        protected class Diction : ISerializable
        {
            public string Type { get; set; } = string.Empty;
            public BaseComponent Component { get; set; } = default(BaseComponent);

            public JsonObject OnSerialize()
            {
                return new JsonObject
                {
                    ["Type"] = Type,
                    ["Component"] = Component.OnSerialize()
                };
            }

            public void OnDeserialize(JsonObject json)
            {
                Type = json["Type"].GetValue<string>();
                Type componentType = System.Type.GetType(Type);
                Component = (BaseComponent)JsonSerializer.Deserialize(json["Component"], componentType);
            }
        }
    }

    
}
