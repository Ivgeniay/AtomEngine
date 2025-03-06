using AtomEngine.RenderEntity;
using AtomEngine;
using Avalonia.Controls;
using Avalonia.Input;
using System.Collections.Generic;
using Avalonia.Threading;
using System.Numerics;
using Avalonia.Layout;
using Avalonia;
using System;
using Silk.NET.OpenGL;
using Avalonia.Media;
using System.Linq;
using System.Reflection;

namespace Editor
{

    internal class SceneViewController : ContentControl, IWindowed
    {
        private GLController _glController;
        private ResourceManager _resourceManager;
        private DispatcherTimer _renderTimer;
        private Grid _mainGrid;
        private Border _toolbarBorder;
        private Grid _renderCanvas;

        private EditorCamera _camera;

        private Dictionary<EntityData, RenderPairCache> _componentRenderCache = new Dictionary<EntityData, RenderPairCache>();
        private Dictionary<uint, EntityData> _entitiesInScene = new Dictionary<uint, EntityData>();

        private SceneManager _sceneManager;
        private ProjectScene _currentScene;

        private GridShader _gridShader;
        private TransformMode _currentTransformMode = TransformMode.Translate;
        private bool _isPerspective = true;
        private bool _isOpen = false;

        public Action<object> OnClose { get; set; }

        public SceneViewController()
        {
            InitializeUI();
            InitializeEvents();

            _sceneManager = ServiceHub.Get<SceneManager>();
            _camera = new EditorCamera(
                        position: new Vector3(5, 5, -10),
                        target: Vector3.Zero,
                        up: Vector3.UnitY,
                        root: _renderCanvas
                    );
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
            _renderTimer.Tick += (sender, args) => Render();


        }

        public void SetScene(ProjectScene scene)
        {
            _currentScene = scene;
            UpdateEntitiesFromScene();
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

            _sceneManager.OnSceneBeforeSave += PrepareToSave;

            _renderTimer.Start();
            _isOpen = true;
            this.Focus();
        }

        public void Close()
        {
            _renderTimer.Stop();

            if (_glController != null)
            {
                _renderCanvas.Children.Remove(_glController);
                Dispose();
                _glController = null;
            }
            _sceneManager.OnSceneBeforeSave -= PrepareToSave;

            _isOpen = false;
        }

        public void Redraw()
        {
            UpdateEntitiesFromScene();
        }

        private void OnGLInitialized(GL gl)
        {
            try
            {
                _resourceManager = ServiceHub.Get<ResourceManager>();
                InitializeGrid(gl);
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

        private void OnRender(GL gl)
        {
            try
            {
                gl.ClearColor(0.2f, 0.2f, 0.2f, 1.0f);
                gl.Enable(EnableCap.DepthTest);

                var view = _camera.GetViewMatrix();
                Matrix4x4 projection = _camera.GetProjection(_isPerspective);

                RenderGrid(gl, view, projection);
                RenderEntities(gl, view, projection);
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
            foreach (var entity in _entitiesInScene.Values)
            {
                try
                {
                    if (!TryGetTransformComponent(entity, out TransformComponent transform))
                        continue;

                    Matrix4x4 model = CreateModelMatrix(transform);
                    if (_componentRenderCache.TryGetValue(entity, out RenderPairCache renderPairCache))
                    {
                        RenderEntityWithShaderAndMesh(gl, renderPairCache.Shader, renderPairCache.Mesh, model, view, projection);
                    }
                    else
                    {
                        ShaderBase shader = null;
                        MeshBase mesh = null;
                        FieldInfo shaderFieldInfo = null;
                        FieldInfo meshFieldInfo = null;
                        Object shaderObject = null;
                        Object meshObject = null;

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
                            RenderEntityWithShaderAndMesh(gl, shader, mesh, model, view, projection);
                        }
                    }

                }
                catch (Exception ex)
                {
                    DebLogger.Error($"Ошибка при рендеринге сущности {entity.Name}: {ex.Message}");
                }
            }
        }

        private void FindRenderableComponents(EntityData entity, out ShaderBase shader, out FieldInfo shaderFieldInfo, out MeshBase mesh, out FieldInfo meshFieldInfo, out object shaderObj, out object meshObj)
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

        private void RenderGLDependableComponent(GL gl, IComponent component, Matrix4x4 model, Matrix4x4 view, Matrix4x4 projection)
        {
            var componentType = component.GetType();

            var shaderField = FindFieldsByBaseType(componentType, typeof(ShaderBase)).FirstOrDefault();
            var meshField = FindFieldsByBaseType(componentType, typeof(MeshBase)).FirstOrDefault();

            if (shaderField == null || meshField == null)
                return;

            var shaderGuidField = componentType.GetField(shaderField.Name + "GUID", BindingFlags.NonPublic | BindingFlags.Instance);
            var meshGuidField = componentType.GetField(meshField.Name + "GUID", BindingFlags.NonPublic | BindingFlags.Instance);

            if (shaderGuidField == null || meshGuidField == null)
                return;

            string shaderGuid = (string)shaderGuidField.GetValue(component);
            string meshGuid = (string)meshGuidField.GetValue(component);

            if (string.IsNullOrEmpty(shaderGuid) || string.IsNullOrEmpty(meshGuid))
                return;

            var shader = _resourceManager.GetResource<ShaderBase>(shaderGuid);
            var mesh = _resourceManager.GetResource<MeshBase>(meshGuid);

            if (shader != null && mesh != null)
            {
                shaderField.SetValue(component, shader);
                meshField.SetValue(component, mesh);

                RenderEntity(gl, shader, mesh, model, view, projection);
            }
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
            if (_currentScene == null)
                return;

            _entitiesInScene.Clear();

            foreach (var entity in _currentScene.CurrentWorldData.Entities)
            {
                _entitiesInScene[entity.Id] = entity;
            }
        }

        private void Render()
        {
            _glController?.Invalidate();
        }

        #region Обработка пользовательского ввода

        private void OnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            _camera.HandlePointerPressed(e.GetPosition(this), e.GetCurrentPoint(this).Properties.PointerUpdateKind);
        }

        private void OnPointerReleased(object sender, PointerReleasedEventArgs e)
        {
            _camera.HandlePointerReleased(e.GetCurrentPoint(this).Properties.PointerUpdateKind);

            //var properties = e.GetCurrentPoint(_renderCanvas).Properties;
            //if (e.InitialPressMouseButton == MouseButton.Left)
            //{
            //    _isLeftMouseDown = false;
            //    if (!_isRightMouseDown && !_isMiddleMouseDown)
            //    {
            //        // Выбор объекта по клику
            //        PickObject(e.GetPosition(_renderCanvas));
            //    }
            //}
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
            // TODO: Реализовать выбор объекта по клику
            // Для этого потребуется рейкастинг в сцене
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

        public void Dispose()
        {
            if (_glController != null)
            {
                GLController.OnGLInitialized -= OnGLInitialized;
                GLController.OnRender -= OnRender;
                _glController.Dispose();
                _glController = null;
            }

            if (_gridShader != null)
            {
                _gridShader.Dispose();
                _gridShader = null;
            }

            SetDefaulFieldValue();
            FreeChache();
            _resourceManager.Dispose();
            _renderTimer?.Stop();
        }

        internal void ComponentChange(uint worldId, uint entityId, IComponent component)
        {
            
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

        #endregion

        private enum TransformMode
        {
            Translate,
            Rotate,
            Scale
        }


    }


}