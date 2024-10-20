using AtomEngine.Diagnostic;
using AtomEngine.Math;

namespace AtomEngine.Scenes
{
    public sealed class SceneDisposer : ISceneDisposer
    {
        private readonly List<Scene> _scenes = new List<Scene>();
        private Scene? _currentScene;
        public Scene? CurrentScene => _currentScene;
        private readonly ILogger? logger;

        public SceneDisposer(ILogger logger = null)
        {
            this.logger = logger;
        }

        public void AddScene(Scene scene)
        {
            if (!_scenes.Contains(scene))
            {
                _scenes.Add(scene);
                logger?.Log($"Scene {scene.ID} added to SceneDisposer");
            }
        }

        public void AddManyScenes(IEnumerable<Scene> scenes)
        {
            foreach (var scene in scenes)
            {
                AddScene(scene);
                logger?.Log($"Scene {scene.ID} added to SceneDisposer");
            }
        }

        public void RemoveScene(Scene scene)
        {
            if (_scenes.Contains(scene))
            {
                if (_currentScene == scene)
                {
                    _currentScene = null;
                    logger?.Log($"Scene {scene.ID} removed from SceneDisposer");
                }

                scene.Unload(); 
                _scenes.Remove(scene);
            }
        }

        public void RemoveScene(string id)
        {
            Scene? scene = _scenes.Find(scene => scene.ID == id);
            if (scene != null)
            {
                RemoveScene(scene);
                logger?.Log($"Scene {scene.ID} removed from SceneDisposer");
            }
        }

        public void RemoveManyScenes(IEnumerable<Scene> scenes)
        {
            foreach (var scene in scenes)
            {
                RemoveScene(scene);
                logger?.Log($"Scene {scene.ID} removed from SceneDisposer");
            } 
        }

        public void LoadScene(string id)
        {
            if (_currentScene?.ID == id && _currentScene.IsLoaded) return;

            _currentScene?.Unload();

            _currentScene = _scenes.Find(scene => scene.ID == id);
            _currentScene?.Load();
        }

        public void LoadScene(Scene scene)
        {
            if (_currentScene?.ID == scene.ID && _currentScene.IsLoaded) return;

            _currentScene?.Unload();

            _currentScene = _scenes.FirstOrDefault(scene => scene.ID == scene.ID);
            if (_currentScene == null)
            {
                _scenes.Add(scene);
                _currentScene = scene;
            }
            _currentScene?.Load();
        }

        public void ResizeCurrentScene(Vector2D<int> size) =>
            _currentScene?.WindowResize(size); 
        public void UpdateCurrentScene(double delta) =>
            _currentScene?.Update(delta);
        public void RenderCurrentScene(double delta) =>
            _currentScene?.Render(delta);
        public void UnloadCurrentScene() =>
            _currentScene?.Unload();

    }
}
