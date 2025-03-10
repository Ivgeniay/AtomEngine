using System.Collections.Concurrent;
using System.Collections.Generic;
using AtomEngine.RenderEntity;
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
using System.Threading.Tasks;

namespace Editor
{


    internal class SceneViewController : ContentControl, IWindowed, IDisposableController
    {
        public Action<uint> OnEntitySelected;

        private GLController _glController;
        private ResourceManager _resourceManager;
        private DispatcherTimer _renderTimer;
        private Grid _mainGrid;
        private Border _toolbarBorder;
        private Grid _renderCanvas;

        private EditorCamera _camera;
        private AABBManager _aabbManager;
        private SceneEntityComponentProvider sceneEntityComponentProvider;

        private Dictionary<EntityData, RenderPairCache> _componentRenderCache = new Dictionary<EntityData, RenderPairCache>();

        private ConcurrentQueue<OpenGLCommand> _glCommands = new ConcurrentQueue<OpenGLCommand>();

        private SceneManager _sceneManager;
        private ProjectScene _currentScene;
        private MaterialFactory _materialFactory;

        private GridShader _gridShader;
        private TransformMode _currentTransformMode = TransformMode.Translate;
        private bool _isPerspective = true;
        private bool _isOpen = false;
        private bool _isGlInitialized = false;

        private bool _isPreparingClose = false;
        private TaskCompletionSource<bool> _disposeTcs;

        public Action<object> OnClose { get; set; }

        public SceneViewController()
        {
            InitializeUI();
            InitializeEvents();

            _camera = new EditorCamera(
                        position: new Vector3(5, 5, -10),
                        target: Vector3.Zero,
                        up: Vector3.UnitY,
                        root: _renderCanvas
                    );

            _resourceManager = ServiceHub.Get<ResourceManager>();
            _materialFactory = ServiceHub.Get<MaterialFactory>();
            _sceneManager = ServiceHub.Get<SceneManager>();

            _sceneManager.OnSceneInitialize += SetScene;
            _sceneManager.OnScenUnload += UnloadScene;

            sceneEntityComponentProvider = new SceneEntityComponentProvider(_sceneManager);
            BVHTree.Instance.Initialize(sceneEntityComponentProvider);
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
            _renderCanvas.PointerPressed += OnPointerPressed;
            _renderCanvas.PointerReleased += OnPointerReleased;
            _renderCanvas.PointerMoved += OnPointerMoved;
            _renderCanvas.PointerWheelChanged += OnPointerWheelChanged;

            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;

            _renderTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            int counter = 0;
            _renderTimer.Tick += (sender, args) =>
            {
                Render();
            };
        }

        private void SetScene(ProjectScene scene)
        {
            _currentScene = scene;
            if (!_isOpen) return;

            EnqueueGLCommand((gl) =>
            {
                UpdateEntitiesFromScene();
            });
        }
        
        public void UnloadScene()
        {
            _currentScene = null;
            if (!_isOpen) return;

            EnqueueGLCommand((gl) =>
            {
                FreeChache();
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

            _renderTimer.Start();
            _isOpen = true;
            this.Focus();
        }

        public void Close()
        { }

        public void Redraw()
        {
            UpdateEntitiesFromScene();
        }

        private void OnGLInitialized(GL gl)
        {
            try
            {
                _isGlInitialized = true;
                InitializeGrid(gl);
                InitializeAABBManager(gl);
                UpdateEntitiesFromScene();
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Failed to initialize scene renderer: {ex.Message}");
            }
        }

        private void InitializeGrid(GL gl)
        {
            try
            {
                _gridShader = new GridShader(gl);
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Failed to initialize grid: {ex.Message}");
            }
        }

        private void InitializeAABBManager(GL gl)
        {
            try
            {
                _aabbManager = new AABBManager(gl, sceneEntityComponentProvider);
            }
            catch (Exception ex)
            {
            }
        }

        private void OnRender(GL gl)
        {
            try
            {
                if (!_isGlInitialized) return;


                var scalingFactor = VisualRoot?.RenderScaling ?? 1.0;
                uint width = (uint)(_renderCanvas.Bounds.Width * scalingFactor);
                uint height = (uint)(_renderCanvas.Bounds.Height * scalingFactor);

                gl.Viewport(0, 0, width, height);
                gl.ClearColor(0.2f, 0.2f, 0.2f, 1.0f);
                gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                gl.Enable(EnableCap.DepthTest);
                gl.DepthFunc(DepthFunction.Lequal);

                var view = _camera.GetViewMatrix();
                Matrix4x4 projection = _camera.GetProjection(_isPerspective);

                while (_glCommands.TryDequeue(out var command))
                {
                    command.Execute(gl);
                }

                RenderGrid(gl, view, projection);
                RenderEntities(gl, view, projection);

                _aabbManager?.Render(view, projection);

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

        private void RenderGrid(GL gl, Matrix4x4 view, Matrix4x4 projection)
        {
            if (_gridShader != null)
            {
                try
                {
                    _gridShader.Use();
                    _gridShader.SetMVP(Matrix4x4.Identity, view, projection);
                    _gridShader.Draw();
                }
                catch (Exception ex)
                {
                    DebLogger.Error($"Ошибка при рендеринге сетки: {ex.Message}");
                }
            }
        }

        private void RenderEntities(GL gl, Matrix4x4 view, Matrix4x4 projection)
        {
            foreach (var kvp in _componentRenderCache)
            {
                try
                {
                    var entity = kvp.Key;
                    var renderPairCache = kvp.Value;

                    if (!TryGetTransformComponent(entity, out TransformComponent transform))
                        continue;

                    if (renderPairCache.Shader == null || renderPairCache.Mesh == null)
                        continue;

                    Matrix4x4 model = CreateModelMatrix(transform);
                    RenderEntityWithShaderAndMesh(gl, renderPairCache.Shader, renderPairCache.Mesh, model, view, projection);
                }
                catch (Exception ex)
                {
                    DebLogger.Error($"Ошибка при рендеринге сущности: {ex.Message}");
                }
            }
        }

        private void FindRenderableComponents(
            EntityData entity, 
            out ShaderBase shader, 
            out FieldInfo shaderFieldInfo, 
            out MeshBase mesh, 
            out FieldInfo meshFieldInfo, 
            out object shaderObj, 
            out object meshObj)
        {
            shader = null;
            mesh = null;
            shaderFieldInfo = null;
            meshFieldInfo = null;
            shaderObj = null;
            meshObj = null;

            var glDependableComponents = FindGLDependableComponents(entity);

            foreach (var component in glDependableComponents)
            {
                if (shader == null)
                {
                    var shaderFields = FindFieldsByBaseType(component.GetType(), typeof(ShaderBase));
                    foreach (var shaderField in shaderFields)
                    {
                        var shaderGuidField = component.GetType().GetField(shaderField.Name + "GUID",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                        if (shaderGuidField != null)
                        {
                            string shaderGuid = (string)shaderGuidField.GetValue(component);
                            if (!string.IsNullOrEmpty(shaderGuid))
                            {
                                shader = _resourceManager.GetResource<ShaderBase>(shaderGuid);
                                if (shader != null)
                                {
                                    shaderFieldInfo = shaderField;
                                    shaderField.SetValue(component, shader);
                                    shaderObj = component;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (mesh == null)
                {
                    var meshFields = FindFieldsByBaseType(component.GetType(), typeof(MeshBase));
                    foreach (var meshField in meshFields)
                    {
                        var meshGuidField = component.GetType().GetField(meshField.Name + "GUID",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                        if (meshGuidField != null)
                        {
                            string meshGuid = (string)meshGuidField.GetValue(component);
                            if (!string.IsNullOrEmpty(meshGuid))
                            {
                                mesh = _resourceManager.GetResource<MeshBase>(meshGuid);
                                if (mesh != null)
                                {
                                    meshFieldInfo = meshField;
                                    meshField.SetValue(component, mesh);
                                    meshObj = component;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (shader != null && mesh != null)
                    break;
            }
        }

        private unsafe void RenderEntityWithShaderAndMesh(GL gl, ShaderBase shader, MeshBase mesh, Matrix4x4 model, Matrix4x4 view, Matrix4x4 projection)
        {
            shader.Use();

            try
            {
                var modelLoc = gl.GetUniformLocation(shader.Handle, "model");
                var viewLoc = gl.GetUniformLocation(shader.Handle, "view");
                var projLoc = gl.GetUniformLocation(shader.Handle, "projection");

                if (modelLoc >= 0)
                    gl.UniformMatrix4(modelLoc, 1, false, GetMatrix4x4Values(model));

                if (viewLoc >= 0)
                    gl.UniformMatrix4(viewLoc, 1, false, GetMatrix4x4Values(view));

                if (projLoc >= 0)
                    gl.UniformMatrix4(projLoc, 1, false, GetMatrix4x4Values(projection));
            }
            catch (Exception ex)
            {
                DebLogger.Warn($"Не удалось установить MVP матрицы в шейдер: {ex.Message}");
            }

            mesh.Draw(shader);
        }
       
        private bool TryGetTransformComponent(EntityData entity, out TransformComponent transform)
        {
            transform = new TransformComponent();

            if (entity.Components.TryGetValue(nameof(TransformComponent), out var transformComponent))
            {
                transform = (TransformComponent)transformComponent;
                return true;
            }

            return false;
        }

        private Matrix4x4 CreateModelMatrix(TransformComponent transform)
        {
            Matrix4x4 model = Matrix4x4.CreateTranslation(transform.Position);

            if (transform.Rotation != Vector3.Zero)
            {
                model *= Matrix4x4.CreateFromYawPitchRoll(
                    transform.Rotation.Y * (MathF.PI / 180.0f),
                    transform.Rotation.X * (MathF.PI / 180.0f),
                    transform.Rotation.Z * (MathF.PI / 180.0f));
            }

            if (transform.Scale != Vector3.One)
            {
                model *= Matrix4x4.CreateScale(transform.Scale);
            }

            return model;
        }

        private List<IComponent> FindGLDependableComponents(EntityData entity)
        {
            var result = new List<IComponent>();

            foreach (var component in entity.Components.Values)
            {
                if (IsGLDependableComponent(component))
                {
                    result.Add(component);
                }
            }

            return result;
        }

        private bool IsGLDependableComponent(IComponent component)
        {
            var componentType = component.GetType();
            var glDependableAttr = componentType.GetCustomAttribute(typeof(GLDependableAttribute), true);

            if (glDependableAttr != null)
                return true;

            var fields = componentType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (GLDependableTypes.IsDependableType(field.FieldType))
                {
                    return true;
                }
            }

            return false;
        }

        private List<FieldInfo> FindFieldsByBaseType(Type componentType, Type baseType)
        {
            return componentType.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => baseType.IsAssignableFrom(f.FieldType))
                .ToList();
        }

        private unsafe void RenderEntity(GL gl, ShaderBase shader, MeshBase mesh, Matrix4x4 model, Matrix4x4 view, Matrix4x4 projection)
        {
            // Используем шейдер
            shader.Use();

            // Устанавливаем MVP матрицы в шейдер, если шейдер поддерживает эти uniform-переменные
            try
            {
                // Получаем местоположения uniform-переменных
                var modelLoc = gl.GetUniformLocation(shader.Handle, "model");
                var viewLoc = gl.GetUniformLocation(shader.Handle, "view");
                var projLoc = gl.GetUniformLocation(shader.Handle, "projection");

                // Устанавливаем uniform-переменные
                if (modelLoc >= 0)
                    gl.UniformMatrix4(modelLoc, 1, false, GetMatrix4x4Values(model));

                if (viewLoc >= 0)
                    gl.UniformMatrix4(viewLoc, 1, false, GetMatrix4x4Values(view));

                if (projLoc >= 0)
                    gl.UniformMatrix4(projLoc, 1, false, GetMatrix4x4Values(projection));
            }
            catch (Exception ex)
            {
                DebLogger.Warn($"Не удалось установить MVP матрицы в шейдер: {ex.Message}");
            }

            // Рендерим меш
            mesh.Draw(shader);
        }

        private unsafe float* GetMatrix4x4Values(Matrix4x4 matrix)
        {
            // Преобразуем Matrix4x4 в массив float для OpenGL
            // Примечание: OpenGL использует порядок столбцов, а не строк
            return (float*)&matrix;
        }

        private void UpdateEntitiesFromScene()
        {
            FreeChache();
            if (!_isOpen || !_isGlInitialized) return;

            if (_currentScene == null)
                return;

            InitializeComponentCache();
        }

        private void Render()
        {
            _glController?.Invalidate();
        }

        public void EnqueueGLCommand(Action<GL> command)
        {
            _glCommands.Enqueue(new OpenGLCommand { Execute = command });
        }


        private void OnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            _camera.HandlePointerPressed(e.GetPosition(this), e.GetCurrentPoint(this).Properties.PointerUpdateKind);
        }

        private void OnPointerReleased(object sender, PointerReleasedEventArgs e)
        {
            _camera.HandlePointerReleased(e.GetCurrentPoint(this).Properties.PointerUpdateKind);
            if (e.InitialPressMouseButton == MouseButton.Left)
            {
                PickObject(e.GetPosition(_renderCanvas));
            }
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            _camera.HandlePointerMoved(e.GetPosition(this));
        }

        private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            _camera.HandlePointerWheelChanged(e.Delta);
        }

        private void PickObject(Point point)
        {
            if (!_isOpen || !_isGlInitialized || _currentScene == null)
                return;

            float x = (float)(point.X / _renderCanvas.Bounds.Width) * 2 - 1;
            float y = 1 - (float)(point.Y / _renderCanvas.Bounds.Height) * 2;

            var rayDirection = CalculateRayDirection(x, y);
            var ray = new BVHTree.BvhRay(_camera.Position, rayDirection);

            if (ray.Raycast(out var hit))
            {
                // Находим соответствующий EntityData
                var entity = _currentScene.CurrentWorldData.Entities.FirstOrDefault(e => e.Id == hit.EntityId);
                if (entity != null)
                {
                    OnEntitySelected?.Invoke(entity.Id);
                    //// Создаем иерархический элемент и выбираем его
                    //var hierarchyItem = new EntityHierarchyItem(entity.Id, entity.Version, entity.Name);
                    //Select.SelectItem(hierarchyItem);
                    //Status.SetStatus($"Выбран объект: {entity.Name}");

                    //// Также можно вызвать обработчик события EntitySelected
                    //ServiceHub.Get<InspectorDistributor>()?.GetInspectable(hierarchyItem);
                }
            }
        }
        private Vector3 CalculateRayDirection(float normalizedX, float normalizedY)
        {
            Vector4 clipCoords = new Vector4(normalizedX, normalizedY, -1.0f, 1.0f);

            Matrix4x4.Invert(_camera.GetProjection(_isPerspective), out var invProjection);
            Vector4 eyeCoords = Vector4.Transform(clipCoords, invProjection);
            eyeCoords = new Vector4(eyeCoords.X, eyeCoords.Y, -1.0f, 0.0f);

            Matrix4x4.Invert(_camera.GetViewMatrix(), out var invView);
            Vector4 rayWorld = Vector4.Transform(eyeCoords, invView);
            Vector3 rayDirection = new Vector3(rayWorld.X, rayWorld.Y, rayWorld.Z);

            return Vector3.Normalize(rayDirection);
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            _camera.HandleKeyboardInput(e);
        }

        private void OnKeyUp(object? sender, KeyEventArgs e)
        {
            
        }

        private void SetTransformMode(TransformMode mode)
        {
            _currentTransformMode = mode;
            Status.SetStatus($"Transform mode: {mode}");
        }

        private void TogglePerspective()
        {
            _isPerspective = !_isPerspective;
            Status.SetStatus($"Camera projection: {(_isPerspective ? "Perspective" : "Orthographic")}");
        }

        private void PrepareToSave()
        {
            SetDefaulFieldValue();
            FreeChache();
        }
        
        private void FreeChache()
        {
            _componentRenderCache.Clear();
        }
        
        private void SetDefaulFieldValue()
        {
            foreach(var kvp in _componentRenderCache)
            {
                RenderPairCache cache = kvp.Value;
                cache.ShaderField.SetValue(cache.ShaderObject, null);
                cache.MeshField.SetValue(cache.MeshObject, null);
            }
        }


        private void InitializeComponentCache()
        {
            if (!_isOpen || !_isGlInitialized) return;

            if (_currentScene == null || _currentScene.CurrentWorldData == null)
                return;

            
            FreeChache();
            foreach (var entity in _currentScene.CurrentWorldData.Entities)
            {
                CacheEntityComponents(entity);
            }
        }
        public void EntityCreated(uint worldId, uint entityId)
        {
            if (!_isOpen) return;

            EnqueueGLCommand((gl) =>
            {
                EntityData entity = _currentScene.CurrentWorldData.Entities.FirstOrDefault(e => e.Id == entityId);
                if (entity == null)
                    return;

                CacheEntityComponents(entity);
            });
        }
        public void EntityRemoved(uint worldId, uint entityId)
        {
            if (!_isOpen) return;

            EnqueueGLCommand((gl) =>
            {
                var entityToRemove = _componentRenderCache.Keys.FirstOrDefault(e => e.Id == entityId);
                if (entityToRemove != null)
                {
                    _componentRenderCache.Remove(entityToRemove);
                    BVHTree.Instance.RemoveEntity(entityId);
                    _aabbManager.RemoveEntity(entityId);
                }
            });
        }
        public void ComponentAdded(uint worldId, uint entityId, IComponent component)
        {
            if (!_isOpen) return;

            EnqueueGLCommand((gl) =>
            {
                var entity = _componentRenderCache.Keys.FirstOrDefault(e => e.Id == entityId);

                if (entity != null)
                {
                    _componentRenderCache.Remove(entity);
                }
                else
                {
                    entity = _currentScene?.CurrentWorldData.Entities.FirstOrDefault(e => e.Id == entityId);
                }

                if (entity != null)
                {
                    CacheEntityComponents(entity);
                }
            });
        }
        public void ComponentRemoved(uint worldId, uint entityId, IComponent component)
        {
            if (!_isOpen) return;

            EnqueueGLCommand((gl) =>
            {
                var entity = _componentRenderCache.Keys.FirstOrDefault(e => e.Id == entityId);

                if (entity != null)
                {
                    var cache = _componentRenderCache[entity];
                    if ((cache.ShaderObject?.GetType() == component.GetType()) ||
                        (cache.MeshObject?.GetType() == component.GetType()))
                    {
                        _componentRenderCache.Remove(entity);
                        CacheEntityComponents(entity);
                        if (component.GetType() == cache.MeshObject?.GetType())
                        {
                            BVHTree.Instance.RemoveEntity(entityId);
                            _aabbManager.RemoveEntity(entity.Id);
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
                var entity = _componentRenderCache.Keys.FirstOrDefault(e => e.Id == entityId);
                if (entity != null)
                {
                    var cache = _componentRenderCache[entity];
                    if (cache.ShaderObject == component || cache.MeshObject == component)
                    {
                        _componentRenderCache.Remove(entity);
                        BVHTree.Instance.RemoveEntity(entityId);
                        _aabbManager.RemoveEntity(entity.Id);
                        CacheEntityComponents(entity);
                    }
                }
                else
                {
                    entity = _currentScene?.CurrentWorldData.Entities.FirstOrDefault(e => e.Id == entityId);
                    if (entity != null)
                    {
                        CacheEntityComponents(entity);
                    }
                }

                if (component is TransformComponent transform)
                {
                    BVHTree.Instance.UpdateEntity(entityId);
                    _aabbManager.UpdateEntity(entityId);
                }
            });
        }

        private void CacheEntityComponents(EntityData entity)
        {
            if (!TryGetTransformComponent(entity, out _))
                return;

            ShaderBase shader = null;
            MeshBase mesh = null;
            FieldInfo shaderFieldInfo = null;
            FieldInfo meshFieldInfo = null;
            object shaderObject = null;
            object meshObject = null;

            FindRenderableComponents(entity, out shader, out shaderFieldInfo, out mesh, out meshFieldInfo, out shaderObject, out meshObject);

            if (shader != null && mesh != null)
            {
                _componentRenderCache[entity] = new RenderPairCache()
                {
                    Shader = shader,
                    ShaderField = shaderFieldInfo,
                    Mesh = mesh,
                    MeshField = meshFieldInfo,
                    ShaderObject = shaderObject,
                    MeshObject = meshObject
                };
                BVHTree.Instance.AddEntity(entity.Id);
                _aabbManager.AddEntity(entity.Id, mesh);
            }
        }
        public void Dispose()
        {
            SetDefaulFieldValue();
            FreeChache();
            _materialFactory.SetSceneViewController(null);

            if (_glController != null)
            {
                _renderCanvas.Children.Remove(_glController);
                GLController.OnGLInitialized -= OnGLInitialized;
                GLController.OnRender -= OnRender;
                _glController.Dispose();
                _glController = null;
            }

            if (_gridShader != null)
            {
                //_gridShader.Dispose();
                _gridShader = null;
            }
            if (_aabbManager != null)
            {
                //_aabbManager.Dispose();
                _aabbManager = null;
            }
            _isGlInitialized = false;

            //_resourceManager?.Dispose();
            _sceneManager.OnSceneBeforeSave -= PrepareToSave;

            _sceneManager.OnComponentChange -= ComponentChange;
            _sceneManager.OnComponentAdded -= ComponentAdded;
            _sceneManager.OnComponentRemoved -= ComponentRemoved;
            _sceneManager.OnEntityCreated -= EntityCreated;
            _sceneManager.OnEntityRemoved -= EntityRemoved;

            _renderTimer.Stop();
            _isOpen = false;
            _isGlInitialized = false;
        }

        public async Task PrepareForCloseAsync()
        {
            if (_isPreparingClose)
                return;

            _isPreparingClose = true;
            _disposeTcs = new TaskCompletionSource<bool>();

            await Task.WhenAny(_disposeTcs.Task, Task.Delay(500));

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
}