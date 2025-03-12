using System.Numerics;

namespace AtomEngine
{
    public interface IInputSystem
    {
        bool IsKeyDown(Key key);
        bool IsKeyUp(Key key);
        bool IsKeyPressed(Key key);
        bool IsKeyReleased(Key key);

        bool IsMouseButtonDown(MouseButton button);
        bool IsMouseButtonUp(MouseButton button);
        bool IsMouseButtonPressed(MouseButton button);
        bool IsMouseButtonReleased(MouseButton button);

        Vector2 GetMousePosition();
        Vector2 GetMouseDelta();
        float GetMouseWheelDelta();

        bool IsGamepadConnected(int gamepadIndex);
        bool IsGamepadButtonDown(int gamepadIndex, GamepadButton button);
        bool IsGamepadButtonUp(int gamepadIndex, GamepadButton button);
        bool IsGamepadButtonPressed(int gamepadIndex, GamepadButton button);
        bool IsGamepadButtonReleased(int gamepadIndex, GamepadButton button);
        Vector2 GetGamepadLeftStick(int gamepadIndex);
        Vector2 GetGamepadRightStick(int gamepadIndex);
        float GetGamepadLeftTrigger(int gamepadIndex);
        float GetGamepadRightTrigger(int gamepadIndex);

        bool IsTouchDown(int touchIndex);
        bool IsTouchUp(int touchIndex);
        bool IsTouchPressed(int touchIndex);
        bool IsTouchReleased(int touchIndex);
        Vector2 GetTouchPosition(int touchIndex);

        void Update();

        event EventHandler<KeyEventArgs> KeyDown;
        event EventHandler<KeyEventArgs> KeyUp;
        event EventHandler<MouseButtonEventArgs> MouseButtonDown;
        event EventHandler<MouseButtonEventArgs> MouseButtonUp;
        event EventHandler<MouseMoveEventArgs> MouseMove;
        event EventHandler<MouseWheelEventArgs> MouseWheel;
        event EventHandler<GamepadButtonEventArgs> GamepadButtonDown;
        event EventHandler<GamepadButtonEventArgs> GamepadButtonUp;
        event EventHandler<GamepadConnectEventArgs> GamepadConnect;
        event EventHandler<GamepadDisconnectEventArgs> GamepadDisconnect;
        event EventHandler<TouchEventArgs> TouchDown;
        event EventHandler<TouchEventArgs> TouchUp;
        event EventHandler<TouchMoveEventArgs> TouchMove;
    }
}
