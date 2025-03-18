using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using AtomEngine;

namespace Editor
{
    internal class SceneSerializer
    {
        private static EditorProjectScene ConvertToEditorScene(ProjectScene scene)
        {
            var editorScene = new EditorProjectScene();
            editorScene.ScenePath = scene.ScenePath;
            editorScene.Systems = scene.Systems;

            editorScene.Worlds = scene.Worlds.Select(worldData => {
                var editorWorld = new EditorWorldData
                {
                    WorldId = worldData.WorldId,
                    WorldName = worldData.WorldName,
                    IsDirty = worldData.IsDirty
                };

                editorWorld.Entities = worldData.Entities.Select(entity => {
                    var editorEntity = new EditorEntityData
                    {
                        Id = entity.Id,
                        Version = entity.Version,
                        Name = entity.Name,
                        Guid = entity.Guid,
                        IsPrefabInstance = entity.IsPrefabInstance,
                        PrefabSourceGuid = entity.PrefabSourceGuid
                    };

                    var componentDict = new Dictionary<string, IComponent>();
                    foreach (var comp in entity.Components)
                    {
                        componentDict.Add(comp.Key, (IComponent)comp.Value);
                    }

                    editorEntity.Components = componentDict;
                    return editorEntity;
                }).ToList();

                return editorWorld;
            }).ToList();

            if (scene.CurrentWorldData != null)
            {
                var currentWorldName = scene.CurrentWorldData.WorldName;
                editorScene.CurrentWorld = editorScene.Worlds.FirstOrDefault(w => w.WorldName == currentWorldName);
            }

            return editorScene;
        }

        private static ProjectScene ConvertToPublicScene(EditorProjectScene editorScene)
        {
            var worlds = editorScene.Worlds.Select(editorWorld => {
                var world = new WorldData
                {
                    WorldId = editorWorld.WorldId,
                    WorldName = editorWorld.WorldName,
                    IsDirty = editorWorld.IsDirty
                };

                world.Entities = editorWorld.Entities.Select(editorEntity => {
                    var entity = new EntityData
                    {
                        Id = editorEntity.Id,
                        Version = editorEntity.Version,
                        Name = editorEntity.Name,
                        Guid = editorEntity.Guid,
                        IsPrefabInstance = editorEntity.IsPrefabInstance,
                        PrefabSourceGuid = editorEntity.PrefabSourceGuid
                    };

                    foreach (var comp in editorEntity.Components)
                    {
                        entity.Components.Add(comp.Key, comp.Value);
                    }

                    return entity;
                }).ToList();

                return world;
            }).ToList();

            var currentWorld = worlds.FirstOrDefault();
            if (editorScene.CurrentWorld != null)
            {
                currentWorld = worlds.FirstOrDefault(w => w.WorldName == editorScene.CurrentWorld.WorldName);
            }

            var scene = new ProjectScene(worlds, currentWorld);
            scene.ScenePath = editorScene.ScenePath;
            scene.Systems = editorScene.Systems;

            return scene;
        }

        public static string SerializeScene(ProjectScene projectScene)
        {
            var sceneService = ServiceHub.Get<SceneManager>();
            sceneService.CallBeforeSave();

            var editorScene = ConvertToEditorScene(projectScene);

            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.Auto,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize
            };

            var result = JsonConvert.SerializeObject(editorScene, settings);
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
             

            var editorScene = JsonConvert.DeserializeObject<EditorProjectScene>(json, settings);
            return ConvertToPublicScene(editorScene);
        }
    }
}
