using AtomEngine;
using AtomEngine.RenderEntity;
using Newtonsoft.Json;
using OpenglLib;

namespace WindowsBuild
{
    internal class SceneLoader
    {
        private readonly WindowBuildFileRouter _router;
        private readonly AssemblyManager _assemblyManager;
        private readonly WorldManager _worldManager;

        public SceneLoader(WindowBuildFileRouter router, AssemblyManager assemblyManager, WorldManager worldManager)
        {
            _router = router;
            _assemblyManager = assemblyManager;
            _worldManager = worldManager;
        }

        public BuildProjectScene LoadDefaultScene()
        {
            var sceneFiles = Directory.GetFiles(_router.ScenesPath, $"*.{_router.SceneExtension}");

            if (sceneFiles.Length == 0)
            {
                throw new FileNotFoundException($"Не найдены файлы сцен в каталоге {_router.ScenesPath}");
            }

            return LoadScene(sceneFiles[0]);
        }

        public BuildProjectScene LoadScene(string scenePath)
        {
            DebLogger.Info($"Загрузка сцены: {scenePath}");

            if (!File.Exists(scenePath))
            {
                throw new FileNotFoundException($"Файл сцены не найден: {scenePath}");
            }

            string jsonContent = File.ReadAllText(scenePath);
            var jsonSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize
            };

            var sceneData = JsonConvert.DeserializeObject<BuildProjectScene>(jsonContent, jsonSettings);

            if (sceneData == null)
            {
                throw new InvalidOperationException($"Ошибка десериализации сцены: {scenePath}");
            }

            return sceneData;
        }

        public void InitializeScene(BuildProjectScene sceneData, RuntimeResourceManager resourceManager)
        {
            Dictionary<uint, World> worldsById = new Dictionary<uint, World>();

            foreach (var worldData in sceneData.Worlds)
            {
                World world = new World();
                worldsById[worldData.WorldId] = world;
                _worldManager.AddWorld(world);
            }

            Dictionary<string, ICommonSystem> systemInstances = new Dictionary<string, ICommonSystem>();

            foreach (var systemData in sceneData.Systems)
            {
                List<World> targetWorlds = new List<World>();

                if (systemData.IncludInWorld == null || systemData.IncludInWorld.Count == 0)
                {
                    targetWorlds.AddRange(worldsById.Values);
                }
                else
                {
                    foreach (var worldId in systemData.IncludInWorld)
                    {
                        if (worldsById.TryGetValue(worldId, out var world))
                        {
                            targetWorlds.Add(world);
                        }
                    }
                }

                if (targetWorlds.Count == 0)
                    continue;

                Type? systemType = AssemblyManager.Instance.FindType(systemData.SystemFullTypeName, true);

                if (systemType == null)
                {
                    DebLogger.Error($"Не удалось найти тип системы: {systemData.SystemFullTypeName}");
                    continue;
                }

                try
                {
                    foreach (var world in targetWorlds)
                    {
                        var constructor = systemType.GetConstructor(new[] { typeof(World) });
                        if (constructor != null)
                        {
                            var system = constructor.Invoke(new object[] { world });
                            
                            world.AddSystem((ICommonSystem)system);

                            string key = $"{systemData.SystemFullTypeName}_{world.GetHashCode()}";
                            systemInstances[key] = (ICommonSystem)system;
                        }
                        else
                        {
                            DebLogger.Error($"Не найден подходящий конструктор для системы: {systemData.SystemFullTypeName}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    DebLogger.Error($"Ошибка при создании системы {systemData.SystemFullTypeName}: {ex.Message}");
                }
            }

            SetupSystemDependencies(sceneData.Systems, systemInstances, worldsById);
            foreach (var worldData in sceneData.Worlds)
            {
                if (worldsById.TryGetValue(worldData.WorldId, out var world))
                {
                    LoadEntities(world, worldData, resourceManager);
                }
            }
        }

        private void SetupSystemDependencies(List<SystemData> systemsData, Dictionary<string, ICommonSystem> systemInstances, Dictionary<uint, World> worldsById)
        {
            foreach (var systemData in systemsData)
            {
                if (systemData.Dependencies == null || systemData.Dependencies.Count == 0)
                    continue;

                List<World> targetWorlds;

                if (systemData.IncludInWorld == null || systemData.IncludInWorld.Count == 0)
                {
                    targetWorlds = new List<World>(worldsById.Values);
                }
                else
                {
                    targetWorlds = new List<World>();
                    foreach (var worldId in systemData.IncludInWorld)
                    {
                        if (worldsById.TryGetValue(worldId, out var world))
                        {
                            targetWorlds.Add(world);
                        }
                    }
                }

                foreach (var world in targetWorlds)
                {
                    string systemKey = $"{systemData.SystemFullTypeName}_{world.GetHashCode()}";
                    if (!systemInstances.TryGetValue(systemKey, out var system) || !(system is ISystem currentSystem))
                        continue;

                    foreach (var dependencyData in systemData.Dependencies)
                    {
                        string dependencyKey = $"{dependencyData.SystemFullTypeName}_{world.GetHashCode()}";
                        if (systemInstances.TryGetValue(dependencyKey, out var dependency) && dependency is ISystem dependencySystem)
                        {
                            try
                            {
                                ((World)world).AddSystemDependency(currentSystem, dependencySystem);
                            }
                            catch (Exception ex)
                            {
                                DebLogger.Error($"Ошибка при настройке зависимости {systemData.SystemFullTypeName} -> {dependencyData.SystemFullTypeName}: {ex.Message}");
                            }
                        }
                    }
                }
            }
        }

        private void LoadEntities(World world, BuildWorldData worldData, RuntimeResourceManager resourceManager)
        {
            foreach (var entityData in worldData.Entities)
            {
                Entity entity = world.CreateEntityWithId(entityData.Id, entityData.Version);
                foreach (var kvp in entityData.Components)
                {
                    string componentTypeName = kvp.Key;
                    IComponent componentData = kvp.Value;

                    Type? componentType = AssemblyManager.Instance.FindType(componentTypeName, true);
                    if (componentType == null)
                    {
                        DebLogger.Error($"Не удалось найти тип компонента: {componentTypeName}");
                        continue;
                    }

                    try
                    {
                        bool isGLDependable = componentType.GetCustomAttributes(typeof(GLDependableAttribute), true).Length > 0;

                        if (isGLDependable)
                        {
                            HandleGLDependableComponent(world, entity, componentType, componentData, resourceManager);
                        }
                        else
                        {
                            var methodInfo = typeof(World).GetMethod("AddComponent")?.MakeGenericMethod(componentType);
                            if (methodInfo != null)
                            {
                                methodInfo.Invoke(world, new object[] { entity, componentData });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        DebLogger.Error($"Ошибка при добавлении компонента {componentTypeName}: {ex.Message}");
                    }
                }
            }
        }

        private void HandleGLDependableComponent(World world, Entity entity, Type componentType, IComponent componentData, RuntimeResourceManager resourceManager)
        {
            var fields = componentType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            IComponent modifiedComponent = (IComponent)Activator.CreateInstance(componentType);

            foreach (var field in fields)
            {
                field.SetValue(modifiedComponent, field.GetValue(componentData));
            }

            foreach (var field in fields)
            {
                if (field.Name.Contains("GUID", StringComparison.OrdinalIgnoreCase))
                {
                    string guidValue = field.GetValue(componentData)?.ToString();

                    if (string.IsNullOrEmpty(guidValue))
                        continue;

                    string resourceFieldName = field.Name.Replace("GUID", "", StringComparison.OrdinalIgnoreCase);
                    var resourceField = fields.FirstOrDefault(f => f.Name.Equals(resourceFieldName, StringComparison.OrdinalIgnoreCase));

                    if (resourceField != null)
                    {
                        object resource = null;

                        Type resourceType = resourceField.FieldType;

                        if (typeof(MeshBase).IsAssignableFrom(resourceType))
                        {
                            resource = resourceManager.GetMesh(guidValue);
                        }
                        else if (typeof(ShaderBase).IsAssignableFrom(resourceType))
                        {
                            resource = resourceManager.GetMaterial(guidValue);
                        }
                        else if (typeof(Texture).IsAssignableFrom(resourceType))
                        {
                            resource = resourceManager.GetTexture(guidValue);
                        }

                        if (resource != null)
                        {
                            resourceField.SetValue(modifiedComponent, resource);
                        }
                    }
                }
            }

            var methodInfo = typeof(World).GetMethod("AddComponent")?.MakeGenericMethod(componentType);
            if (methodInfo != null)
            {
                methodInfo.Invoke(world, new object[] { entity, modifiedComponent });
            }
        }

    }
}
