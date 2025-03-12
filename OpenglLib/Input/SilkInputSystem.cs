using Silk.NET.Input;
using Silk.NET.Windowing;
using System.Numerics;

namespace AtomEngine
{
    public class SilkInputSystem : IInputSystem, IDisposable
    {
        private readonly IWindow _window;
        private IInputContext _inputContext;

        private readonly HashSet<Key> _currentKeys = new HashSet<Key>();
        private readonly HashSet<Key> _previousKeys = new HashSet<Key>();

        private readonly HashSet<MouseButton> _currentMouseButtons = new HashSet<MouseButton>();
        private readonly HashSet<MouseButton> _previousMouseButtons = new HashSet<MouseButton>();

        private readonly Dictionary<int, Dictionary<GamepadButton, bool>> _currentGamepadButtons = new Dictionary<int, Dictionary<GamepadButton, bool>>();
        private readonly Dictionary<int, Dictionary<GamepadButton, bool>> _previousGamepadButtons = new Dictionary<int, Dictionary<GamepadButton, bool>>();

        private readonly Dictionary<int, bool> _connectedGamepads = new Dictionary<int, bool>();

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

        public SilkInputSystem(IWindow window)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));

            _window.Load += OnWindowLoad;
        }

        private void OnWindowLoad()
        {
            _inputContext = _window.CreateInput();

            foreach (var keyboard in _inputContext.Keyboards)
            {
                keyboard.KeyDown += OnKeyDown;
                keyboard.KeyUp += OnKeyUp;
            }

            foreach (var mouse in _inputContext.Mice)
            {
                mouse.MouseDown += OnMouseDown;
                mouse.MouseUp += OnMouseUp;
                mouse.MouseMove += OnMouseMove;
                mouse.Scroll += OnMouseScroll;
            }

            _inputContext.ConnectionChanged += OnConnectionChanged;

            foreach (var gamepad in _inputContext.Gamepads)
            {
                RegisterGamepad(gamepad);
            }
        }

        private void OnKeyDown(IKeyboard keyboard, Silk.NET.Input.Key key, int scancode)
        {
            var engineKey = ConvertSilkKey(key);
            if (engineKey != Key.Unknown)
            {
                _currentKeys.Add(engineKey);

                var modifiers = GetCurrentModifiers(keyboard);
                KeyDown?.Invoke(this, new KeyEventArgs(engineKey, false, modifiers));
            }
        }

        private void OnKeyUp(IKeyboard keyboard, Silk.NET.Input.Key key, int scancode)
        {
            var engineKey = ConvertSilkKey(key);
            if (engineKey != Key.Unknown)
            {
                _currentKeys.Remove(engineKey);

                var modifiers = GetCurrentModifiers(keyboard);
                KeyUp?.Invoke(this, new KeyEventArgs(engineKey, false, modifiers));
            }
        }

        private void OnMouseDown(IMouse mouse, Silk.NET.Input.MouseButton button)
        {
            var engineButton = ConvertSilkMouseButton(button);
            if (engineButton != MouseButton.Unknown)
            {
                _currentMouseButtons.Add(engineButton);

                var modifiers = GetCurrentModifiers();
                MouseButtonDown?.Invoke(this, new MouseButtonEventArgs(engineButton, _mousePosition, 1, modifiers));
            }
        }

        private void OnMouseUp(IMouse mouse, Silk.NET.Input.MouseButton button)
        {
            var engineButton = ConvertSilkMouseButton(button);
            if (engineButton != MouseButton.Unknown)
            {
                _currentMouseButtons.Remove(engineButton);

                var modifiers = GetCurrentModifiers();
                MouseButtonUp?.Invoke(this, new MouseButtonEventArgs(engineButton, _mousePosition, 1, modifiers));
            }
        }

        private void OnMouseMove(IMouse mouse, Vector2 position)
        {
            _previousMousePosition = _mousePosition;
            _mousePosition = position;
            _mouseDelta = _mousePosition - _previousMousePosition;

            var modifiers = GetCurrentModifiers();
            MouseMove?.Invoke(this, new MouseMoveEventArgs(_mousePosition, _mouseDelta, modifiers));
        }

        private void OnMouseScroll(IMouse mouse, ScrollWheel scrollWheel)
        {
            _mouseWheelDelta = scrollWheel.Y;

            var modifiers = GetCurrentModifiers();
            MouseWheel?.Invoke(this, new MouseWheelEventArgs(_mousePosition, _mouseWheelDelta, MouseWheelDirection.Vertical, modifiers));
        }

        private void OnConnectionChanged(IInputDevice device, bool connected)
        {
            switch (device)
            {
                case IGamepad gamepad:
                    if (connected)
                    {
                        RegisterGamepad(gamepad);
                        GamepadConnect?.Invoke(this, new GamepadConnectEventArgs(gamepad.Index, gamepad.Name));
                    }
                    else
                    {
                        UnregisterGamepad(gamepad);
                        GamepadDisconnect?.Invoke(this, new GamepadDisconnectEventArgs(gamepad.Index));
                    }
                    break;
            }
        }

        private void RegisterGamepad(IGamepad gamepad)
        {
            _connectedGamepads[gamepad.Index] = true;
            _currentGamepadButtons[gamepad.Index] = new Dictionary<GamepadButton, bool>();
            _previousGamepadButtons[gamepad.Index] = new Dictionary<GamepadButton, bool>();

            gamepad.ButtonDown += OnGamepadButtonDown;
            gamepad.ButtonUp += OnGamepadButtonUp;
        }

        private void UnregisterGamepad(IGamepad gamepad)
        {
            _connectedGamepads[gamepad.Index] = false;

            gamepad.ButtonDown -= OnGamepadButtonDown;
            gamepad.ButtonUp -= OnGamepadButtonUp;
        }

        private void OnGamepadButtonDown(IGamepad gamepad, Silk.NET.Input.Button button)
        {
            var engineButton = ConvertSilkGamepadButton(button);
            if (engineButton != GamepadButton.Unknown)
            {
                if (!_currentGamepadButtons.ContainsKey(gamepad.Index))
                {
                    _currentGamepadButtons[gamepad.Index] = new Dictionary<GamepadButton, bool>();
                }

                _currentGamepadButtons[gamepad.Index][engineButton] = true;

                GamepadButtonDown?.Invoke(this, new GamepadButtonEventArgs(gamepad.Index, engineButton, 1.0f));
            }
        }

        private void OnGamepadButtonUp(IGamepad gamepad, Silk.NET.Input.Button button)
        {
            var engineButton = ConvertSilkGamepadButton(button);
            if (engineButton != GamepadButton.Unknown)
            {
                if (_currentGamepadButtons.ContainsKey(gamepad.Index) && _currentGamepadButtons[gamepad.Index].ContainsKey(engineButton))
                {
                    _currentGamepadButtons[gamepad.Index][engineButton] = false;
                }

                GamepadButtonUp?.Invoke(this, new GamepadButtonEventArgs(gamepad.Index, engineButton, 0.0f));
            }
        }

        public void Update()
        {
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

            foreach (var gamepad in _inputContext.Gamepads)
            {
                if (_connectedGamepads.ContainsKey(gamepad.Index) && _connectedGamepads[gamepad.Index])
                {
                    _gamepadLeftSticks[gamepad.Index] = new Vector2(gamepad.Thumbsticks[0].X, gamepad.Thumbsticks[0].Y);
                    _gamepadRightSticks[gamepad.Index] = new Vector2(gamepad.Thumbsticks[1].X, gamepad.Thumbsticks[1].Y);
                    _gamepadLeftTriggers[gamepad.Index] = gamepad.Triggers[0].Position;
                    _gamepadRightTriggers[gamepad.Index] = gamepad.Triggers[1].Position;
                }
            }
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

        private ModifierKeys GetCurrentModifiers(IKeyboard keyboard = null)
        {
            ModifierKeys modifiers = ModifierKeys.None;

            if (keyboard == null && _inputContext.Keyboards.Count > 0)
            {
                keyboard = _inputContext.Keyboards[0];
            }

            if (keyboard != null)
            {
                if (keyboard.IsKeyPressed(Silk.NET.Input.Key.ShiftLeft) || keyboard.IsKeyPressed(Silk.NET.Input.Key.ShiftRight))
                    modifiers |= ModifierKeys.Shift;

                if (keyboard.IsKeyPressed(Silk.NET.Input.Key.ControlLeft) || keyboard.IsKeyPressed(Silk.NET.Input.Key.ControlRight))
                    modifiers |= ModifierKeys.Control;

                if (keyboard.IsKeyPressed(Silk.NET.Input.Key.AltLeft) || keyboard.IsKeyPressed(Silk.NET.Input.Key.AltRight))
                    modifiers |= ModifierKeys.Alt;

                if (keyboard.IsKeyPressed(Silk.NET.Input.Key.SuperLeft) || keyboard.IsKeyPressed(Silk.NET.Input.Key.SuperRight))
                    modifiers |= ModifierKeys.Super;

                if (keyboard.IsKeyPressed(Silk.NET.Input.Key.CapsLock))
                    modifiers |= ModifierKeys.CapsLock;

                if (keyboard.IsKeyPressed(Silk.NET.Input.Key.NumLock))
                    modifiers |= ModifierKeys.NumLock;
            }

            return modifiers;
        }

        private Key ConvertSilkKey(Silk.NET.Input.Key silkKey)
        {
            switch (silkKey)
            {
                // Alphabet
                case Silk.NET.Input.Key.A: return Key.A;
                case Silk.NET.Input.Key.B: return Key.B;
                case Silk.NET.Input.Key.C: return Key.C;
                case Silk.NET.Input.Key.D: return Key.D;
                case Silk.NET.Input.Key.E: return Key.E;
                case Silk.NET.Input.Key.F: return Key.F;
                case Silk.NET.Input.Key.G: return Key.G;
                case Silk.NET.Input.Key.H: return Key.H;
                case Silk.NET.Input.Key.I: return Key.I;
                case Silk.NET.Input.Key.J: return Key.J;
                case Silk.NET.Input.Key.K: return Key.K;
                case Silk.NET.Input.Key.L: return Key.L;
                case Silk.NET.Input.Key.M: return Key.M;
                case Silk.NET.Input.Key.N: return Key.N;
                case Silk.NET.Input.Key.O: return Key.O;
                case Silk.NET.Input.Key.P: return Key.P;
                case Silk.NET.Input.Key.Q: return Key.Q;
                case Silk.NET.Input.Key.R: return Key.R;
                case Silk.NET.Input.Key.S: return Key.S;
                case Silk.NET.Input.Key.T: return Key.T;
                case Silk.NET.Input.Key.U: return Key.U;
                case Silk.NET.Input.Key.V: return Key.V;
                case Silk.NET.Input.Key.W: return Key.W;
                case Silk.NET.Input.Key.X: return Key.X;
                case Silk.NET.Input.Key.Y: return Key.Y;
                case Silk.NET.Input.Key.Z: return Key.Z;

                // Numbers
                case Silk.NET.Input.Key.Number0: return Key.D0;
                case Silk.NET.Input.Key.Number1: return Key.D1;
                case Silk.NET.Input.Key.Number2: return Key.D2;
                case Silk.NET.Input.Key.Number3: return Key.D3;
                case Silk.NET.Input.Key.Number4: return Key.D4;
                case Silk.NET.Input.Key.Number5: return Key.D5;
                case Silk.NET.Input.Key.Number6: return Key.D6;
                case Silk.NET.Input.Key.Number7: return Key.D7;
                case Silk.NET.Input.Key.Number8: return Key.D8;
                case Silk.NET.Input.Key.Number9: return Key.D9;

                // Function keys
                case Silk.NET.Input.Key.F1: return Key.F1;
                case Silk.NET.Input.Key.F2: return Key.F2;
                case Silk.NET.Input.Key.F3: return Key.F3;
                case Silk.NET.Input.Key.F4: return Key.F4;
                case Silk.NET.Input.Key.F5: return Key.F5;
                case Silk.NET.Input.Key.F6: return Key.F6;
                case Silk.NET.Input.Key.F7: return Key.F7;
                case Silk.NET.Input.Key.F8: return Key.F8;
                case Silk.NET.Input.Key.F9: return Key.F9;
                case Silk.NET.Input.Key.F10: return Key.F10;
                case Silk.NET.Input.Key.F11: return Key.F11;
                case Silk.NET.Input.Key.F12: return Key.F12;

                // Special keys
                case Silk.NET.Input.Key.Escape: return Key.Escape;
                case Silk.NET.Input.Key.Tab: return Key.Tab;
                case Silk.NET.Input.Key.CapsLock: return Key.CapsLock;
                case Silk.NET.Input.Key.ShiftLeft: return Key.LeftShift;
                case Silk.NET.Input.Key.ShiftRight: return Key.RightShift;
                case Silk.NET.Input.Key.ControlLeft: return Key.LeftControl;
                case Silk.NET.Input.Key.ControlRight: return Key.RightControl;
                case Silk.NET.Input.Key.AltLeft: return Key.LeftAlt;
                case Silk.NET.Input.Key.AltRight: return Key.RightAlt;
                case Silk.NET.Input.Key.Space: return Key.Space;
                case Silk.NET.Input.Key.Enter: return Key.Enter;
                case Silk.NET.Input.Key.Backspace: return Key.Backspace;
                case Silk.NET.Input.Key.Insert: return Key.Insert;
                case Silk.NET.Input.Key.Delete: return Key.Delete;
                case Silk.NET.Input.Key.Home: return Key.Home;
                case Silk.NET.Input.Key.End: return Key.End;
                case Silk.NET.Input.Key.PageUp: return Key.PageUp;
                case Silk.NET.Input.Key.PageDown: return Key.PageDown;

                // Arrow keys
                case Silk.NET.Input.Key.Left: return Key.Left;
                case Silk.NET.Input.Key.Right: return Key.Right;
                case Silk.NET.Input.Key.Up: return Key.Up;
                case Silk.NET.Input.Key.Down: return Key.Down;

                // Numpad
                case Silk.NET.Input.Key.NumLock: return Key.NumLock;
                case Silk.NET.Input.Key.Keypad0: return Key.NumPad0;
                case Silk.NET.Input.Key.Keypad1: return Key.NumPad1;
                case Silk.NET.Input.Key.Keypad2: return Key.NumPad2;
                case Silk.NET.Input.Key.Keypad3: return Key.NumPad3;
                case Silk.NET.Input.Key.Keypad4: return Key.NumPad4;
                case Silk.NET.Input.Key.Keypad5: return Key.NumPad5;
                case Silk.NET.Input.Key.Keypad6: return Key.NumPad6;
                case Silk.NET.Input.Key.Keypad7: return Key.NumPad7;
                case Silk.NET.Input.Key.Keypad8: return Key.NumPad8;
                case Silk.NET.Input.Key.Keypad9: return Key.NumPad9;
                case Silk.NET.Input.Key.KeypadDivide: return Key.NumPadDivide;
                case Silk.NET.Input.Key.KeypadMultiply: return Key.NumPadMultiply;
                case Silk.NET.Input.Key.KeypadSubtract: return Key.NumPadSubtract;
                case Silk.NET.Input.Key.KeypadAdd: return Key.NumPadAdd;
                case Silk.NET.Input.Key.KeypadDecimal: return Key.NumPadDecimal;
                case Silk.NET.Input.Key.KeypadEnter: return Key.NumPadEnter;

                // Special characters
                case Silk.NET.Input.Key.GraveAccent: return Key.Grave;
                case Silk.NET.Input.Key.Minus: return Key.Minus;
                case Silk.NET.Input.Key.Equal: return Key.Equal;
                case Silk.NET.Input.Key.LeftBracket: return Key.LeftBracket;
                case Silk.NET.Input.Key.RightBracket: return Key.RightBracket;
                case Silk.NET.Input.Key.BackSlash: return Key.Backslash;
                case Silk.NET.Input.Key.Semicolon: return Key.Semicolon;
                case Silk.NET.Input.Key.Apostrophe: return Key.Apostrophe;
                case Silk.NET.Input.Key.Comma: return Key.Comma;
                case Silk.NET.Input.Key.Period: return Key.Period;
                case Silk.NET.Input.Key.Slash: return Key.Slash;

                // Modifiers
                case Silk.NET.Input.Key.SuperLeft: return Key.LeftSuper;
                case Silk.NET.Input.Key.SuperRight: return Key.RightSuper;
                case Silk.NET.Input.Key.Menu: return Key.Menu;

                // Media keys
                case Silk.NET.Input.Key.PrintScreen: return Key.PrintScreen;
                case Silk.NET.Input.Key.ScrollLock: return Key.ScrollLock;
                case Silk.NET.Input.Key.Pause: return Key.Pause;

                default: return Key.Unknown;
            }
        }

        private MouseButton ConvertSilkMouseButton(Silk.NET.Input.MouseButton silkButton)
        {
            switch (silkButton)
            {
                case Silk.NET.Input.MouseButton.Left: return MouseButton.Left;
                case Silk.NET.Input.MouseButton.Right: return MouseButton.Right;
                case Silk.NET.Input.MouseButton.Middle: return MouseButton.Middle;
                case Silk.NET.Input.MouseButton.Button4: return MouseButton.Button4;
                case Silk.NET.Input.MouseButton.Button5: return MouseButton.Button5;
                case Silk.NET.Input.MouseButton.Button6: return MouseButton.Button6;
                case Silk.NET.Input.MouseButton.Button7: return MouseButton.Button7;
                case Silk.NET.Input.MouseButton.Button8: return MouseButton.Button8;
                default: return MouseButton.Unknown;
            }
        }

        private GamepadButton ConvertSilkGamepadButton(Silk.NET.Input.Button silkButton)
        {
            switch (silkButton.Name)
            {
                case Silk.NET.Input.ButtonName.A: return GamepadButton.A;
                case Silk.NET.Input.ButtonName.B: return GamepadButton.B;
                case Silk.NET.Input.ButtonName.X: return GamepadButton.X;
                case Silk.NET.Input.ButtonName.Y: return GamepadButton.Y;
                case Silk.NET.Input.ButtonName.LeftBumper: return GamepadButton.LeftBumper;
                case Silk.NET.Input.ButtonName.RightBumper: return GamepadButton.RightBumper;
                case Silk.NET.Input.ButtonName.Back: return GamepadButton.Back;
                case Silk.NET.Input.ButtonName.Start: return GamepadButton.Start;
                case Silk.NET.Input.ButtonName.Home: return GamepadButton.Home;
                case Silk.NET.Input.ButtonName.LeftStick: return GamepadButton.LeftStick;
                case Silk.NET.Input.ButtonName.RightStick: return GamepadButton.RightStick;
                case Silk.NET.Input.ButtonName.DPadUp: return GamepadButton.DPadUp;
                case Silk.NET.Input.ButtonName.DPadRight: return GamepadButton.DPadRight;
                case Silk.NET.Input.ButtonName.DPadDown: return GamepadButton.DPadDown;
                case Silk.NET.Input.ButtonName.DPadLeft: return GamepadButton.DPadLeft;
                default: return GamepadButton.Unknown;
            }
        }

        public void Dispose()
        {
            if (_inputContext != null)
            {
                foreach (var keyboard in _inputContext.Keyboards)
                {
                    keyboard.KeyDown -= OnKeyDown;
                    keyboard.KeyUp -= OnKeyUp;
                }

                foreach (var mouse in _inputContext.Mice)
                {
                    mouse.MouseDown -= OnMouseDown;
                    mouse.MouseUp -= OnMouseUp;
                    mouse.MouseMove -= OnMouseMove;
                    mouse.Scroll -= OnMouseScroll;
                }

                foreach (var gamepad in _inputContext.Gamepads)
                {
                    gamepad.ButtonDown -= OnGamepadButtonDown;
                    gamepad.ButtonUp -= OnGamepadButtonUp;
                }

                _inputContext.ConnectionChanged -= OnConnectionChanged;
                _inputContext.Dispose();
            }
        }
    }
}
