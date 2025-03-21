using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using AtomEngine;
using EngineLib;

namespace Editor
{
    internal class SceneSerializer
    {
        public static string SerializeScene(ProjectScene projectScene)
        {
            var sceneService = ServiceHub.Get<SceneManager>();
            sceneService.CallBeforeSave();

            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.Auto,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize
            };

            var result = JsonConvert.SerializeObject(projectScene, settings);
            sceneService.CallAfterSafe();
            return result;
        }

        public static ProjectScene DeserializeScene(string json)
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.Auto,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                ObjectCreationHandling = ObjectCreationHandling.Replace
            };
             
            var editorScene = JsonConvert.DeserializeObject<ProjectScene>(json, settings);
            return editorScene;
        }
    }
}
