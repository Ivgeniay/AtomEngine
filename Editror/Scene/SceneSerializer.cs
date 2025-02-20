using System.Threading.Tasks;
using Newtonsoft.Json;
using AtomEngine;
using System.IO;
using System;

namespace Editor
{
    internal static class SceneSerializer
    {
        public async static void SaveScene(string path, SceneData scene)
        {
            try
            {
                string sceneData = JsonConvert.SerializeObject(scene);
                using(StreamWriter stream = new StreamWriter(path))
                {
                    Status.SetStatus($"Saving {scene.SceneName}...");
                    await stream.WriteAsync(sceneData);
                    Status.SetStatus($"Scene {scene.SceneName} saved");
                }
            }
            catch(Exception e)
            {
                Status.SetStatus($"Saving {scene.SceneName} failed");
                DebLogger.Error(e);
            }
        }

        public static async Task<SceneData> LoadScene(string path)
        {
            SceneData scene = new SceneData();
            try
            {
                using (StreamReader stream = new StreamReader(path))
                {
                    Status.SetStatus("Loading scene...");
                    var sceneData = await stream.ReadToEndAsync();
                    scene = JsonConvert.DeserializeObject<SceneData>(sceneData);
                    Status.SetStatus($"Scene {scene} loaded");
                }
            }
            catch (Exception e)
            {
                Status.SetStatus($"Loading failed");
                DebLogger.Error(e);
            }

            return scene;
        }
    }
}
