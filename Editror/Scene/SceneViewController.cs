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

        // Камера редактора
        private Vector3 _cameraPosition = new Vector3(5, 5, -10);
        private Vector3 _cameraTarget = Vector3.Zero;
        private Vector3 _cameraUp = Vector3.UnitY;
        private float _cameraSpeed = 0.1f;
        private float _cameraRotationSpeed = 0.01f;

        // Состояние мыши
        private Point _lastMousePosition;
        private bool _isLeftMouseDown;
        private bool _isRightMouseDown;
        private bool _isMiddleMouseDown;

        // Выбранные объекты в сцене
        private Dictionary<uint, EntityData> _entitiesInScene = new Dictionary<uint, EntityData>();

        // Текущая сцена
        private ProjectScene _currentScene;

        // Grid для визуализации сцены
        private GridShader _gridShader;
        private TransformMode _currentTransformMode = TransformMode.Translate;
        private bool _isPerspective = true;
        private bool _isOpen = false;

        public Action<object> OnClose { get; set; }

        public SceneViewController()
        {
            InitializeUI();
            InitializeEvents();
        }

        private void InitializeUI()
        {
            _mainGrid = new Grid();
            _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });

            // Панель инструментов сцены
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

            // Кнопки для управления сценой
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

            // Настройка таймера рендеринга
            _renderTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
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

                Matrix4x4 view = Matrix4x4.CreateLookAt(
                    _cameraPosition,
                    _cameraTarget,
                    _cameraUp);

                float aspectRatio = (float)_renderCanvas.Bounds.Width / (float)_renderCanvas.Bounds.Height;
                Matrix4x4 projection;

                if (_isPerspective)
                {
                    projection = Matrix4x4.CreatePerspectiveFieldOfView(
                        MathF.PI / 4.0f,  // 45 градусов
                        aspectRatio,
                        0.1f,
                        1000.0f);
                }
                else
                {
                    float size = Vector3.Distance(_cameraPosition, _cameraTarget) * 0.1f;
                    projection = Matrix4x4.CreateOrthographic(
                        size * aspectRatio,
                        size,
                        0.1f,
                        1000.0f);
                }

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

                    // Ищем все GL-зависимые компоненты в сущности
                    var glDependableComponents = FindGLDependableComponents(entity);

                    foreach (var component in glDependableComponents)
                    {
                        RenderGLDependableComponent(gl, component, model, view, projection);
                    }
                }
                catch (Exception ex)
                {
                    DebLogger.Error($"Ошибка при рендеринге сущности {entity.Name}: {ex.Message}");
                }
            }
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
            // Запускаем рендеринг через GLController
            _glController?.Invalidate();
        }

        #region Обработка пользовательского ввода

        private void OnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            var point = e.GetPosition(_renderCanvas);
            _lastMousePosition = point;

            var properties = e.GetCurrentPoint(_renderCanvas).Properties;

            if (properties.IsLeftButtonPressed)
                _isLeftMouseDown = true;

            if (properties.IsRightButtonPressed)
                _isRightMouseDown = true;

            if (properties.IsMiddleButtonPressed)
                _isMiddleMouseDown = true;
        }

        private void OnPointerReleased(object sender, PointerReleasedEventArgs e)
        {
            var properties = e.GetCurrentPoint(_renderCanvas).Properties;

            if (e.InitialPressMouseButton == MouseButton.Left)
            {
                _isLeftMouseDown = false;
                if (!_isRightMouseDown && !_isMiddleMouseDown)
                {
                    // Выбор объекта по клику
                    PickObject(e.GetPosition(_renderCanvas));
                }
            }

            if (e.InitialPressMouseButton == MouseButton.Right)
                _isRightMouseDown = false;

            if (e.InitialPressMouseButton == MouseButton.Middle)
                _isMiddleMouseDown = false;
        }

        private void OnPointerMoved(object sender, PointerEventArgs e)
        {
            var point = e.GetPosition(_renderCanvas);
            var delta = point - _lastMousePosition;

            // Вращение камеры с правой кнопкой мыши
            if (_isRightMouseDown)
            {
                RotateCamera(delta);
            }

            // Панорамирование с средней кнопкой мыши
            if (_isMiddleMouseDown)
            {
                PanCamera(delta);
            }

            _lastMousePosition = point;
        }

        private void OnPointerWheelChanged(object sender, PointerWheelEventArgs e)
        {
            // Зум с колесиком мыши
            ZoomCamera(e.Delta.Y);
        }

        private void RotateCamera(Point delta)
        {
            // Вращение камеры вокруг центра
            var angleY = delta.X * _cameraRotationSpeed;
            var angleX = delta.Y * _cameraRotationSpeed;

            // Вектор от цели к камере
            var direction = _cameraPosition - _cameraTarget;

            // Поворот вокруг оси Y (влево-вправо)
            var rotationY = Matrix4x4.CreateRotationY((float)-angleY);
            direction = Vector3.Transform(direction, rotationY);

            // Вычисляем правый вектор камеры
            var right = Vector3.Cross(_cameraUp, direction);
            right = Vector3.Normalize(right);

            // Поворот вокруг правого вектора (вверх-вниз)
            var rotationX = Matrix4x4.CreateFromAxisAngle(right, (float)-angleX);
            direction = Vector3.Transform(direction, rotationX);

            // Обновляем позицию камеры
            _cameraPosition = _cameraTarget + direction;
        }

        private void PanCamera(Point delta)
        {
            // Перемещение камеры в плоскости просмотра
            var direction = _cameraPosition - _cameraTarget;
            var distance = direction.Length();

            // Вычисляем правый вектор камеры
            var forward = Vector3.Normalize(direction);
            var right = Vector3.Cross(_cameraUp, forward);
            right = Vector3.Normalize(right);

            // Перемещение вправо/влево
            var rightOffset = right * (float)delta.X * _cameraSpeed * (distance * 0.01f);
            // Перемещение вверх/вниз
            var upOffset = _cameraUp * (float)delta.Y * _cameraSpeed * (distance * 0.01f);

            // Обновляем позицию и цель камеры
            _cameraPosition -= rightOffset - upOffset;
            _cameraTarget -= rightOffset - upOffset;
        }

        private void ZoomCamera(double delta)
        {
            // Приближение/отдаление камеры
            var direction = _cameraPosition - _cameraTarget;
            var forward = Vector3.Normalize(direction);

            // Скорость зума зависит от расстояния до цели
            var zoomSpeed = Math.Max(0.01f, direction.Length() * 0.05f);
            var offset = forward * (float)delta * zoomSpeed;

            // Обновляем позицию камеры
            _cameraPosition -= offset;
        }

        private void PickObject(Point point)
        {
            // TODO: Реализовать выбор объекта по клику
            // Для этого потребуется рейкастинг в сцене
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