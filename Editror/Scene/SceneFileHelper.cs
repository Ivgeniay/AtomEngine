using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using System.Numerics;
using Newtonsoft.Json;
using AtomEngine;
using System;

namespace Editor
{
    internal static class SceneFileHelper
    {
        public static readonly FileDialogService.FileFilter[] SceneFileFilters = new[]
        {
            new FileDialogService.FileFilter("Scene Files", "scene"),
            new FileDialogService.FileFilter("JSON Files", "json"),
            new FileDialogService.FileFilter("All Files", "*")
        };

        public static async Task<ProjectScene?> OpenSceneAsync(Window window)
        {
            try
            {
                var filePath = await FileDialogService.OpenFileAsync(
                    window,
                    "Open Scene",
                    SceneFileFilters);

                if (string.IsNullOrEmpty(filePath))
                {
                    return null;
                }

                var fileContent = await FileDialogService.ReadTextFileAsync(filePath);
                if (string.IsNullOrEmpty(fileContent))
                {
                    DebLogger.Error("Не удалось прочитать содержимое файла");
                    return null;
                }

                ProjectScene? sceneData = SceneSerializer.DeserializeScene(fileContent);
                //ProjectScene? sceneData = JsonConvert.DeserializeObject<ProjectScene>(fileContent, GlobalDeserializationSettings.Settings);

                if (sceneData != null)
                {
                    DebLogger.Info($"Сцена загружена: {sceneData.WorldName} ({sceneData.Worlds?.Count ?? 0} миров)");
                }

                return sceneData;
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при загрузке сцены: {ex.Message}");
                return null;
            }
        }
        public static async Task<(bool, string)> SaveSceneAsync(Window window, ProjectScene scene, Action beforeSafe = null, Action afterSafe = null)
        {
            if (scene == null)
            {
                DebLogger.Error("Невозможно сохранить пустую сцену");
                return (false, string.Empty);
            }

            try
            {
                var fileName = string.IsNullOrEmpty(scene.WorldName)
                    ? "NewScene.scene"
                    : $"{scene.WorldName}.scene";

                string? filePath = await FileDialogService.SaveFileAsync(
                    window,
                    "Save Scene File",
                    fileName,
                    SceneFileFilters);

                if (string.IsNullOrEmpty(filePath))
                {
                    return (false, string.Empty);
                }
                scene.ScenePath = filePath;

                var jsonSettings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    TypeNameHandling = TypeNameHandling.Auto,
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                    ReferenceLoopHandling = ReferenceLoopHandling.Serialize
                };

                beforeSafe?.Invoke();
                //string jsonContent = JsonConvert.SerializeObject(scene, jsonSettings);
                string jsonContent = SceneSerializer.SerializeScene(scene);
                bool result = await FileDialogService.WriteTextFileAsync(filePath, jsonContent);

                if (result)
                {
                    DebLogger.Info($"Сцена сохранена в {filePath}");
                    Status.SetStatus($"Scene saved {filePath}");
                    return (true, filePath);
                }
                afterSafe?.Invoke();

                return (false, string.Empty);
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при сохранении сцены: {ex.Message}");
                Status.SetStatus($"Scene not saved");
                return (false, string.Empty);
            }
        }
        public static WorldData CreateWorldData()
        {
            var newScene = new WorldData
            {
                WorldName = "New Scene",
                IsDirty = false,
                Entities = new List<EntityData>
                {
                    new EntityData
                    {
                        Id = 0,
                        Name = "Main Camera",
                        Version = 0,
                        Components = new Dictionary<string, IComponent>
                        {
                            {
                                nameof(TransformComponent),
                                new TransformComponent
                                {
                                    Position = new Vector3(0, 0, -5),
                                    Scale = new Vector3(1, 1, 1),
                                }
                            },
                            {
                                nameof(CameraComponent),
                                new CameraComponent()
                            }
                        }
                    },
                    new EntityData
                    {
                        Id = 1,
                        Name = "Directional Light",
                        Version = 0,
                        Components = new Dictionary<string, IComponent>
                        {
                            {
                                nameof(TransformComponent),
                                new TransformComponent
                                {
                                    Position = Vector3.UnitX,
                                    Rotation = new Vector3(0, -90, 0),
                                    Scale = new Vector3(1, 1, 1),
                                }
                            }
                        }
                    },
                    new EntityData
                    {
                        Id = 2,
                        Name = "Cube",
                        Version = 0,
                        Components = new Dictionary<string, IComponent>
                        {
                            {
                                nameof(TransformComponent),
                                new TransformComponent()
                                {
                                    Scale = new Vector3(1, 1, 1),
                                }
                            },
                            {
                                nameof(ColliderComponent),
                                new ColliderComponent()
                            },
                            {
                                nameof(MeshComponent),
                                new MeshComponent()
                            }
                        }
                    }
                }
            };
            return newScene;
        }
    }
}
