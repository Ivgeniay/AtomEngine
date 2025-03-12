using System.Numerics;

namespace AtomEngine
{
    public static class Input
    {
        private static IInputSystem _instance;
        private static bool _isInitialized;

        public static event EventHandler<KeyEventArgs> KeyDown
        {
            add
            {
                EnsureInitialized();
                _instance.KeyDown += value;
            }
            remove
            {
                if (_isInitialized)
                    _instance.KeyDown -= value;
            }
        }

        public static event EventHandler<KeyEventArgs> KeyUp
        {
            add
            {
                EnsureInitialized();
                _instance.KeyUp += value;
            }
            remove
            {
                if (_isInitialized)
                    _instance.KeyUp -= value;
            }
        }

        public static event EventHandler<MouseButtonEventArgs> MouseButtonDown
        {
            add
            {
                EnsureInitialized();
                _instance.MouseButtonDown += value;
            }
            remove
            {
                if (_isInitialized)
                    _instance.MouseButtonDown -= value;
            }
        }

        public static event EventHandler<MouseButtonEventArgs> MouseButtonUp
        {
            add
            {
                EnsureInitialized();
                _instance.MouseButtonUp += value;
            }
            remove
            {
                if (_isInitialized)
                    _instance.MouseButtonUp -= value;
            }
        }

        public static event EventHandler<MouseMoveEventArgs> MouseMove
        {
            add
            {
                EnsureInitialized();
                _instance.MouseMove += value;
            }
            remove
            {
                if (_isInitialized)
                    _instance.MouseMove -= value;
            }
        }

        public static event EventHandler<MouseWheelEventArgs> MouseWheel
        {
            add
            {
                EnsureInitialized();
                _instance.MouseWheel += value;
            }
            remove
            {
                if (_isInitialized)
                    _instance.MouseWheel -= value;
            }
        }

        public static event EventHandler<GamepadButtonEventArgs> GamepadButtonDown
        {
            add
            {
                EnsureInitialized();
                _instance.GamepadButtonDown += value;
            }
            remove
            {
                if (_isInitialized)
                    _instance.GamepadButtonDown -= value;
            }
        }

        public static event EventHandler<GamepadButtonEventArgs> GamepadButtonUp
        {
            add
            {
                EnsureInitialized();
                _instance.GamepadButtonUp += value;
            }
            remove
            {
                if (_isInitialized)
                    _instance.GamepadButtonUp -= value;
            }
        }

        public static event EventHandler<GamepadConnectEventArgs> GamepadConnect
        {
            add
            {
                EnsureInitialized();
                _instance.GamepadConnect += value;
            }
            remove
            {
                if (_isInitialized)
                    _instance.GamepadConnect -= value;
            }
        }

        public static event EventHandler<GamepadDisconnectEventArgs> GamepadDisconnect
        {
            add
            {
                EnsureInitialized();
                _instance.GamepadDisconnect += value;
            }
            remove
            {
                if (_isInitialized)
                    _instance.GamepadDisconnect -= value;
            }
        }

        public static event EventHandler<TouchEventArgs> TouchDown
        {
            add
            {
                EnsureInitialized();
                _instance.TouchDown += value;
            }
            remove
            {
                if (_isInitialized)
                    _instance.TouchDown -= value;
            }
        }

        public static event EventHandler<TouchEventArgs> TouchUp
        {
            add
            {
                EnsureInitialized();
                _instance.TouchUp += value;
            }
            remove
            {
                if (_isInitialized)
                    _instance.TouchUp -= value;
            }
        }

        public static event EventHandler<TouchMoveEventArgs> TouchMove
        {
            add
            {
                EnsureInitialized();
                _instance.TouchMove += value;
            }
            remove
            {
                if (_isInitialized)
                    _instance.TouchMove -= value;
            }
        }

        public static void Initialize(IInputSystem inputSystem)
        {
            _instance = inputSystem ?? throw new ArgumentNullException(nameof(inputSystem));
            _isInitialized = true;
        }

        public static void Update()
        {
            EnsureInitialized();
            _instance.Update();
        }

        public static bool IsKeyDown(Key key)
        {
            EnsureInitialized();
            return _instance.IsKeyDown(key);
        }

        public static bool IsKeyUp(Key key)
        {
            EnsureInitialized();
            return _instance.IsKeyUp(key);
        }

        public static bool IsKeyPressed(Key key)
        {
            EnsureInitialized();
            return _instance.IsKeyPressed(key);
        }

        public static bool IsKeyReleased(Key key)
        {
            EnsureInitialized();
            return _instance.IsKeyReleased(key);
        }

        public static bool IsMouseButtonDown(MouseButton button)
        {
            EnsureInitialized();
            return _instance.IsMouseButtonDown(button);
        }

        public static bool IsMouseButtonUp(MouseButton button)
        {
            EnsureInitialized();
            return _instance.IsMouseButtonUp(button);
        }

        public static bool IsMouseButtonPressed(MouseButton button)
        {
            EnsureInitialized();
            return _instance.IsMouseButtonPressed(button);
        }

        public static bool IsMouseButtonReleased(MouseButton button)
        {
            EnsureInitialized();
            return _instance.IsMouseButtonReleased(button);
        }

        public static Vector2 GetMousePosition()
        {
            EnsureInitialized();
            return _instance.GetMousePosition();
        }

        public static Vector2 GetMouseDelta()
        {
            EnsureInitialized();
            return _instance.GetMouseDelta();
        }

        public static float GetMouseWheelDelta()
        {
            EnsureInitialized();
            return _instance.GetMouseWheelDelta();
        }

        public static bool IsGamepadConnected(int gamepadIndex)
        {
            EnsureInitialized();
            return _instance.IsGamepadConnected(gamepadIndex);
        }

        public static bool IsGamepadButtonDown(int gamepadIndex, GamepadButton button)
        {
            EnsureInitialized();
            return _instance.IsGamepadButtonDown(gamepadIndex, button);
        }

        public static bool IsGamepadButtonUp(int gamepadIndex, GamepadButton button)
        {
            EnsureInitialized();
            return _instance.IsGamepadButtonUp(gamepadIndex, button);
        }

        public static bool IsGamepadButtonPressed(int gamepadIndex, GamepadButton button)
        {
            EnsureInitialized();
            return _instance.IsGamepadButtonPressed(gamepadIndex, button);
        }

        public static bool IsGamepadButtonReleased(int gamepadIndex, GamepadButton button)
        {
            EnsureInitialized();
            return _instance.IsGamepadButtonReleased(gamepadIndex, button);
        }

        public static Vector2 GetGamepadLeftStick(int gamepadIndex)
        {
            EnsureInitialized();
            return _instance.GetGamepadLeftStick(gamepadIndex);
        }

        public static Vector2 GetGamepadRightStick(int gamepadIndex)
        {
            EnsureInitialized();
            return _instance.GetGamepadRightStick(gamepadIndex);
        }

        public static float GetGamepadLeftTrigger(int gamepadIndex)
        {
            EnsureInitialized();
            return _instance.GetGamepadLeftTrigger(gamepadIndex);
        }

        public static float GetGamepadRightTrigger(int gamepadIndex)
        {
            EnsureInitialized();
            return _instance.GetGamepadRightTrigger(gamepadIndex);
        }

        public static bool IsTouchDown(int touchIndex)
        {
            EnsureInitialized();
            return _instance.IsTouchDown(touchIndex);
        }

        public static bool IsTouchUp(int touchIndex)
        {
            EnsureInitialized();
            return _instance.IsTouchUp(touchIndex);
        }

        public static bool IsTouchPressed(int touchIndex)
        {
            EnsureInitialized();
            return _instance.IsTouchPressed(touchIndex);
        }

        public static bool IsTouchReleased(int touchIndex)
        {
            EnsureInitialized();
            return _instance.IsTouchReleased(touchIndex);
        }

        public static Vector2 GetTouchPosition(int touchIndex)
        {
            EnsureInitialized();
            return _instance.GetTouchPosition(touchIndex);
        }

        private static void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Input system is not initialized. Call Input.Initialize() first.");
            }
        }
    }
}
