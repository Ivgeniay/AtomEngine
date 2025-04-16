using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using System.Numerics;
using Newtonsoft.Json;
using AtomEngine;
using System;
using OpenglLib;

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

                string jsonContent = SceneSerializer.SerializeScene(scene);
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
        public static WorldData CreateDefauldWorldData()
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
                                typeof(TransformComponent).FullName,
                                new TransformComponent
                                {
                                    Position = new Vector3(0, 0, -5),
                                    Scale = new Vector3(1, 1, 1),
                                    Rotation = new Vector3(180, 0, 0)
                                }
                            },
                            {
                                typeof(CameraComponent).FullName,
                                new CameraComponent()
                                {
                                    FieldOfView = 45,
                                    AspectRatio = 1.777f,
                                    NearPlane = 0.1f,
                                    FarPlane = 45,
                                    CameraUp = new Vector3(0, 1, 0),
                                    CameraFront = new Vector3(0, 0, 1)
                                }
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
                                typeof(TransformComponent).FullName,
                                new TransformComponent
                                {
                                    Position = Vector3.UnitX,
                                    Rotation = new Vector3(0, -90, 0),
                                    Scale = new Vector3(1, 1, 1),
                                }
                            },
                            {
                                typeof(LightComponent).FullName,
                                new LightComponent
                                {
                                    Color = new Vector3(1,1,1),
                                    Intensity = 10,
                                    Enabled = 1,
                                    CastShadows = true,
                                    LightId = 0,
                                    IsDirty = true,
                                    Radius = 10f,
                                    FalloffExponent = 5f,
                                    Type = LightType.Directional,
                                }
                            }
                        }
                    },
                    new EntityData
                    {
                        Id = 2,
                        Name = "GlobalLightSettings",
                        Version = 0,
                        Components = new Dictionary<string, IComponent>
                        {
                            {
                                typeof(GlobalLightSettingsComponent).FullName,
                                new GlobalLightSettingsComponent
                                {
                                    AmbientColor = new Vector3(1,1,1),
                                    AmbientIntensity = 1,
                                    ShadowBias = 0.085f,
                                    PcfKernelSize = 3,
                                    ShadowIntensity = 0.7f,
                                    IsDirty = true
                                }
                            }
                        }
                    },
                    new EntityData
                    {
                        Id = 3,
                        Name = "Cube",
                        Version = 0,
                        Components = new Dictionary<string, IComponent>
                        {
                            {
                                typeof(TransformComponent).FullName,
                                new TransformComponent()
                                {
                                    Scale = new Vector3(1, 1, 1),
                                }
                            },
                            {
                                typeof(MaterialComponent).FullName,
                                new MaterialComponent()
                            },
                            {
                                typeof(MeshComponent).FullName,
                                new MeshComponent()
                            },
                            {
                                typeof(ColliderComponent).FullName,
                                new ColliderComponent()
                            },
                        }
                    }
                },
                
            };
            return newScene;
        }

        public static List<SystemData> CreateDefaultSystems()
        {
            return new List<SystemData>
            {
                new SystemData
                {
                    SystemFullTypeName = "OpenglLib.ViewRenderSystem",
                    ExecutionOrder = 1,
                    IncludInWorld = new List<uint>{ 0 },
                    Dependencies = new List<SystemData> { },
                    Category = SystemCategory.Render,
                },

                new SystemData
                {
                    SystemFullTypeName = "OpenglLib.LightUboRenderSystem",
                    ExecutionOrder = 2,
                    IncludInWorld = new List<uint>{ 0 },
                    Dependencies = new List<SystemData> { },
                    Category = SystemCategory.Render,
                },

                new SystemData
                {
                    SystemFullTypeName = "OpenglLib.CameraUboRenderSystem",
                    ExecutionOrder = 0,
                    IncludInWorld = new List<uint>{ 0 },
                    Dependencies = new List<SystemData> { },
                    Category = SystemCategory.Render,
                },
            };
        }
    }
}
