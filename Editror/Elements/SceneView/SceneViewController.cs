using System.Collections.Concurrent;
using System.Collections.Generic;
using AtomEngine.RenderEntity;
using System.Threading.Tasks;
using Avalonia.Threading;
using Avalonia.Controls;
using System.Reflection;
using Avalonia.Layout;
using System.Numerics;
using Silk.NET.OpenGL;
using Avalonia.Input;
using Avalonia.Media;
using System.Linq;
using AtomEngine;
using Avalonia;
using System;

using KeyEventArgs = Avalonia.Input.KeyEventArgs;
using MouseButton = Avalonia.Input.MouseButton;
using OpenglLib;
using Avalonia.Interactivity;

namespace Editor
{
    internal class SceneViewController : ContentControl, IWindowed, IDisposableController, ICacheble
    {
        public Action<uint> OnEntitySelected;

        private GL _gl;

        private GLController _glController;
        private EditorRuntimeResourceManager _resourceManager;
        private DispatcherTimer _renderTimer;
        private Grid _mainGrid;
        private Border _toolbarBorder;
        private Grid _renderCanvas;

        private WorldManager _worldManager;
        private Dictionary<string, World> _sceneWorlds = new Dictionary<string, World>();
        private World _currentEditorWorld;

        private Dictionary<EntityData, RenderPairCache> _componentRenderCache = new Dictionary<EntityData, RenderPairCache>();
        private ConcurrentQueue<OpenGLCommand> _glCommands = new ConcurrentQueue<OpenGLCommand>();

        private SceneManager _sceneManager;
        private MaterialFactory _materialFactory;
        private EventHub _eventHub;

        private bool _isPerspective = true;
        private bool _isOpen = false;
        private bool _isGlInitialized = false;
        private bool _isDataInitialized = false;

        private bool _isPreparingClose = false;
        private TaskCompletionSource<bool> _disposeTcs;

        private Entity _editorCameraEntity;

        public Action<object> OnClose { get; set; }

        public SceneViewController()
        {
            InitializeUI();
            InitializeEvents();

            _resourceManager = ServiceHub.Get<EditorRuntimeResourceManager>();
            _materialFactory = ServiceHub.Get<MaterialFactory>();
            _sceneManager = ServiceHub.Get<SceneManager>();
            _eventHub = ServiceHub.Get<EventHub>();

            _sceneManager.OnSceneInitialize += SetScene;
            _sceneManager.OnScenUnload += UnloadScene;

            BVHTree.Instance.Initialize(SceneManager.EntityCompProvider);

            _worldManager = new WorldManager();
        }

        private void InitializeUI()
        {
            this.Focusable = true;

            _mainGrid = new Grid();
            _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });

            _toolbarBorder = new Border
            {
                Height = 48,
                Classes = { "toolbarBackground" }
            };

            var toolbarPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                Margin = new Thickness(5)
            };

            var translateButton = new Button { Content = "Translate", Classes = { "toolButton" } };
            var rotateButton = new Button { Content = "Rotate", Classes = { "toolButton" } };
            var scaleButton = new Button { Content = "Scale", Classes = { "toolButton" } };
            var perspectiveButton = new Button { Content = "Perspective", Classes = { "toolButton" } };

            translateButton.Click += (s, e) => SetTransformMode(TransformMode.Translate);
            rotateButton.Click += (s, e) => SetTransformMode(TransformMode.Rotate);
            scaleButton.Click += (s, e) => SetTransformMode(TransformMode.Scale);
            perspectiveButton.Click += (s, e) => TogglePerspective();

            toolbarPanel.Children.Add(translateButton);
            toolbarPanel.Children.Add(rotateButton);
            toolbarPanel.Children.Add(scaleButton);
            toolbarPanel.Children.Add(perspectiveButton);

            _toolbarBorder.Child = toolbarPanel;
            _renderCanvas = new Grid
            {
                Background = Brushes.DarkGray
            };

            Grid.SetRow(_toolbarBorder, 0);
            Grid.SetRow(_renderCanvas, 1);

            _mainGrid.Children.Add(_toolbarBorder);
            _mainGrid.Children.Add(_renderCanvas);

            this.Content = _mainGrid;
        }

        private void InitializeEvents()
        {
            _renderCanvas.PointerReleased += OnPointerReleased;

            _renderTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            _renderTimer.Tick += (sender, args) =>
            {
                Render();
            };
        }

        private void SetScene(ProjectScene scene)
        {
            EnqueueGLCommand((gl) =>
            {
                SetupWorldsFromScene(scene);
            });
        }

        private void SetupWorldsFromScene(ProjectScene scene)
        {
            if (!_isGlInitialized) return;
            if (_isDataInitialized) return;

            foreach (var world in _sceneWorlds.Values)
            {
                world.Dispose();
            }
            _sceneWorlds.Clear();

            foreach (var worldData in scene.Worlds)
            {
                var world = new World();
                _sceneWorlds[worldData.WorldName] = world;
                _worldManager.AddWorld(world);

                CreateEditorCamera(world, worldData);

                InitializeRenderSystemsForWorld(world);
                CreateEntitiesForWorld(world, worldData);
            }

            if (scene.CurrentWorldData != null && _sceneWorlds.TryGetValue(scene.CurrentWorldData.WorldName, out var currentWorld))
            {
                _worldManager.CurrentWorld = currentWorld;
                _currentEditorWorld = currentWorld;
            }
            else if (_sceneWorlds.Count > 0)
            {
                _currentEditorWorld = _sceneWorlds.Values.First();
                _worldManager.CurrentWorld = _currentEditorWorld;
            }

            _isDataInitialized = true;
        }

        private void CreateEditorCamera(World world, WorldData worldData)
        {
            uint id = _sceneManager.GetAndReserveId(worldData.WorldId);
            var admin = world.GetAdmin();

            _editorCameraEntity = admin.CreateEntityWithId(id, 0);

            var transform = new TransformComponent
            {
                Position = new Vector3(5, 5, -10),
                Rotation = new Vector3(0, -90, 0),
                Scale = Vector3.One
            };
            world.AddComponent(_editorCameraEntity, in transform);

            var cameraComp = new CameraComponent
            {
                FieldOfView = 45,
                AspectRatio = (float)_renderCanvas.Bounds.Width / (float)_renderCanvas.Bounds.Height,
                NearPlane = 0.1f,
                FarPlane = 1000.0f,
                CameraUp = Vector3.UnitY,
                CameraFront = Vector3.UnitZ
            };
            world.AddComponent(_editorCameraEntity, in cameraComp);

            var editorCamComp = new EditorCameraComponent
            {
                Target = Vector3.Zero,
                MoveSpeed = 0.1f,
                RotationSpeedX = 0.001f,
                RotationSpeedY = 0.01f,
                IsPerspective = _isPerspective,
                LastMousePosition = new Point(0, 0)
            };
            world.AddComponent(_editorCameraEntity, in editorCamComp);

            var cameraControl = new EditorCameraControllerComponent
            {
                IsActive = true
            };
            world.AddComponent(_editorCameraEntity, cameraControl);
        }

        private void InitializeRenderSystemsForWorld(World world)
        {
            world.AddSystem(new EditorGridRenderSystem(world, _gl));
            world.AddSystem(new EditorAABBRenderSystem(world, _gl, SceneManager.EntityCompProvider));
            world.AddSystem(new EditorCameraFrustumRenderSystem(world, _gl));
            world.AddSystem(new EditorCameraControllerSystem(world));
        }

        private void CreateEntitiesForWorld(World world, WorldData worldData)
        {
            var admin = world.GetAdmin();

            foreach (var entityData in worldData.Entities)
            {
                var entity = admin.CreateEntityWithId(entityData.Id, entityData.Version);
                foreach (var componentPair in entityData.Components)
                {
                    var component = componentPair.Value;
                    var componentType = component.GetType();

                    // Используем reflection для добавления компонента
                    var addComponentMethod = typeof(World).GetMethod("AddComponent").MakeGenericMethod(componentType);
                    addComponentMethod.Invoke(world, new object[] { entity, component });

                    if (typeof(MeshComponent).IsAssignableFrom(componentType))
                    {
                        var mesh = GetMeshFromComponent((MeshComponent)component);
                        if (mesh?.BoundingVolume != null)
                        {
                            BVHTree.Instance.AddEntity(entityData.Id);
                        }
                    }
                }
            }
        }

        private MeshBase GetMeshFromComponent(MeshComponent meshComponent)
        {
            var type = meshComponent.GetType();
            var fields = type
                .GetFields(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(e => typeof(MeshBase).IsAssignableFrom(e.FieldType));

            if (fields != null)
            {
                var value = fields.GetValue(meshComponent);
                if (value != null) return value as MeshBase;
            }

            return null;
        }

        public void UnloadScene()
        {
            if (!_isOpen) return;

            EnqueueGLCommand((gl) =>
            {
                FreeCache();
            });
        }

        public void Open()
        {
            if (_glController == null)
            {
                _glController = new GLController();
                GLController.OnGLInitialized += OnGLInitialized;
                GLController.OnRender += OnRender;

                _glController.HorizontalAlignment = HorizontalAlignment.Stretch;
                _glController.VerticalAlignment = VerticalAlignment.Stretch;

                _renderCanvas.Children.Add(_glController);
            }

            _materialFactory.SetSceneViewController(this);

            _sceneManager.OnSceneBeforeSave += PrepareToSave;
            _sceneManager.OnSceneAfterSave += UpdateEntitiesFromScene;

            _sceneManager.OnComponentChange += ComponentChange;
            _sceneManager.OnComponentAdded += ComponentAdded;
            _sceneManager.OnComponentRemoved += ComponentRemoved;
            _sceneManager.OnEntityCreated += EntityCreated;
            _sceneManager.OnEntityRemoved += EntityRemoved;
            _sceneManager.OnWorldSelected += WorldSelected;

            _renderTimer.Start();
            _isOpen = true;
            this.Focus();
        }

        public void Close() { }

        public void Redraw()
        {
            UpdateEntitiesFromScene();
        }

        private void OnGLInitialized(GL gl)
        {
            try
            {
                _gl = gl;
                _isGlInitialized = true;
                if (_sceneManager.CurrentScene != null)
                {
                    SetupWorldsFromScene(_sceneManager.CurrentScene);
                }
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Failed to initialize scene renderer: {ex.Message}");
            }
        }
        
        private void OnRender(GL gl)
        {
            try
            {
                if (!_isGlInitialized) return;

                var error = gl.GetError();
                if (error != GLEnum.NoError)
                {
                    DebLogger.Error($"Error viewport: {error}");
                    return;
                }

                var scalingFactor = VisualRoot?.RenderScaling ?? 1.0;
                uint width = (uint)(_renderCanvas.Bounds.Width * scalingFactor);
                uint height = (uint)(_renderCanvas.Bounds.Height * scalingFactor);

                gl.Viewport(0, 0, width, height);
                gl.ClearColor(0.2f, 0.2f, 0.2f, 1.0f);
                gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                gl.Enable(EnableCap.DepthTest);
                gl.DepthFunc(DepthFunction.Lequal);

                while (_glCommands.TryDequeue(out var command))
                {
                    command.Execute(gl);
                }

                _worldManager.Render(0.016);

                Input.Update();
                if (_isPreparingClose)
                {
                    Dispose();
                }
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка рендеринга сцены: {ex.Message}");
            }
        }

        private void UpdateEntitiesFromScene()
        {
            if (!_isOpen || !_isGlInitialized) return;

            if (_sceneManager.CurrentScene == null)
                return;

            EnqueueGLCommand((gl) =>
            {
                SetupWorldsFromScene(_sceneManager.CurrentScene);
            });
        }

        private void Render()
        {
            _glController?.Invalidate();
        }

        public void EnqueueGLCommand(Action<GL> command)
        {
            _glCommands.Enqueue(new OpenGLCommand { Execute = command });
        }

        private void OnPointerReleased(object sender, PointerReleasedEventArgs e)
        {
            if (e.InitialPressMouseButton == MouseButton.Left)
            {
                PickObject(e.GetPosition(_renderCanvas));
            }
        }

        private void PickObject(Point point)
        {
            if (!_isOpen || !_isGlInitialized || _sceneManager.CurrentScene == null || _editorCameraEntity == null)
                return;

            float x = (float)(point.X / _renderCanvas.Bounds.Width) * 2 - 1;
            float y = 1 - (float)(point.Y / _renderCanvas.Bounds.Height) * 2;

            ref var transform = ref _currentEditorWorld.GetComponent<TransformComponent>(_editorCameraEntity);
            ref var camera = ref _currentEditorWorld.GetComponent<CameraComponent>(_editorCameraEntity);
            ref var editorCamera = ref _currentEditorWorld.GetComponent<EditorCameraComponent>(_editorCameraEntity);

            var rayDirection = CalculateRayDirection(x, y, transform, camera, editorCamera.IsPerspective);
            var ray = new BVHTree.BvhRay(transform.Position, rayDirection);

            if (ray.Raycast(out var hit))
            {
                var entity = _sceneManager.CurrentScene.CurrentWorldData.Entities.FirstOrDefault(e => e.Id == hit.EntityId);
                if (entity != null)
                {
                    OnEntitySelected?.Invoke(entity.Id);
                }
            }
        }

        private Vector3 CalculateRayDirection(float normalizedX, float normalizedY, TransformComponent transform, CameraComponent camera, bool isPerspective)
        {
            Vector4 clipCoords = new Vector4(normalizedX, normalizedY, -1.0f, 1.0f);

            Matrix4x4.Invert(GetProjectionMatrix(camera, isPerspective), out var invProjection);
            Vector4 eyeCoords = Vector4.Transform(clipCoords, invProjection);
            eyeCoords = new Vector4(eyeCoords.X, eyeCoords.Y, -1.0f, 0.0f);

            Matrix4x4.Invert(GetViewMatrix(transform, camera), out var invView);
            Vector4 rayWorld = Vector4.Transform(eyeCoords, invView);
            Vector3 rayDirection = new Vector3(rayWorld.X, rayWorld.Y, rayWorld.Z);

            return Vector3.Normalize(rayDirection);
        }

        private Matrix4x4 GetProjectionMatrix(CameraComponent camera, bool isPerspective)
        {
            float aspectRatio = (float)_renderCanvas.Bounds.Width / (float)_renderCanvas.Bounds.Height;

            if (isPerspective)
            {
                return Matrix4x4.CreatePerspectiveFieldOfView(
                    camera.FieldOfView * (float)(Math.PI / 180.0),
                    aspectRatio,
                    camera.NearPlane,
                    camera.FarPlane);
            }
            else
            {
                float size = 5f; // Может быть настроено в зависимости от масштаба
                return Matrix4x4.CreateOrthographic(
                    size * aspectRatio,
                    size,
                    camera.NearPlane,
                    camera.FarPlane);
            }
        }

        private Matrix4x4 GetViewMatrix(TransformComponent transform, CameraComponent camera)
        {
            return Matrix4x4.CreateLookAt(
                transform.Position,
                transform.Position + camera.CameraFront,
                camera.CameraUp);
        }

        private void SetTransformMode(TransformMode mode)
        {
            // Логика выбора режима трансформации
            Status.SetStatus($"Transform mode: {mode}");
        }

        private void TogglePerspective()
        {
            _isPerspective = !_isPerspective;

            if (_currentEditorWorld != null && _editorCameraEntity != null)
            {
                ref var editorCamera = ref _currentEditorWorld.GetComponent<EditorCameraComponent>(_editorCameraEntity);
                editorCamera.IsPerspective = _isPerspective;
            }

            Status.SetStatus($"Camera projection: {(_isPerspective ? "Perspective" : "Orthographic")}");
        }

        private void PrepareToSave()
        {
            // Подготовка к сохранению
            FreeCache();
        }

        Dictionary<World, QueryEntity> _cameraQueries = new Dictionary<World, QueryEntity>();
        private void WorldSelected(uint worldId, string worldName)
        {
            if (_sceneWorlds.TryGetValue(worldName, out var world))
            {
                _currentEditorWorld = world;
                if (_cameraQueries.TryGetValue(world, out QueryEntity queryEntity)) {
                    _editorCameraEntity = queryEntity.Build()[0];
                }
                else
                {
                    _cameraQueries[world] = world.CreateEntityQuery().With<EditorCameraComponent>();
                    _editorCameraEntity = _cameraQueries[world].Build()[0];
                }
                _worldManager.CurrentWorld = world;
            }
        }

        public void FreeCache()
        {
            foreach (var world in _sceneWorlds.Values)
            {
                world.Dispose();
            }

            _sceneWorlds.Clear();
            _componentRenderCache.Clear();
            _sceneManager.DisposeAllReservedId();
            _cameraQueries.Clear();
            BVHTree.Instance?.FreeCache();

            _editorCameraEntity = new Entity(uint.MaxValue, uint.MaxValue);
        }

        public void EntityCreated(uint worldId, uint entityId)
        {
            if (!_isOpen) return;

            EnqueueGLCommand((gl) =>
            {
                if (_sceneManager.CurrentScene == null || !_sceneWorlds.Any()) return;

                var worldData = _sceneManager.CurrentScene.Worlds.FirstOrDefault(w => w.WorldId == worldId);
                if (worldData == null) return;

                if (_sceneWorlds.TryGetValue(worldData.WorldName, out var world))
                {
                    var entityData = _sceneManager.CurrentScene.CurrentWorldData.Entities.FirstOrDefault(e => e.Id == entityId);
                    if (entityData == null) return;

                    var admin = world.GetAdmin();
                    var entity = admin.CreateEntityWithId(entityId, entityData.Version);

                    foreach (var componentPair in entityData.Components)
                    {
                        var component = componentPair.Value;
                        var componentType = component.GetType();

                        var addComponentMethod = typeof(World).GetMethod("AddComponent").MakeGenericMethod(componentType);
                        addComponentMethod.Invoke(world, new object[] { entity, component });
                    }
                }
            });
        }

        public void EntityRemoved(uint worldId, uint entityId)
        {
            if (!_isOpen) return;

            EnqueueGLCommand((gl) =>
            {
                // Находим соответствующий мир
                var worldData = _sceneManager.CurrentScene.Worlds.FirstOrDefault(w => w.WorldId == worldId);
                if (worldData != null && _sceneWorlds.TryGetValue(worldData.WorldName, out var world))
                {
                    // Удаляем сущность из мира ECS
                    world.DestroyEntity(new Entity(entityId, 0));
                }

                // Удаляем из BVH
                BVHTree.Instance.RemoveEntity(entityId);
            });
        }

        public void ComponentAdded(uint worldId, uint entityId, IComponent component)
        {
            if (!_isOpen) return;

            EnqueueGLCommand((gl) =>
            {
                // Находим соответствующий мир
                var worldData = _sceneManager.CurrentScene.Worlds.FirstOrDefault(w => w.WorldId == worldId);
                if (worldData != null && _sceneWorlds.TryGetValue(worldData.WorldName, out var world))
                {
                    var componentType = component.GetType();
                    var entity = new Entity(entityId, 0);

                    // Проверяем существует ли сущность
                    if (world.IsEntityValid(entityId, 0))
                    {
                        // Добавляем компонент к сущности
                        var addComponentMethod = typeof(World).GetMethod("AddComponent").MakeGenericMethod(componentType);
                        addComponentMethod.Invoke(world, new object[] { entity, component });

                        // Обновляем BVH если это MeshComponent
                        if (componentType == typeof(MeshComponent))
                        {
                            BVHTree.Instance.AddEntity(entityId);
                        }
                    }
                }
            });
        }

        public void ComponentRemoved(uint worldId, uint entityId, IComponent component)
        {
            if (!_isOpen) return;

            EnqueueGLCommand((gl) =>
            {
                // Находим соответствующий мир
                var worldData = _sceneManager.CurrentScene.Worlds.FirstOrDefault(w => w.WorldId == worldId);
                if (worldData != null && _sceneWorlds.TryGetValue(worldData.WorldName, out var world))
                {
                    var componentType = component.GetType();
                    var entity = new Entity(entityId, 0);

                    // Проверяем существует ли сущность
                    if (world.IsEntityValid(entityId, 0))
                    {
                        // Удаляем компонент у сущности
                        var removeComponentMethod = typeof(World).GetMethod("RemoveComponent").MakeGenericMethod(componentType);
                        removeComponentMethod.Invoke(world, new object[] { entity });

                        // Если удаляется MeshComponent, удаляем из BVH
                        if (componentType == typeof(MeshComponent))
                        {
                            BVHTree.Instance.RemoveEntity(entityId);
                        }
                    }
                }
            });
        }

        public void ComponentChange(uint worldId, uint entityId, IComponent component)
        {
            if (!_isOpen) return;

            EnqueueGLCommand((gl) =>
            {
                // Находим соответствующий мир
                var worldData = _sceneManager.CurrentScene.Worlds.FirstOrDefault(w => w.WorldId == worldId);
                if (worldData != null && _sceneWorlds.TryGetValue(worldData.WorldName, out var world))
                {
                    var componentType = component.GetType();
                    var entity = new Entity(entityId, 0);

                    // Проверяем существует ли сущность
                    if (world.IsEntityValid(entityId, 0))
                    {
                        // Удаляем старый компонент и добавляем новый
                        var removeComponentMethod = typeof(World).GetMethod("RemoveComponent").MakeGenericMethod(componentType);
                        removeComponentMethod.Invoke(world, new object[] { entity });

                        var addComponentMethod = typeof(World).GetMethod("AddComponent").MakeGenericMethod(componentType);
                        addComponentMethod.Invoke(world, new object[] { entity, component });

                        // Обновляем BVH, если это TransformComponent
                        if (componentType == typeof(TransformComponent))
                        {
                            BVHTree.Instance.UpdateEntity(entityId);
                        }
                    }
                }
            });
        }

        public void Dispose()
        {
            _materialFactory.SetSceneViewController(null);
            _sceneManager.DisposeAllReservedId();

            foreach (var world in _sceneWorlds.Values)
            {
                world.Dispose();
            }
            _sceneWorlds.Clear();

            if (_glController != null)
            {
                _renderCanvas.Children.Remove(_glController);
                GLController.OnGLInitialized -= OnGLInitialized;
                GLController.OnRender -= OnRender;
                _glController.Dispose();
                _glController = null;
            }

            _isGlInitialized = false;

            _resourceManager?.Dispose();
            _sceneManager.OnSceneBeforeSave -= PrepareToSave;

            _sceneManager.OnComponentChange -= ComponentChange;
            _sceneManager.OnComponentAdded -= ComponentAdded;
            _sceneManager.OnComponentRemoved -= ComponentRemoved;
            _sceneManager.OnEntityCreated -= EntityCreated;
            _sceneManager.OnEntityRemoved -= EntityRemoved;
            _sceneManager.OnWorldSelected -= WorldSelected;

            _renderTimer.Stop();
            _isOpen = false;
            _isGlInitialized = false;

            BVHTree.Instance?.FreeCache();
        }

        public async Task PrepareForCloseAsync()
        {
            if (_isPreparingClose)
                return;

            _isPreparingClose = true;
            _disposeTcs = new TaskCompletionSource<bool>();

            await Task.WhenAny(_disposeTcs.Task, Task.Delay(70));

            _isPreparingClose = false;
        }

        private class RenderPairCache
        {
            public ShaderBase Shader = null;
            public FieldInfo ShaderField = null;
            public object ShaderObject = null;
            public MeshBase Mesh = null;
            public FieldInfo MeshField = null;
            public object MeshObject = null;
        }
        private enum TransformMode
        {
            Translate,
            Rotate,
            Scale
        }

    }

    public class EditorCameraFrustumRenderSystem : IRenderSystem
    {
        private IWorld _world;
        public IWorld World => _world;

        private readonly IEntityComponentInfoProvider _componentProvider;
        private readonly GL _gl;
        private CameraFrustumShader _shader;
        private Vector4 _defaultColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);

        private QueryEntity _queryCameras;
        private QueryEntity _queryEditorCamera;

        public EditorCameraFrustumRenderSystem(IWorld world, GL gl)
        {
            _world = world;
            _gl = gl;

            _queryCameras = world.CreateEntityQuery()
                .Without<EditorCameraComponent>()
                .With<TransformComponent>()
                .With<CameraComponent>();

            _queryEditorCamera = world.CreateEntityQuery()
                .With<TransformComponent>()
                .With<CameraComponent>()
                .With<EditorCameraComponent>();

            try
            {
                _shader = new CameraFrustumShader(_gl);
                _shader.SetColor(_defaultColor);
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка инициализации CameraFrustumShader: {ex.Message}");
            }
        }

        public void Initialize() { }

        public void Render(double deltaTime)
        {
            if (_shader == null)
                return;

            var editorCameras = _queryEditorCamera.Build();
            if (editorCameras.Length == 0) return;

            var cameras = _queryCameras.Build();
            if (cameras.Length == 0)
                return;

            var editorCameraEntity = editorCameras[0];
            ref var editorTransform = ref _world.GetComponent<TransformComponent>(editorCameraEntity);
            ref var editorCamera = ref _world.GetComponent<CameraComponent>(editorCameraEntity);
            ref var editorCameraExt = ref _world.GetComponent<EditorCameraComponent>(editorCameraEntity);

            Matrix4x4 view = editorCamera.ViewMatrix;
            Matrix4x4 projection = editorCameraExt.IsPerspective
                ? editorCamera.CreateProjectionMatrix()
                : CreateOrthographicMatrix(editorCamera, editorTransform, editorCameraExt);

            GLEnum blendingEnabled = _gl.GetBoolean(GetPName.Blend) ? GLEnum.True : GLEnum.False;
            GLEnum depthTestEnabled = _gl.GetBoolean(GetPName.DepthTest) ? GLEnum.True : GLEnum.False;
            GLEnum cullFaceEnabled = _gl.GetBoolean(GetPName.CullFace) ? GLEnum.True : GLEnum.False;

            _gl.Enable(EnableCap.Blend);
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            _gl.Enable(EnableCap.DepthTest);
            _gl.DepthFunc(DepthFunction.Lequal);
            _gl.Disable(EnableCap.CullFace);

            _shader.Use();

            foreach (var cameraEntity in cameras)
            {
                ref var transformComponent = ref _world.GetComponent<TransformComponent>(cameraEntity);
                ref var cameraComponent = ref _world.GetComponent<CameraComponent>(cameraEntity);

                Vector3[] frustumCorners = CalculateFrustumCorners(transformComponent, cameraComponent);
                _shader.UpdateFrustumVertices(frustumCorners);
                _shader.SetMVP(Matrix4x4.Identity, view, projection);
                _shader.SetColor(_defaultColor);
                _shader.Draw();
            }

            if (blendingEnabled == GLEnum.False) _gl.Disable(EnableCap.Blend);
            else _gl.Enable(EnableCap.Blend);

            if (depthTestEnabled == GLEnum.False) _gl.Disable(EnableCap.DepthTest);
            else _gl.Enable(EnableCap.DepthTest);

            if (cullFaceEnabled == GLEnum.True) _gl.Enable(EnableCap.CullFace);
            else _gl.Disable(EnableCap.CullFace);
        }

        private Vector3[] CalculateFrustumCorners(TransformComponent transform, CameraComponent camera)
        {
            Vector3[] frustumCornersLocal = new Vector3[8];

            float nearPlane = camera.NearPlane;
            float farPlane = camera.FarPlane;
            float fovY = camera.FieldOfView * (MathF.PI / 180f);
            float aspect = camera.AspectRatio;

            float nearHeight = 2.0f * MathF.Tan(fovY / 2.0f) * nearPlane;
            float nearWidth = nearHeight * aspect;
            float farHeight = 2.0f * MathF.Tan(fovY / 2.0f) * farPlane;
            float farWidth = farHeight * aspect;

            frustumCornersLocal[0] = new Vector3(-nearWidth / 2, -nearHeight / 2, -nearPlane);  // левый нижний
            frustumCornersLocal[1] = new Vector3(nearWidth / 2, -nearHeight / 2, -nearPlane);   // правый нижний
            frustumCornersLocal[2] = new Vector3(nearWidth / 2, nearHeight / 2, -nearPlane);    // правый верхний
            frustumCornersLocal[3] = new Vector3(-nearWidth / 2, nearHeight / 2, -nearPlane);   // левый верхний

            float visualFarPlane = Math.Min(farPlane, 20.0f);
            float visualFarHeight = 2.0f * MathF.Tan(fovY / 2.0f) * visualFarPlane;
            float visualFarWidth = visualFarHeight * aspect;

            frustumCornersLocal[4] = new Vector3(-visualFarWidth / 2, -visualFarHeight / 2, -visualFarPlane);  // левый нижний
            frustumCornersLocal[5] = new Vector3(visualFarWidth / 2, -visualFarHeight / 2, -visualFarPlane);   // правый нижний
            frustumCornersLocal[6] = new Vector3(visualFarWidth / 2, visualFarHeight / 2, -visualFarPlane);    // правый верхний
            frustumCornersLocal[7] = new Vector3(-visualFarWidth / 2, visualFarHeight / 2, -visualFarPlane);   // левый верхний

            Vector3[] frustumCornersWorld = new Vector3[8];
            Matrix4x4 cameraTransform = CreateModelMatrix(transform);

            for (int i = 0; i < 8; i++)
            {
                frustumCornersWorld[i] = Vector3.Transform(frustumCornersLocal[i], cameraTransform);
            }

            return frustumCornersWorld;
        }

        private Matrix4x4 CreateModelMatrix(TransformComponent transform)
        {
            Matrix4x4 rotationMatrix = Matrix4x4.CreateFromQuaternion(transform.Rotation.ToQuaternion());
            Matrix4x4 translationMatrix = Matrix4x4.CreateTranslation(transform.Position);
            Matrix4x4 scaleMatrix = Matrix4x4.CreateScale(transform.Scale);

            Matrix4x4 result = Matrix4x4.Identity;
            result *= rotationMatrix;
            result *= translationMatrix;

            return result;
        }

        private Matrix4x4 CreateOrthographicMatrix(CameraComponent camera, TransformComponent transform, EditorCameraComponent editorCamera)
        {
            float size = Vector3.Distance(transform.Position, editorCamera.Target) * 0.1f;
            return Matrix4x4.CreateOrthographic(
                size * camera.AspectRatio,
                size,
                camera.NearPlane,
                camera.FarPlane);
        }

        public void Resize(Vector2 size)
        {
            var cameras = _queryEditorCamera.Build();
            if (cameras.Length > 0)
            {
                ref var camera = ref _world.GetComponent<CameraComponent>(cameras[0]);
                camera.AspectRatio = size.X / size.Y;
            }
        }
    }
}