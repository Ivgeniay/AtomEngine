using AtomEngine;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Editor
{
    public class EditorInputSystem : IInputSystem
    {
        private readonly Window _window;
        private readonly Dictionary<AtomEngine.Key, bool> _currentKeyState = new();
        private readonly Dictionary<AtomEngine.Key, bool> _previousKeyState = new();
        private readonly Dictionary<AtomEngine.MouseButton, bool> _currentMouseButtonState = new();
        private readonly Dictionary<AtomEngine.MouseButton, bool> _previousMouseButtonState = new();
        private readonly Dictionary<int, Dictionary<GamepadButton, bool>> _currentGamepadButtonState = new();
        private readonly Dictionary<int, Dictionary<GamepadButton, bool>> _previousGamepadButtonState = new();
        private readonly Dictionary<int, bool> _gamepadConnectionState = new();
        private readonly Dictionary<int, Vector2> _gamepadLeftStickState = new();
        private readonly Dictionary<int, Vector2> _gamepadRightStickState = new();
        private readonly Dictionary<int, float> _gamepadLeftTriggerState = new();
        private readonly Dictionary<int, float> _gamepadRightTriggerState = new();
        private readonly Dictionary<int, bool> _currentTouchState = new();
        private readonly Dictionary<int, bool> _previousTouchState = new();
        private readonly Dictionary<int, Vector2> _touchPositions = new();

        private Vector2 _currentMousePosition;
        private Vector2 _previousMousePosition;
        private Vector2 _mouseDelta;
        private float _mouseWheelDelta;

        public event EventHandler<AtomEngine.KeyEventArgs> KeyDown;
        public event EventHandler<AtomEngine.KeyEventArgs> KeyUp;
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

        public EditorInputSystem(Window window)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            InitializeInputHandlers();
        }

        private void InitializeInputHandlers()
        {
            // Клавиатура
            _window.KeyDown += Window_KeyDown;
            _window.KeyUp += Window_KeyUp;

            // Мышь
            _window.PointerPressed += Window_PointerPressed;
            _window.PointerReleased += Window_PointerReleased;
            _window.PointerMoved += Window_PointerMoved;
            _window.PointerWheelChanged += Window_PointerWheelChanged;

            // Сенсорные события (если поддерживаются)
            var inputElement = _window as InputElement;
            if (inputElement != null)
            {
                inputElement.Tapped += InputElement_Tapped;
            }

            // В Avalonia нет прямого доступа к событиям геймпада
            // Для поддержки геймпада потребуется дополнительная реализация
            // через нативные API или внешнюю библиотеку
        }

        private ModifierKeys GetCurrentModifiers()
        {
            ModifierKeys modifiers = ModifierKeys.None;

            if (IsKeyDown(AtomEngine.Key.Shift) || IsKeyDown(AtomEngine.Key.LeftShift) || IsKeyDown(AtomEngine.Key.RightShift))
                modifiers |= ModifierKeys.Shift;

            if (IsKeyDown(AtomEngine.Key.Control) || IsKeyDown(AtomEngine.Key.LeftControl) || IsKeyDown(AtomEngine.Key.RightControl))
                modifiers |= ModifierKeys.Control;

            if (IsKeyDown(AtomEngine.Key.Alt) || IsKeyDown(AtomEngine.Key.LeftAlt) || IsKeyDown(AtomEngine.Key.RightAlt))
                modifiers |= ModifierKeys.Alt;

            if (IsKeyDown(AtomEngine.Key.LeftSuper) || IsKeyDown(AtomEngine.Key.RightSuper))
                modifiers |= ModifierKeys.Super;

            return modifiers;
        }

        #region Обработчики событий Avalonia

        private void Window_KeyDown(object sender, Avalonia.Input.KeyEventArgs e)
        {
            var key = ConvertAvaloniaKeyToEngineKey(e.Key);
            _currentKeyState[key] = true;

            var modifiers = ConvertAvaloniaModifiersToEngineModifiers(e.KeyModifiers);
            bool isRepeat = _previousKeyState.TryGetValue(key, out bool prevState) && prevState;

            KeyDown?.Invoke(this, new AtomEngine.KeyEventArgs(key, isRepeat, modifiers));
        }

        private void Window_KeyUp(object sender, Avalonia.Input.KeyEventArgs e)
        {
            var key = ConvertAvaloniaKeyToEngineKey(e.Key);
            _currentKeyState[key] = false;

            var modifiers = ConvertAvaloniaModifiersToEngineModifiers(e.KeyModifiers);
            KeyUp?.Invoke(this, new AtomEngine.KeyEventArgs(key, false, modifiers));
        }

        private void Window_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            var properties = e.GetCurrentPoint(_window).Properties;
            var position = e.GetPosition(_window);

            var modifiers = GetCurrentModifiers();

            if (properties.IsLeftButtonPressed)
            {
                HandleMouseButtonPress(AtomEngine.MouseButton.Left, position, e.ClickCount, modifiers);
            }
            if (properties.IsRightButtonPressed)
            {
                HandleMouseButtonPress(AtomEngine.MouseButton.Right, position, e.ClickCount, modifiers);
            }
            if (properties.IsMiddleButtonPressed)
            {
                HandleMouseButtonPress(AtomEngine.MouseButton.Middle, position, e.ClickCount, modifiers);
            }
            if (properties.IsXButton1Pressed)
            {
                HandleMouseButtonPress(AtomEngine.MouseButton.Button4, position, e.ClickCount, modifiers);
            }
            if (properties.IsXButton2Pressed)
            {
                HandleMouseButtonPress(AtomEngine.MouseButton.Button5, position, e.ClickCount, modifiers);
            }
        }



        private void HandleMouseButtonPress(AtomEngine.MouseButton button, Point position, int clickCount, ModifierKeys modifiers)
        {
            _currentMouseButtonState[button] = true;
            Vector2 pos = new Vector2((float)position.X, (float)position.Y);
            MouseButtonDown?.Invoke(this, new MouseButtonEventArgs(button, pos, clickCount, modifiers));
        }

        private void Window_PointerReleased(object sender, PointerReleasedEventArgs e)
        {
            var properties = e.GetCurrentPoint(_window).Properties;
            var position = e.GetPosition(_window);

            var modifiers = GetCurrentModifiers();

            AtomEngine.MouseButton button;

            if (e.InitialPressMouseButton == Avalonia.Input.MouseButton.Left)
            {
                button = AtomEngine.MouseButton.Left;
            }
            else if (e.InitialPressMouseButton == Avalonia.Input.MouseButton.Right)
            {
                button = AtomEngine.MouseButton.Right;
            }
            else if (e.InitialPressMouseButton == Avalonia.Input.MouseButton.Middle)
            {
                button = AtomEngine.MouseButton.Middle;
            }
            else
            {
                button = AtomEngine.MouseButton.Unknown;
            }

            if (button != AtomEngine.MouseButton.Unknown)
            {
                _currentMouseButtonState[button] = false;
                Vector2 pos = new Vector2((float)position.X, (float)position.Y);
                MouseButtonUp?.Invoke(this, new MouseButtonEventArgs(button, pos, 0, modifiers));
            }
        }

        private void Window_PointerMoved(object sender, PointerEventArgs e)
        {
            var position = e.GetPosition(_window);
            var newPosition = new Vector2((float)position.X, (float)position.Y);
            _mouseDelta = newPosition - _currentMousePosition;
            _currentMousePosition = newPosition;

            var modifiers = GetCurrentModifiers();
            MouseMove?.Invoke(this, new MouseMoveEventArgs(_currentMousePosition, _mouseDelta, modifiers));
        }

        private void Window_PointerWheelChanged(object sender, PointerWheelEventArgs e)
        {
            var position = e.GetPosition(_window);
            Vector2 pos = new Vector2((float)position.X, (float)position.Y);

            var modifiers = GetCurrentModifiers();

            if (e.Delta.Y != 0)
            {
                _mouseWheelDelta = (float)e.Delta.Y;
                MouseWheel?.Invoke(this, new MouseWheelEventArgs(pos, _mouseWheelDelta, MouseWheelDirection.Vertical, modifiers));
            }

            if (e.Delta.X != 0)
            {
                _mouseWheelDelta = (float)e.Delta.X;
                MouseWheel?.Invoke(this, new MouseWheelEventArgs(pos, _mouseWheelDelta, MouseWheelDirection.Horizontal, modifiers));
            }

            if (e.Delta.X == 0 && e.Delta.Y == 0)
            {
                _mouseWheelDelta = 0;
            }
        }

        private void InputElement_Tapped(object sender, TappedEventArgs e)
        {
        }

        #endregion

        #region Конвертация типов Avalonia в типы Engine

        private AtomEngine.Key ConvertAvaloniaKeyToEngineKey(Avalonia.Input.Key key)
        {
            // Реализуйте конвертацию клавиш Avalonia в клавиши движка
            switch (key)
            {
                case Avalonia.Input.Key.A: return AtomEngine.Key.A;
                case Avalonia.Input.Key.B: return AtomEngine.Key.B;
                case Avalonia.Input.Key.C: return AtomEngine.Key.C;
                case Avalonia.Input.Key.D: return AtomEngine.Key.D;
                case Avalonia.Input.Key.E: return AtomEngine.Key.E;
                case Avalonia.Input.Key.F: return AtomEngine.Key.F;
                case Avalonia.Input.Key.G: return AtomEngine.Key.G;
                case Avalonia.Input.Key.H: return AtomEngine.Key.H;
                case Avalonia.Input.Key.I: return AtomEngine.Key.I;
                case Avalonia.Input.Key.J: return AtomEngine.Key.J;
                case Avalonia.Input.Key.K: return AtomEngine.Key.K;
                case Avalonia.Input.Key.L: return AtomEngine.Key.L;
                case Avalonia.Input.Key.M: return AtomEngine.Key.M;
                case Avalonia.Input.Key.N: return AtomEngine.Key.N;
                case Avalonia.Input.Key.O: return AtomEngine.Key.O;
                case Avalonia.Input.Key.P: return AtomEngine.Key.P;
                case Avalonia.Input.Key.Q: return AtomEngine.Key.Q;
                case Avalonia.Input.Key.R: return AtomEngine.Key.R;
                case Avalonia.Input.Key.S: return AtomEngine.Key.S;
                case Avalonia.Input.Key.T: return AtomEngine.Key.T;
                case Avalonia.Input.Key.U: return AtomEngine.Key.U;
                case Avalonia.Input.Key.V: return AtomEngine.Key.V;
                case Avalonia.Input.Key.W: return AtomEngine.Key.W;
                case Avalonia.Input.Key.X: return AtomEngine.Key.X;
                case Avalonia.Input.Key.Y: return AtomEngine.Key.Y;
                case Avalonia.Input.Key.Z: return AtomEngine.Key.Z;

                case Avalonia.Input.Key.D0: return AtomEngine.Key.D0;
                case Avalonia.Input.Key.D1: return AtomEngine.Key.D1;
                case Avalonia.Input.Key.D2: return AtomEngine.Key.D2;
                case Avalonia.Input.Key.D3: return AtomEngine.Key.D3;
                case Avalonia.Input.Key.D4: return AtomEngine.Key.D4;
                case Avalonia.Input.Key.D5: return AtomEngine.Key.D5;
                case Avalonia.Input.Key.D6: return AtomEngine.Key.D6;
                case Avalonia.Input.Key.D7: return AtomEngine.Key.D7;
                case Avalonia.Input.Key.D8: return AtomEngine.Key.D8;
                case Avalonia.Input.Key.D9: return AtomEngine.Key.D9;

                case Avalonia.Input.Key.F1: return AtomEngine.Key.F1;
                case Avalonia.Input.Key.F2: return AtomEngine.Key.F2;
                case Avalonia.Input.Key.F3: return AtomEngine.Key.F3;
                case Avalonia.Input.Key.F4: return AtomEngine.Key.F4;
                case Avalonia.Input.Key.F5: return AtomEngine.Key.F5;
                case Avalonia.Input.Key.F6: return AtomEngine.Key.F6;
                case Avalonia.Input.Key.F7: return AtomEngine.Key.F7;
                case Avalonia.Input.Key.F8: return AtomEngine.Key.F8;
                case Avalonia.Input.Key.F9: return AtomEngine.Key.F9;
                case Avalonia.Input.Key.F10: return AtomEngine.Key.F10;
                case Avalonia.Input.Key.F11: return AtomEngine.Key.F11;
                case Avalonia.Input.Key.F12: return AtomEngine.Key.F12;

                case Avalonia.Input.Key.Escape: return AtomEngine.Key.Escape;
                case Avalonia.Input.Key.Tab: return AtomEngine.Key.Tab;
                case Avalonia.Input.Key.CapsLock: return AtomEngine.Key.CapsLock;
                case Avalonia.Input.Key.LeftShift: return AtomEngine.Key.LeftShift;
                case Avalonia.Input.Key.RightShift: return AtomEngine.Key.RightShift;
                case Avalonia.Input.Key.LeftCtrl: return AtomEngine.Key.LeftControl;
                case Avalonia.Input.Key.RightCtrl: return AtomEngine.Key.RightControl;
                case Avalonia.Input.Key.LeftAlt: return AtomEngine.Key.LeftAlt;
                case Avalonia.Input.Key.RightAlt: return AtomEngine.Key.RightAlt;
                case Avalonia.Input.Key.Space: return AtomEngine.Key.Space;
                case Avalonia.Input.Key.Enter: return AtomEngine.Key.Enter;
                case Avalonia.Input.Key.Back: return AtomEngine.Key.Backspace;
                case Avalonia.Input.Key.Insert: return AtomEngine.Key.Insert;
                case Avalonia.Input.Key.Delete: return AtomEngine.Key.Delete;
                case Avalonia.Input.Key.Home: return AtomEngine.Key.Home;
                case Avalonia.Input.Key.End: return AtomEngine.Key.End;
                case Avalonia.Input.Key.PageUp: return AtomEngine.Key.PageUp;
                case Avalonia.Input.Key.PageDown: return AtomEngine.Key.PageDown;

                case Avalonia.Input.Key.Left: return AtomEngine.Key.Left;
                case Avalonia.Input.Key.Right: return AtomEngine.Key.Right;
                case Avalonia.Input.Key.Up: return AtomEngine.Key.Up;
                case Avalonia.Input.Key.Down: return AtomEngine.Key.Down;

                case Avalonia.Input.Key.NumLock: return AtomEngine.Key.NumLock;
                case Avalonia.Input.Key.NumPad0: return AtomEngine.Key.NumPad0;
                case Avalonia.Input.Key.NumPad1: return AtomEngine.Key.NumPad1;
                case Avalonia.Input.Key.NumPad2: return AtomEngine.Key.NumPad2;
                case Avalonia.Input.Key.NumPad3: return AtomEngine.Key.NumPad3;
                case Avalonia.Input.Key.NumPad4: return AtomEngine.Key.NumPad4;
                case Avalonia.Input.Key.NumPad5: return AtomEngine.Key.NumPad5;
                case Avalonia.Input.Key.NumPad6: return AtomEngine.Key.NumPad6;
                case Avalonia.Input.Key.NumPad7: return AtomEngine.Key.NumPad7;
                case Avalonia.Input.Key.NumPad8: return AtomEngine.Key.NumPad8;
                case Avalonia.Input.Key.NumPad9: return AtomEngine.Key.NumPad9;
                case Avalonia.Input.Key.Divide: return AtomEngine.Key.NumPadDivide;
                case Avalonia.Input.Key.Multiply: return AtomEngine.Key.NumPadMultiply;
                case Avalonia.Input.Key.Subtract: return AtomEngine.Key.NumPadSubtract;
                case Avalonia.Input.Key.Add: return AtomEngine.Key.NumPadAdd;
                case Avalonia.Input.Key.Decimal: return AtomEngine.Key.NumPadDecimal;

                case Avalonia.Input.Key.OemTilde: return AtomEngine.Key.Grave;
                case Avalonia.Input.Key.OemMinus: return AtomEngine.Key.Minus;
                case Avalonia.Input.Key.OemPlus: return AtomEngine.Key.Equal;
                case Avalonia.Input.Key.OemOpenBrackets: return AtomEngine.Key.LeftBracket;
                case Avalonia.Input.Key.OemCloseBrackets: return AtomEngine.Key.RightBracket;
                case Avalonia.Input.Key.OemBackslash: return AtomEngine.Key.Backslash;
                case Avalonia.Input.Key.OemSemicolon: return AtomEngine.Key.Semicolon;
                case Avalonia.Input.Key.OemQuotes: return AtomEngine.Key.Apostrophe;
                case Avalonia.Input.Key.OemComma: return AtomEngine.Key.Comma;
                case Avalonia.Input.Key.OemPeriod: return AtomEngine.Key.Period;
                case Avalonia.Input.Key.OemQuestion: return AtomEngine.Key.Slash;

                case Avalonia.Input.Key.LWin: return AtomEngine.Key.LeftSuper;
                case Avalonia.Input.Key.RWin: return AtomEngine.Key.RightSuper;
                case Avalonia.Input.Key.Apps: return AtomEngine.Key.Menu;

                case Avalonia.Input.Key.PrintScreen: return AtomEngine.Key.PrintScreen;
                case Avalonia.Input.Key.Scroll: return AtomEngine.Key.ScrollLock;
                case Avalonia.Input.Key.Pause: return AtomEngine.Key.Pause;

                default: return AtomEngine.Key.Unknown;
            }
        }

        private AtomEngine.ModifierKeys ConvertAvaloniaModifiersToEngineModifiers(KeyModifiers keyModifiers)
        {
            AtomEngine.ModifierKeys result = AtomEngine.ModifierKeys.None;

            if ((keyModifiers & KeyModifiers.Shift) != 0)
                result |= AtomEngine.ModifierKeys.Shift;

            if ((keyModifiers & KeyModifiers.Control) != 0)
                result |= AtomEngine.ModifierKeys.Control;

            if ((keyModifiers & KeyModifiers.Alt) != 0)
                result |= AtomEngine.ModifierKeys.Alt;

            if ((keyModifiers & KeyModifiers.Meta) != 0)
                result |= AtomEngine.ModifierKeys.Super;

            // Для CapsLock и NumLock требуется отдельная логика определения состояния
            // так как в Avalonia KeyModifiers они не всегда включены

            return result;
        }

        #endregion

        #region Реализация IInputSystem

        public bool IsKeyDown(AtomEngine.Key key)
        {
            return _currentKeyState.TryGetValue(key, out bool value) && value;
        }

        public bool IsKeyUp(AtomEngine.Key key)
        {
            return !IsKeyDown(key);
        }

        public bool IsKeyPressed(AtomEngine.Key key)
        {
            return IsKeyDown(key) && (!_previousKeyState.TryGetValue(key, out bool prevValue) || !prevValue);
        }

        public bool IsKeyReleased(AtomEngine.Key key)
        {
            return !IsKeyDown(key) && _previousKeyState.TryGetValue(key, out bool prevValue) && prevValue;
        }

        public bool IsMouseButtonDown(AtomEngine.MouseButton button)
        {
            return _currentMouseButtonState.TryGetValue(button, out bool value) && value;
        }

        public bool IsMouseButtonUp(AtomEngine.MouseButton button)
        {
            return !IsMouseButtonDown(button);
        }

        public bool IsMouseButtonPressed(AtomEngine.MouseButton button)
        {
            return IsMouseButtonDown(button) && (!_previousMouseButtonState.TryGetValue(button, out bool prevValue) || !prevValue);
        }

        public bool IsMouseButtonReleased(AtomEngine.MouseButton button)
        {
            return !IsMouseButtonDown(button) && _previousMouseButtonState.TryGetValue(button, out bool prevValue) && prevValue;
        }

        public Vector2 GetMousePosition()
        {
            return _currentMousePosition;
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
            return _gamepadConnectionState.TryGetValue(gamepadIndex, out bool value) && value;
        }

        public bool IsGamepadButtonDown(int gamepadIndex, GamepadButton button)
        {
            return _currentGamepadButtonState.TryGetValue(gamepadIndex, out var buttonState) &&
                   buttonState.TryGetValue(button, out bool value) && value;
        }

        public bool IsGamepadButtonUp(int gamepadIndex, GamepadButton button)
        {
            return !IsGamepadButtonDown(gamepadIndex, button);
        }

        public bool IsGamepadButtonPressed(int gamepadIndex, GamepadButton button)
        {
            if (!IsGamepadButtonDown(gamepadIndex, button))
                return false;

            if (!_previousGamepadButtonState.TryGetValue(gamepadIndex, out var prevState))
                return true;

            return !prevState.TryGetValue(button, out bool prevValue) || !prevValue;
        }

        public bool IsGamepadButtonReleased(int gamepadIndex, GamepadButton button)
        {
            if (IsGamepadButtonDown(gamepadIndex, button))
                return false;

            if (!_previousGamepadButtonState.TryGetValue(gamepadIndex, out var prevState))
                return false;

            return prevState.TryGetValue(button, out bool prevValue) && prevValue;
        }

        public Vector2 GetGamepadLeftStick(int gamepadIndex)
        {
            return _gamepadLeftStickState.TryGetValue(gamepadIndex, out var value) ? value : Vector2.Zero;
        }

        public Vector2 GetGamepadRightStick(int gamepadIndex)
        {
            return _gamepadRightStickState.TryGetValue(gamepadIndex, out var value) ? value : Vector2.Zero;
        }

        public float GetGamepadLeftTrigger(int gamepadIndex)
        {
            return _gamepadLeftTriggerState.TryGetValue(gamepadIndex, out var value) ? value : 0f;
        }

        public float GetGamepadRightTrigger(int gamepadIndex)
        {
            return _gamepadRightTriggerState.TryGetValue(gamepadIndex, out var value) ? value : 0f;
        }

        public bool IsTouchDown(int touchIndex)
        {
            return _currentTouchState.TryGetValue(touchIndex, out bool value) && value;
        }

        public bool IsTouchUp(int touchIndex)
        {
            return !IsTouchDown(touchIndex);
        }

        public bool IsTouchPressed(int touchIndex)
        {
            return IsTouchDown(touchIndex) && (!_previousTouchState.TryGetValue(touchIndex, out bool prevValue) || !prevValue);
        }

        public bool IsTouchReleased(int touchIndex)
        {
            return !IsTouchDown(touchIndex) && _previousTouchState.TryGetValue(touchIndex, out bool prevValue) && prevValue;
        }

        public Vector2 GetTouchPosition(int touchIndex)
        {
            return _touchPositions.TryGetValue(touchIndex, out var value) ? value : Vector2.Zero;
        }

        public void Update()
        {
            // Сохраняем предыдущее состояние кнопок и клавиш
            _previousKeyState.Clear();
            foreach (var kvp in _currentKeyState)
            {
                _previousKeyState[kvp.Key] = kvp.Value;
            }

            _previousMouseButtonState.Clear();
            foreach (var kvp in _currentMouseButtonState)
            {
                _previousMouseButtonState[kvp.Key] = kvp.Value;
            }

            _previousTouchState.Clear();
            foreach (var kvp in _currentTouchState)
            {
                _previousTouchState[kvp.Key] = kvp.Value;
            }

            // Для геймпадов
            _previousGamepadButtonState.Clear();
            foreach (var gamepadKvp in _currentGamepadButtonState)
            {
                int gamepadIndex = gamepadKvp.Key;
                if (!_previousGamepadButtonState.ContainsKey(gamepadIndex))
                {
                    _previousGamepadButtonState[gamepadIndex] = new Dictionary<GamepadButton, bool>();
                }

                foreach (var buttonKvp in gamepadKvp.Value)
                {
                    _previousGamepadButtonState[gamepadIndex][buttonKvp.Key] = buttonKvp.Value;
                }
            }

            _previousMousePosition = _currentMousePosition;
            _mouseWheelDelta = 0; 

        }

        #endregion
    }
}