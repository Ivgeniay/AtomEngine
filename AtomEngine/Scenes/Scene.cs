using System.Text.Json.Nodes;
using AtomEngine.Diagnostic;
using AtomEngine.Serialize;
using AtomEngine.Utilits; 
using AtomEngine.Math;
using AtomEngine.Reactive;

namespace AtomEngine.Scenes
{
    public abstract class Scene : ISerializable
    {
        public string ID { get; internal set; } = Guid.CreateVersion7().ToString();
        public string Name { get; private set; } = string.Empty;
        public bool IsLoaded { get; private set; }


        private event VoidDelegate? OnLoad;
        private event VoidDelegate? OnUnload;
        private event Vector2DDelegate? OnResize;
        private event DoubleDelegate? OnUpdate;
        private event DoubleDelegate? OnRender;

        protected List<AtomObject> _objects = new List<AtomObject>();
        protected ReactiveProperty<bool> _isDirty = new ReactiveProperty<bool>(false);
        protected readonly ILogger? _logger; 

        public Scene(string name, ILogger logger = null)
        {
            Name = name;
            this._logger = logger;
        }

        internal void Load()
        {
            if (IsLoaded)
            {
                _logger?.LogWarning($"Scene {Name} already loaded");
                return;
            }
            _logger?.LogInformation($"Scene {Name} loading");

            OnLoad += OnLoadHandler;
            OnUnload += OnUnloadHandler;
            OnResize += OnResizeHandler;
            OnUpdate += OnUpdateHandler;
            OnRender += OnRenderHandler;

            IsLoaded = true;
            _logger?.LogInformation($"Scene {Name} loaded");
            OnLoad?.Invoke();

            _objects.ForEach(e => e.Awake());
            _logger?.LogInformation($"Scene {Name} objects awake");
            _objects.ForEach(e => e.Start());
            _logger?.LogInformation($"Scene {Name} objects started");

            Test();
        } 
        private void Test()
        {
            AtomObject go = Instantiate<AtomObject>();
            AtomObject go2 = Instantiate<AtomObject>();

            go.AddComponent<MeshFilterComponent>();
            go.AddComponent<MeshRendererComponent>();
             
            go2.AddComponent<MeshFilterComponent>();
            go2.AddComponent<MeshRendererComponent>();

            _logger.LogInformation($"GO transform: {go.Transform.AbsolutePositon}");

            var result = OnSerialize();
        } 
        internal void WindowResize(Vector2D<int> size, bool force = false)
        {
            if (!IsLoaded) return;
            _logger?.LogInformation($"Scene {Name} resize to {size}");
            OnResize?.Invoke(size);
        } 
        internal void Update(double delta)
        {
            if (!IsLoaded) return;
            OnUpdate?.Invoke(delta);
            _objects.ForEach(e => e.Update());
        } 
        internal void Render(double delta)
        {
            if (!IsLoaded) return;
            OnRender?.Invoke(delta);
            _objects.ForEach(e => e.Render());
        } 
        internal void Unload()
        {
            if (!IsLoaded)
            {
                _logger?.LogWarning($"Scene {Name} already unloaded");
                return;
            }

            OnLoad -= OnLoadHandler;

            OnUnload?.Invoke();
            OnUnload -= OnUnloadHandler;
            OnResize -= OnResizeHandler;
            OnUpdate -= OnUpdateHandler;
            OnRender -= OnRenderHandler;

            IsLoaded = false;
            _logger?.LogInformation($"Scene {Name} unloaded");
            _objects.ForEach(e => e.OnUnload());
            _logger?.LogInformation($"Scene {Name} objects unloaded");
        }

        #region ProtectedEvents
        protected abstract void OnRenderHandler(double value);
        protected abstract void OnUpdateHandler(double value);
        protected abstract void OnResizeHandler(Vector2D<int> value);
        protected abstract void OnUnloadHandler();
        protected abstract void OnLoadHandler();
        #endregion

        #region Objects
        public void AddObject(AtomObject obj)
        {
            _objects.Add(obj);
            _logger?.LogInformation($"Object {obj.ID} added to Scene {Name} with {obj.Length} components");
        } 
        public void RemoveObject(AtomObject obj)
        {
            _objects.Remove(obj);
            _logger?.LogInformation($"Object {obj.ID} removed from Scene {Name} with {obj.Length} components");
        } 
        public AtomObject Instantiate(string typeName, params object[] constructParams)
        {
            Type type = Type.GetType(typeName) ??
                        AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(a => a.GetTypes())
                            .FirstOrDefault(t => t.FullName == typeName);

            if (type == null)
            {
                _logger?.LogError($"Type '{nameof(typeName)}' not found (from  AtomObject Instantiate(string typeName, params object[] constructParams))");
                throw new ArgumentException($"Type '{typeName}' not found", nameof(typeName));
            }

            return Instantiate(type, constructParams);
        } 
        public AtomObject Instantiate(Type type, params object[] constructParams)
        {
            if (!typeof(AtomObject).IsAssignableFrom(type))
            {
                _logger?.LogError($"Type must be derived from AtomObject (from AtomObject Instantiate(Type type, params object[] constructParams))");
                throw new ArgumentException($"Type must be derived from AtomObject", nameof(type));
            }

            AtomObject obj = (AtomObject)Activator.CreateInstance(type, constructParams);
            obj.Scene = this;

            _logger?.LogInformation($"Object {obj.ID} instantiated from {type.FullName} on Scene {Name}");
            AddObject(obj);

            return obj;
        } 
        public T Instantiate<T>(params object[] constructParams) where T : AtomObject
        {
            var obj = (T)Activator.CreateInstance(typeof(T), constructParams);
            obj.Scene = this;

            _logger?.LogInformation($"Object {obj.ID} instantiated from {typeof(T).FullName} on Scene {Name}");
            AddObject(obj);

            return obj;
        }
        #endregion
        
        public void SetDirty() => _isDirty.Value = true; 
        public void ClearDirty() => _isDirty.Value = false;
        public void SafeScene()
        {
            if (!_isDirty.Value)
            {
                _logger?.LogInformation($"Scene {Name} is not dirty");
                return;
            }

            _logger?.LogInformation($"Scene {Name} is dirty and safe");
            _isDirty.Value = false;
        }

        public static bool operator ==(Scene a, Scene b) => a.Name == b.Name; 
        public static bool operator !=(Scene a, Scene b) => a.Name != b.Name; 

        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj is Scene scene)
            {
                return this == scene;
            }
            return false;
        }

        public override int GetHashCode() => Name.GetHashCode() + ID.GetHashCode(); 

        public virtual JsonObject OnSerialize()
        {
            JsonObject json = new JsonObject
            {
                ["ID"] = ID,
                ["Name"] = Name,
                ["Type"] = GetType().FullName,
                ["Objects"] = new JsonArray(_objects.Select(obj => obj.OnSerialize()).ToArray())
            };
            _logger?.LogInformation($"Scene {Name} serialized");
            return json;
        }

        public virtual void OnDeserialize(JsonObject json)
        {
            ID = json["ID"].GetValue<string>();
            Name = json["Name"].GetValue<string>();
            string type = json["Type"].GetValue<string>();
            _objects = json["Objects"].AsArray()
                .Select(j =>
                {
                    AtomObject ao = (AtomObject)Instantiate(type);
                    ao.OnDeserialize(j.AsObject());
                    return ao;
                })
                .ToList();
            _logger?.LogInformation($"Scene {Name} deserialized");
        }
    }
}
