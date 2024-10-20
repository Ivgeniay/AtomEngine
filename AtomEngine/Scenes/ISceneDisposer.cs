using AtomEngine.Math;

namespace AtomEngine.Scenes
{
    public interface ISceneDisposer
    {
        public Scene? CurrentScene { get; }
        public void AddScene(Scene scene);
        public void AddManyScenes(IEnumerable<Scene> scenes);
        public void RemoveScene(string id);
        public void RemoveScene(Scene scene);
        public void RemoveManyScenes(IEnumerable<Scene> scenes);
        public void LoadScene(string id);
        public void LoadScene(Scene scene);

        public void ResizeCurrentScene(Vector2D<int> size);
        public void UpdateCurrentScene(double delta);
        public void RenderCurrentScene(double delta);
        public void UnloadCurrentScene();

    }
}
