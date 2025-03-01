using System.Threading.Tasks;
using Newtonsoft.Json;
using AtomEngine;
using System.IO;
using System;

namespace Editor
{
    internal static class SceneSerializer
    {
        public async static void SaveScene(string path, WorldData scene)
        {
            try
            {
                string sceneData = JsonConvert.SerializeObject(scene);
                using(StreamWriter stream = new StreamWriter(path))
                {
                    Status.SetStatus($"Saving {scene.WorldName}...");
                    await stream.WriteAsync(sceneData);
                    Status.SetStatus($"Scene {scene.WorldName} saved");
                }
            }
            catch(Exception e)
            {
                Status.SetStatus($"Saving {scene.WorldName} failed");
                DebLogger.Error(e);
            }
        }

        public static async Task<WorldData> LoadScene(string path)
        {
            WorldData scene = new WorldData();
            try
            {
                using (StreamReader stream = new StreamReader(path))
                {
                    Status.SetStatus("Loading scene...");
                    var sceneData = await stream.ReadToEndAsync();
                    scene = JsonConvert.DeserializeObject<WorldData>(sceneData);
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
