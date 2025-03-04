using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Newtonsoft.Json;
using System.Linq;
using AtomEngine;
using System;
using System.Numerics;

namespace Editor
{
    internal static class SceneFileHelper
    {
        /// <summary>
        /// Стандартные фильтры для файлов сцен
        /// </summary>
        public static readonly FileDialogService.FileFilter[] SceneFileFilters = new[]
        {
            new FileDialogService.FileFilter("Scene Files", "scene"),
            new FileDialogService.FileFilter("JSON Files", "json"),
            new FileDialogService.FileFilter("All Files", "*")
        };

        /// <summary>
        /// Открывает диалог и загружает файл сцены
        /// </summary>
        /// <param name="window">Родительское окно</param>
        /// <returns>Загруженные данные сцены или null</returns>
        public static async Task<ProjectScene?> OpenSceneAsync(Window window)
        {
            try
            {
                var filePath = await FileDialogService.OpenFileAsync(
                    window,
                    "Open Scene File",
                    SceneFileFilters);

                if (string.IsNullOrEmpty(filePath))
                {
                    return null;
                }

                // Читаем содержимое файла
                var fileContent = await FileDialogService.ReadTextFileAsync(filePath);
                if (string.IsNullOrEmpty(fileContent))
                {
                    DebLogger.Error("Не удалось прочитать содержимое файла");
                    return null;
                }

                ProjectScene? sceneData = JsonConvert.DeserializeObject<ProjectScene>(fileContent, GlobalDeserializationSettings.Settings);

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

        /// <summary>
        /// Открывает диалог и сохраняет данные сцены в файл
        /// </summary>
        /// <param name="window">Родительское окно</param>
        /// <param name="scene">Данные сцены для сохранения</param>
        /// <returns>true если сохранение успешно, иначе false</returns>
        public static async Task<(bool, string)> SaveSceneAsync(Window window, ProjectScene scene)
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

                string jsonContent = JsonConvert.SerializeObject(scene, jsonSettings);
                bool result = await FileDialogService.WriteTextFileAsync(filePath, jsonContent);

                if (result)
                {
                    DebLogger.Info($"Сцена сохранена в {filePath}");
                    Status.SetStatus($"Scene saved {filePath}");
                    return (true, filePath);
                }

                return (false, string.Empty);
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при сохранении сцены: {ex.Message}");
                Status.SetStatus($"Scene not saved");
                return (false, string.Empty);
            }
        }

        /// <summary>
        /// Открывает диалоговое окно для создания новой сцены
        /// </summary>
        /// <param name="window">Родительское окно</param>
        /// <returns>Данные новой сцены или null</returns>
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
                                    Position = new Vector3(0, 0, -5)
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
                            },
                            {
                                nameof(ColliderComponent),
                                new ColliderComponent()
                            },
                            {
                                nameof(ShaderComponent),
                                new ShaderComponent()
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
