﻿using AtomEngine.RenderEntity;
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

        private Dictionary<uint, EntityData> _entitiesInScene = new Dictionary<uint, EntityData>();

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

            _camera = new EditorCamera(
                        position: new Vector3(5, 5, -10),
                        target: Vector3.Zero,
                        up: Vector3.UnitY,
                        root: _renderCanvas
                    );
            
            this.Focus();
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

            _renderTimer.Start();
            _isOpen = true;
            Status.SetStatus("Scene view opened");
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

            _isOpen = false;
            Status.SetStatus("Scene view closed");
        }

        public void Redraw()
        {
            UpdateEntitiesFromScene();
        }

        private void OnGLInitialized(GL gl)
        {
            try
            {
                // Получаем ResourceManager для загрузки ресурсов
                _resourceManager = ServiceHub.Get<ResourceManager>();

                // Инициализируем сетку для визуализации сцены
                InitializeGrid(gl);

                DebLogger.Debug("Scene renderer initialized successfully");
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
                // Создаем шейдер для сетки
                _gridShader = new GridShader(gl);

                DebLogger.Debug("Grid initialized successfully");
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

                // Рендерим сетку
                RenderGrid(gl, view, projection);

                // Рендерим все сущности сцены
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
                    // Ищем компонент трансформации
                    if (!TryGetTransformComponent(entity, out TransformComponent transform))
                        continue;

                    // Создаем матрицу модели из компонента трансформации
                    Matrix4x4 model = CreateModelMatrix(transform);

                    // Ищем шейдер и меш среди компонентов сущности
                    ShaderBase shader = null;
                    MeshBase mesh = null;

                    // Находим компоненты с шейдерами и мешами
                    FindRenderableComponents(entity, out shader, out mesh);

                    // Если нашли и шейдер, и меш - рендерим сущность
                    if (shader != null && mesh != null)
                    {
                        RenderEntityWithShaderAndMesh(gl, shader, mesh, model, view, projection);
                    }
                }
                catch (Exception ex)
                {
                    DebLogger.Error($"Ошибка при рендеринге сущности {entity.Name}: {ex.Message}");
                }
            }
        }

        private void FindRenderableComponents(EntityData entity, out ShaderBase shader, out MeshBase mesh)
        {
            shader = null;
            mesh = null;

            var glDependableComponents = FindGLDependableComponents(entity);

            foreach (var component in glDependableComponents)
            {
                // Ищем шейдер, если он еще не найден
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
                                    shaderField.SetValue(component, shader);
                                    break;
                                }
                            }
                        }
                    }
                }

                // Ищем меш, если он еще не найден
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
                                    meshField.SetValue(component, mesh);
                                    break;
                                }
                            }
                        }
                    }
                }

                // Если нашли и шейдер, и меш, можно прекратить поиск
                if (shader != null && mesh != null)
                    break;
            }
        }

        private unsafe void RenderEntityWithShaderAndMesh(GL gl, ShaderBase shader, MeshBase mesh, Matrix4x4 model, Matrix4x4 view, Matrix4x4 projection)
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
                //model *= Matrix4x4.CreateScale(transform.Scale);
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
            // Проверяем, есть ли атрибут GLDependable на типе компонента
            var componentType = component.GetType();
            var glDependableAttr = componentType.GetCustomAttribute(typeof(GLDependableAttribute), true);

            if (glDependableAttr != null)
                return true;

            // Проверяем, содержит ли компонент поля типа ShaderBase, MeshBase или Texture
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

            // Ищем поля с шейдерами и мешами в компоненте
            var shaderField = FindFieldsByBaseType(componentType, typeof(ShaderBase)).FirstOrDefault();
            var meshField = FindFieldsByBaseType(componentType, typeof(MeshBase)).FirstOrDefault();

            if (shaderField == null || meshField == null)
                return;

            // Получаем соответствующие GUID поля
            var shaderGuidField = componentType.GetField(shaderField.Name + "GUID", BindingFlags.NonPublic | BindingFlags.Instance);
            var meshGuidField = componentType.GetField(meshField.Name + "GUID", BindingFlags.NonPublic | BindingFlags.Instance);

            if (shaderGuidField == null || meshGuidField == null)
                return;

            // Получаем значения GUID
            string shaderGuid = (string)shaderGuidField.GetValue(component);
            string meshGuid = (string)meshGuidField.GetValue(component);

            if (string.IsNullOrEmpty(shaderGuid) || string.IsNullOrEmpty(meshGuid))
                return;

            // Получаем шейдер и меш из ResourceManager
            var shader = _resourceManager.GetResource<ShaderBase>(shaderGuid);
            var mesh = _resourceManager.GetResource<MeshBase>(meshGuid);

            if (shader != null && mesh != null)
            {
                // Устанавливаем значения полей компонента
                shaderField.SetValue(component, shader);
                meshField.SetValue(component, mesh);

                // Рендерим с использованием этих ресурсов
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

            _renderTimer?.Stop();
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