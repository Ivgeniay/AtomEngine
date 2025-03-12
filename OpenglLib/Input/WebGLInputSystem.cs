using System.Numerics;

namespace AtomEngine
{
    public class WebGLInputSystem : IInputSystem
    {
        private readonly HashSet<Key> _currentKeys = new HashSet<Key>();
        private readonly HashSet<Key> _previousKeys = new HashSet<Key>();

        private readonly HashSet<MouseButton> _currentMouseButtons = new HashSet<MouseButton>();
        private readonly HashSet<MouseButton> _previousMouseButtons = new HashSet<MouseButton>();

        private readonly Dictionary<int, bool> _connectedGamepads = new Dictionary<int, bool>();
        private readonly Dictionary<int, Dictionary<GamepadButton, bool>> _currentGamepadButtons = new Dictionary<int, Dictionary<GamepadButton, bool>>();
        private readonly Dictionary<int, Dictionary<GamepadButton, bool>> _previousGamepadButtons = new Dictionary<int, Dictionary<GamepadButton, bool>>();

        private readonly Dictionary<int, Vector2> _touchPositions = new Dictionary<int, Vector2>();
        private readonly HashSet<int> _currentTouches = new HashSet<int>();
        private readonly HashSet<int> _previousTouches = new HashSet<int>();

        private Vector2 _mousePosition;
        private Vector2 _previousMousePosition;
        private Vector2 _mouseDelta;
        private float _mouseWheelDelta;

        private readonly Dictionary<int, Vector2> _gamepadLeftSticks = new Dictionary<int, Vector2>();
        private readonly Dictionary<int, Vector2> _gamepadRightSticks = new Dictionary<int, Vector2>();
        private readonly Dictionary<int, float> _gamepadLeftTriggers = new Dictionary<int, float>();
        private readonly Dictionary<int, float> _gamepadRightTriggers = new Dictionary<int, float>();

        public event EventHandler<KeyEventArgs> KeyDown;
        public event EventHandler<KeyEventArgs> KeyUp;
        public event EventHandler<MouseButtonEventArgs> MouseButtonDown;
        public event EventHandler<MouseButtonEventArgs> MouseButtonUp;
        public event EventHandler<MouseMoveEventArgs> MouseMove;
        public event EventHandler<MouseWheelEventArgs> MouseWheel;
        public event EventHandler<GamepadButtonEventArgs> GamepadButtonDown;
        public event EventHandler<GamepadButtonEventArgs> GamepadButtonUp;
        public event EventHandler<GamepadConnectEventArgs> GamepadConnect;
        public event EventHandler<GamepadDisconnectEventArgs> GamepadDisconnect;
        public event EventHandler<TouchEventArgs> TouchDown;
        public event EventHandler<TouchEventArgs> TouchUp;
        public event EventHandler<TouchMoveEventArgs> TouchMove;

        private readonly object _domElement;

        public WebGLInputSystem(object domElement)
        {
            _domElement = domElement ?? throw new ArgumentNullException(nameof(domElement));

            // Здесь будет регистрация JavaScript-обработчиков событий DOM
            // Например через JSInterop в Blazor или аналогичный механизм
            InitializeDomEvents();
        }

        private void InitializeDomEvents()
        {
            // Здесь будет регистрация обработчиков событий DOM для:
            // - keydown, keyup
            // - mousedown, mouseup, mousemove, wheel
            // - touchstart, touchend, touchmove
            // - gamepadconnected, gamepaddisconnected

            // Также здесь будет настройка gamepad API polling если необходимо
        }

        // Методы, которые будут вызываться из JavaScript
        public void ProcessKeyDown(int keyCode, bool isRepeat, int modifiers)
        {
            var key = ConvertWebKeyCode(keyCode);
            if (key != Key.Unknown)
            {
                _currentKeys.Add(key);

                var modifierKeys = ConvertWebModifiers(modifiers);
                KeyDown?.Invoke(this, new KeyEventArgs(key, isRepeat, modifierKeys));
            }
        }

        public void ProcessKeyUp(int keyCode, int modifiers)
        {
            var key = ConvertWebKeyCode(keyCode);
            if (key != Key.Unknown)
            {
                _currentKeys.Remove(key);

                var modifierKeys = ConvertWebModifiers(modifiers);
                KeyUp?.Invoke(this, new KeyEventArgs(key, false, modifierKeys));
            }
        }

        public void ProcessMouseDown(int button, float x, float y, int clickCount, int modifiers)
        {
            var mouseButton = ConvertWebMouseButton(button);
            if (mouseButton != MouseButton.Unknown)
            {
                _currentMouseButtons.Add(mouseButton);

                _mousePosition = new Vector2(x, y);

                var modifierKeys = ConvertWebModifiers(modifiers);
                MouseButtonDown?.Invoke(this, new MouseButtonEventArgs(mouseButton, _mousePosition, clickCount, modifierKeys));
            }
        }

        public void ProcessMouseUp(int button, float x, float y, int clickCount, int modifiers)
        {
            var mouseButton = ConvertWebMouseButton(button);
            if (mouseButton != MouseButton.Unknown)
            {
                _currentMouseButtons.Remove(mouseButton);

                _mousePosition = new Vector2(x, y);

                var modifierKeys = ConvertWebModifiers(modifiers);
                MouseButtonUp?.Invoke(this, new MouseButtonEventArgs(mouseButton, _mousePosition, clickCount, modifierKeys));
            }
        }

        public void ProcessMouseMove(float x, float y, int modifiers)
        {
            _previousMousePosition = _mousePosition;
            _mousePosition = new Vector2(x, y);
            _mouseDelta = _mousePosition - _previousMousePosition;

            var modifierKeys = ConvertWebModifiers(modifiers);
            MouseMove?.Invoke(this, new MouseMoveEventArgs(_mousePosition, _mouseDelta, modifierKeys));
        }

        public void ProcessMouseWheel(float deltaX, float deltaY, int modifiers)
        {
            _mouseWheelDelta = deltaY;

            var modifierKeys = ConvertWebModifiers(modifiers);
            MouseWheel?.Invoke(this, new MouseWheelEventArgs(_mousePosition, _mouseWheelDelta, MouseWheelDirection.Vertical, modifierKeys));
        }

        public void ProcessTouchStart(int touchId, float x, float y, float pressure)
        {
            _touchPositions[touchId] = new Vector2(x, y);
            _currentTouches.Add(touchId);

            TouchDown?.Invoke(this, new TouchEventArgs(touchId, _touchPositions[touchId], pressure));
        }

        public void ProcessTouchEnd(int touchId, float x, float y, float pressure)
        {
            _touchPositions[touchId] = new Vector2(x, y);
            _currentTouches.Remove(touchId);

            TouchUp?.Invoke(this, new TouchEventArgs(touchId, _touchPositions[touchId], pressure));
        }

        public void ProcessTouchMove(int touchId, float x, float y, float deltaX, float deltaY, float pressure)
        {
            var position = new Vector2(x, y);
            var delta = new Vector2(deltaX, deltaY);

            _touchPositions[touchId] = position;

            TouchMove?.Invoke(this, new TouchMoveEventArgs(touchId, position, delta, pressure));
        }

        public void ProcessGamepadConnect(int gamepadIndex, string name)
        {
            _connectedGamepads[gamepadIndex] = true;
            _currentGamepadButtons[gamepadIndex] = new Dictionary<GamepadButton, bool>();
            _previousGamepadButtons[gamepadIndex] = new Dictionary<GamepadButton, bool>();

            GamepadConnect?.Invoke(this, new GamepadConnectEventArgs(gamepadIndex, name));
        }

        public void ProcessGamepadDisconnect(int gamepadIndex)
        {
            _connectedGamepads[gamepadIndex] = false;

            GamepadDisconnect?.Invoke(this, new GamepadDisconnectEventArgs(gamepadIndex));
        }

        public void ProcessGamepadButtonDown(int gamepadIndex, int buttonIndex)
        {
            var button = ConvertWebGamepadButton(buttonIndex);
            if (button != GamepadButton.Unknown)
            {
                if (!_currentGamepadButtons.ContainsKey(gamepadIndex))
                {
                    _currentGamepadButtons[gamepadIndex] = new Dictionary<GamepadButton, bool>();
                }

                _currentGamepadButtons[gamepadIndex][button] = true;

                GamepadButtonDown?.Invoke(this, new GamepadButtonEventArgs(gamepadIndex, button, 1.0f));
            }
        }

        public void ProcessGamepadButtonUp(int gamepadIndex, int buttonIndex)
        {
            var button = ConvertWebGamepadButton(buttonIndex);
            if (button != GamepadButton.Unknown)
            {
                if (_currentGamepadButtons.ContainsKey(gamepadIndex) && _currentGamepadButtons[gamepadIndex].ContainsKey(button))
                {
                    _currentGamepadButtons[gamepadIndex][button] = false;
                }

                GamepadButtonUp?.Invoke(this, new GamepadButtonEventArgs(gamepadIndex, button, 0.0f));
            }
        }

        public void ProcessGamepadAxisChange(int gamepadIndex, int axisIndex, float value)
        {
            // Обработка изменений осей геймпада:
            // 0, 1 - левый стик (X, Y)
            // 2, 3 - правый стик (X, Y)
            // 4 - левый триггер
            // 5 - правый триггер

            switch (axisIndex)
            {
                case 0: // Left stick X
                    if (!_gamepadLeftSticks.ContainsKey(gamepadIndex))
                    {
                        _gamepadLeftSticks[gamepadIndex] = Vector2.Zero;
                    }
                    _gamepadLeftSticks[gamepadIndex] = new Vector2(value, _gamepadLeftSticks[gamepadIndex].Y);
                    break;

                case 1: // Left stick Y
                    if (!_gamepadLeftSticks.ContainsKey(gamepadIndex))
                    {
                        _gamepadLeftSticks[gamepadIndex] = Vector2.Zero;
                    }
                    _gamepadLeftSticks[gamepadIndex] = new Vector2(_gamepadLeftSticks[gamepadIndex].X, value);
                    break;

                case 2: // Right stick X
                    if (!_gamepadRightSticks.ContainsKey(gamepadIndex))
                    {
                        _gamepadRightSticks[gamepadIndex] = Vector2.Zero;
                    }
                    _gamepadRightSticks[gamepadIndex] = new Vector2(value, _gamepadRightSticks[gamepadIndex].Y);
                    break;

                case 3: // Right stick Y
                    if (!_gamepadRightSticks.ContainsKey(gamepadIndex))
                    {
                        _gamepadRightSticks[gamepadIndex] = Vector2.Zero;
                    }
                    _gamepadRightSticks[gamepadIndex] = new Vector2(_gamepadRightSticks[gamepadIndex].X, value);
                    break;

                case 4: // Left trigger
                    _gamepadLeftTriggers[gamepadIndex] = value;
                    break;

                case 5: // Right trigger
                    _gamepadRightTriggers[gamepadIndex] = value;
                    break;
            }
        }

        public void Update()
        {
            // Копирование текущих состояний в предыдущие
            _previousKeys.Clear();
            foreach (var key in _currentKeys)
            {
                _previousKeys.Add(key);
            }

            _previousMouseButtons.Clear();
            foreach (var button in _currentMouseButtons)
            {
                _previousMouseButtons.Add(button);
            }

            _previousTouches.Clear();
            foreach (var touchId in _currentTouches)
            {
                _previousTouches.Add(touchId);
            }

            foreach (var gamepadIndex in _currentGamepadButtons.Keys)
            {
                if (!_previousGamepadButtons.ContainsKey(gamepadIndex))
                {
                    _previousGamepadButtons[gamepadIndex] = new Dictionary<GamepadButton, bool>();
                }

                _previousGamepadButtons[gamepadIndex].Clear();
                foreach (var buttonEntry in _currentGamepadButtons[gamepadIndex])
                {
                    _previousGamepadButtons[gamepadIndex][buttonEntry.Key] = buttonEntry.Value;
                }
            }

            // Запрос состояния геймпадов через Gamepad API в JavaScript
            PollGamepads();
        }

        private void PollGamepads()
        {
            // Здесь будет код для опроса состояния геймпадов через JavaScript
            // navigator.getGamepads() в веб-контексте
        }

        public bool IsKeyDown(Key key)
        {
            return _currentKeys.Contains(key);
        }

        public bool IsKeyUp(Key key)
        {
            return !_currentKeys.Contains(key);
        }

        public bool IsKeyPressed(Key key)
        {
            return _currentKeys.Contains(key) && !_previousKeys.Contains(key);
        }

        public bool IsKeyReleased(Key key)
        {
            return !_currentKeys.Contains(key) && _previousKeys.Contains(key);
        }

        public bool IsMouseButtonDown(MouseButton button)
        {
            return _currentMouseButtons.Contains(button);
        }

        public bool IsMouseButtonUp(MouseButton button)
        {
            return !_currentMouseButtons.Contains(button);
        }

        public bool IsMouseButtonPressed(MouseButton button)
        {
            return _currentMouseButtons.Contains(button) && !_previousMouseButtons.Contains(button);
        }

        public bool IsMouseButtonReleased(MouseButton button)
        {
            return !_currentMouseButtons.Contains(button) && _previousMouseButtons.Contains(button);
        }

        public Vector2 GetMousePosition()
        {
            return _mousePosition;
        }

        public Vector2 GetMouseDelta()
        {
            return _mouseDelta;
        }

        public float GetMouseWheelDelta()
        {
            return _mouseWheelDelta;
        }

        public bool IsGamepadConnected(int gamepadIndex)
        {
            return _connectedGamepads.ContainsKey(gamepadIndex) && _connectedGamepads[gamepadIndex];
        }

        public bool IsGamepadButtonDown(int gamepadIndex, GamepadButton button)
        {
            return _currentGamepadButtons.ContainsKey(gamepadIndex) &&
                   _currentGamepadButtons[gamepadIndex].ContainsKey(button) &&
                   _currentGamepadButtons[gamepadIndex][button];
        }

        public bool IsGamepadButtonUp(int gamepadIndex, GamepadButton button)
        {
            return !IsGamepadButtonDown(gamepadIndex, button);
        }

        public bool IsGamepadButtonPressed(int gamepadIndex, GamepadButton button)
        {
            return IsGamepadButtonDown(gamepadIndex, button) &&
                   (!_previousGamepadButtons.ContainsKey(gamepadIndex) ||
                    !_previousGamepadButtons[gamepadIndex].ContainsKey(button) ||
                    !_previousGamepadButtons[gamepadIndex][button]);
        }

        public bool IsGamepadButtonReleased(int gamepadIndex, GamepadButton button)
        {
            return !IsGamepadButtonDown(gamepadIndex, button) &&
                   _previousGamepadButtons.ContainsKey(gamepadIndex) &&
                   _previousGamepadButtons[gamepadIndex].ContainsKey(button) &&
                   _previousGamepadButtons[gamepadIndex][button];
        }

        public Vector2 GetGamepadLeftStick(int gamepadIndex)
        {
            return _gamepadLeftSticks.ContainsKey(gamepadIndex) ? _gamepadLeftSticks[gamepadIndex] : Vector2.Zero;
        }

        public Vector2 GetGamepadRightStick(int gamepadIndex)
        {
            return _gamepadRightSticks.ContainsKey(gamepadIndex) ? _gamepadRightSticks[gamepadIndex] : Vector2.Zero;
        }

        public float GetGamepadLeftTrigger(int gamepadIndex)
        {
            return _gamepadLeftTriggers.ContainsKey(gamepadIndex) ? _gamepadLeftTriggers[gamepadIndex] : 0.0f;
        }

        public float GetGamepadRightTrigger(int gamepadIndex)
        {
            return _gamepadRightTriggers.ContainsKey(gamepadIndex) ? _gamepadRightTriggers[gamepadIndex] : 0.0f;
        }

        public bool IsTouchDown(int touchIndex)
        {
            return _currentTouches.Contains(touchIndex);
        }

        public bool IsTouchUp(int touchIndex)
        {
            return !_currentTouches.Contains(touchIndex);
        }

        public bool IsTouchPressed(int touchIndex)
        {
            return _currentTouches.Contains(touchIndex) && !_previousTouches.Contains(touchIndex);
        }

        public bool IsTouchReleased(int touchIndex)
        {
            return !_currentTouches.Contains(touchIndex) && _previousTouches.Contains(touchIndex);
        }

        public Vector2 GetTouchPosition(int touchIndex)
        {
            return _touchPositions.ContainsKey(touchIndex) ? _touchPositions[touchIndex] : Vector2.Zero;
        }

        private Key ConvertWebKeyCode(int keyCode)
        {
            // Преобразование JavaScript key codes в наш enum Key
            // Здесь будет реализация конвертации DOM KeyboardEvent.keyCode или KeyboardEvent.code
            // в соответствующие значения enum Key

            // Пример для некоторых кодов (это лишь пример, полная реализация потребует больше кодов)
            switch (keyCode)
            {
                // DOM KeyboardEvent.keyCode или KeyboardEvent.code значения
                case 65: return Key.A; // KeyA
                case 66: return Key.B; // KeyB
                case 67: return Key.C; // KeyC
                                       // ... остальные буквы

                case 48: return Key.D0; // Digit0
                case 49: return Key.D1; // Digit1
                                        // ... остальные цифры

                case 37: return Key.Left; // ArrowLeft
                case 38: return Key.Up; // ArrowUp
                case 39: return Key.Right; // ArrowRight
                case 40: return Key.Down; // ArrowDown

                // ... и остальные клавиши

                default: return Key.Unknown;
            }
        }

        private MouseButton ConvertWebMouseButton(int button)
        {
            switch (button)
            {
                case 0: return MouseButton.Left;
                case 1: return MouseButton.Middle;
                case 2: return MouseButton.Right;
                case 3: return MouseButton.Button4;
                case 4: return MouseButton.Button5;
                default: return MouseButton.Unknown;
            }
        }

        private GamepadButton ConvertWebGamepadButton(int buttonIndex)
        {
            switch (buttonIndex)
            {
                case 0: return GamepadButton.A;
                case 1: return GamepadButton.B;
                case 2: return GamepadButton.X;
                case 3: return GamepadButton.Y;
                case 4: return GamepadButton.LeftBumper;
                case 5: return GamepadButton.RightBumper;
                //case 6: return GamepadButton.LeftTrigger; // Иногда обрабатывается как кнопка, иногда как ось
                //case 7: return GamepadButton.RightTrigger; // Иногда обрабатывается как кнопка, иногда как ось
                case 8: return GamepadButton.Back;
                case 9: return GamepadButton.Start;
                case 10: return GamepadButton.LeftStick;
                case 11: return GamepadButton.RightStick;
                case 12: return GamepadButton.DPadUp;
                case 13: return GamepadButton.DPadDown;
                case 14: return GamepadButton.DPadLeft;
                case 15: return GamepadButton.DPadRight;
                case 16: return GamepadButton.Guide;
                default: return GamepadButton.Unknown;
            }
        }

        private ModifierKeys ConvertWebModifiers(int modifiers)
        {
            ModifierKeys result = ModifierKeys.None;

            if ((modifiers & 1) != 0) result |= ModifierKeys.Shift;
            if ((modifiers & 2) != 0) result |= ModifierKeys.Control;
            if ((modifiers & 4) != 0) result |= ModifierKeys.Alt;
            if ((modifiers & 8) != 0) result |= ModifierKeys.Super;

            return result;
        }
    }
}
