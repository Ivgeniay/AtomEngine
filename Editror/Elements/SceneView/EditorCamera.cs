using Silk.NET.Input.Extensions;
using System.Numerics;
using Avalonia.Input;
using Avalonia;
using System;
using Avalonia.Controls;

namespace Editor
{
    public class EditorCamera
    {
        public Vector3 Position { get; private set; }
        public Vector3 Target { get; private set; }
        public Vector3 Up { get; private set; }
        public float MoveSpeed { get; set; }
        public float RotationSpeedY { get; set; }
        public float RotationSpeedX { get; set; }

        public Vector3 Forward => Vector3.Normalize(Target - Position);
        public Vector3 Right => Vector3.Normalize(Vector3.Cross(Forward, Up));

        private Point _lastMousePosition;
        private bool _isLeftMouseDown;
        private bool _isRightMouseDown;
        private bool _isMiddleMouseDown;

        private Grid _root;

        public EditorCamera(
            Vector3 position, 
            Vector3 target, 
            Vector3 up,
            Grid root, 
            float moveSpeed = 0.1f, 
            float rotationSpeedX = 0.001f,
            float rotationSpeedY = 0.01f
            )
        {
            this._root = root;
            Position = position;
            Target = target;
            Up = up;
            MoveSpeed = moveSpeed;
            RotationSpeedX = rotationSpeedX;
            RotationSpeedY = rotationSpeedY;
        }

        internal float GetAspectRatio() => (float)_root.Bounds.Width / (float)_root.Bounds.Height;
        internal Matrix4x4 GetProjection(bool isPerspective)
        {
            if (isPerspective)
            {
                return Matrix4x4.CreatePerspectiveFieldOfView(
                    MathF.PI / 4.0f,
                    GetAspectRatio(),
                    0.1f,
                    1000.0f);
            }
            else
            {
                float size = Vector3.Distance(Position, Target) * 0.1f;
                return Matrix4x4.CreateOrthographic(
                    size * GetAspectRatio(),
                    size,
                    0.1f,
                    1000.0f);
            }
        }
        internal void HandlePointerMoved(Point currentPosition)
        {
            if (_isRightMouseDown)
            {
                RotateCamera(currentPosition);
            }
            else if (_isMiddleMouseDown)
            {
                PanCamera(currentPosition);
            }

            _lastMousePosition = currentPosition;
        }
        internal void HandlePointerPressed(Point position, PointerUpdateKind updateKind)
        {
            _lastMousePosition = position;

            switch (updateKind)
            {
                case PointerUpdateKind.LeftButtonPressed:
                    _isLeftMouseDown = true;
                    break;
                case PointerUpdateKind.RightButtonPressed:
                    _isRightMouseDown = true;
                    break;
                case PointerUpdateKind.MiddleButtonPressed:
                    _isMiddleMouseDown = true;
                    break;
            }
        }
        internal void HandlePointerReleased(PointerUpdateKind updateKind)
        {
            switch (updateKind)
            {
                case PointerUpdateKind.LeftButtonReleased:
                    _isLeftMouseDown = false;
                    break;
                case PointerUpdateKind.RightButtonReleased:
                    _isRightMouseDown = false;
                    break;
                case PointerUpdateKind.MiddleButtonReleased:
                    _isMiddleMouseDown = false;
                    break;
            }
        }
        internal void HandlePointerWheelChanged(Avalonia.Vector delta) => ZoomCamera(delta.Y);
        internal void MoveForward(float amount)
        {
            Vector3 direction = Vector3.Normalize(Target - Position);
            Vector3 movement = direction * amount * MoveSpeed;
            Position += movement;
            Target += movement;
        }
        internal void MoveRight(float amount)
        {
            Vector3 right = Vector3.Normalize(Vector3.Cross(Forward, Up));
            Vector3 movement = right * amount * MoveSpeed;
            Position += movement;
            Target += movement;
        }
        public void MoveUp(float amount)
        {
            Vector3 movement = Up * amount * MoveSpeed;
            Position += movement;
            Target += movement;
        }

        public void HandleKeyboardInput(KeyEventArgs e)
        {
            if (!_isRightMouseDown) return;

            if (e.Key == Key.W)
                MoveForward(1);
            if (e.Key == Key.S)
                MoveForward(-1);

            if (e.Key == Key.A)
                MoveRight(-1);
            if (e.Key == Key.D)
                MoveRight(1);

            if (e.Key == Key.Q)
                MoveUp(1);
            if (e.Key == Key.E)
                MoveUp(-1);
        }

        private void RotateCamera(Point currentPosition)
        {
            var deltaX = _lastMousePosition.X - currentPosition.X;
            var deltaY = _lastMousePosition.Y - currentPosition.Y;

            var direction = Target - Position;
            var right = Vector3.Cross(direction, Up);

            var rotationMatrixX = Matrix4x4.CreateFromAxisAngle(right, (float)deltaY * RotationSpeedX);
            direction = Vector3.Transform(direction, rotationMatrixX);

            var rotationMatrixY = Matrix4x4.CreateFromAxisAngle(Up, (float)deltaX * RotationSpeedY);
            direction = Vector3.Transform(direction, rotationMatrixY);


            Target = Position + direction;
        }
        private void PanCamera(Point currentPosition)
        {
            var deltaX = (float)(currentPosition.X - _lastMousePosition.X) * MoveSpeed;
            var deltaY = (float)(currentPosition.Y - _lastMousePosition.Y) * MoveSpeed;

            var direction = Target - Position;
            var right = Vector3.Normalize(Vector3.Cross(direction, Up));
            var up = Vector3.Normalize(Vector3.Cross(right, direction));

            var movement = right * -deltaX + up * deltaY;
            Position += movement;
            Target += movement;
        }

        private void ZoomCamera(double delta)
        {
            var direction = Vector3.Normalize(Target - Position);
            var distance = Vector3.Distance(Position, Target);
            var newDistance = Math.Max(0.1f, distance - (float)delta * MoveSpeed);

            Position = Target - direction * newDistance;
        }

        public Matrix4x4 GetViewMatrix() => Matrix4x4.CreateLookAt(Position, Target, Up);
    }
}