using System.Numerics;
using AtomEngine;
using Avalonia;
using System;
using EngineLib;

namespace Editor
{
    [HideInspectorSearch]
    [ExecutionOnScene]
    public class EditorCameraControllerSystem : ISystem
    {
        public IWorld World { get; set; }

        private QueryEntity _queryCameraController;

        private const float VelocityDamping = 0.9f;
        private const float RotationDamping = 0.8f;

        public EditorCameraControllerSystem(IWorld world)
        {
            World = world;
            _queryCameraController = world.CreateEntityQuery()
                .With<TransformComponent>()
                .With<CameraComponent>()
                .With<EditorCameraComponent>()
                .With<EditorCameraControllerComponent>();
        }

        public void Initialize() { }

        public void Update(double deltaTime)
        {
            float dt = (float)deltaTime;
            var cameras = _queryCameraController.Build();
            if (cameras.Length == 0) return;

            foreach (var entity in cameras)
            {
                ref var transform = ref World.GetComponent<TransformComponent>(entity);
                ref var camera = ref World.GetComponent<CameraComponent>(entity);
                ref var editorCamera = ref World.GetComponent<EditorCameraComponent>(entity);
                ref var controller = ref World.GetComponent<EditorCameraControllerComponent>(entity);

                if (!controller.IsActive) continue;

                ProcessKeyboardInput(ref transform, ref camera, ref editorCamera, dt);

                ProcessMouseInput(ref transform, ref camera, ref editorCamera, dt);

                editorCamera.CurrentVelocity *= VelocityDamping;

                transform.Position += editorCamera.CurrentVelocity * dt;
                editorCamera.Target += editorCamera.CurrentVelocity * dt;

                editorCamera.CurrentRotation *= RotationDamping;

                if (editorCamera.CurrentRotation != Vector2.Zero)
                {
                    ApplyRotation(ref transform, ref camera, ref editorCamera,
                        editorCamera.CurrentRotation.X, editorCamera.CurrentRotation.Y);
                }

                camera.ViewMatrix = Matrix4x4.CreateLookAt(
                    transform.Position,
                    editorCamera.Target,
                    camera.CameraUp);
            }
        }

        private void ProcessKeyboardInput(ref TransformComponent transform, ref CameraComponent camera,
            ref EditorCameraComponent editorCamera, float deltaTime)
        {
            Vector3 inputDirection = Vector3.Zero;

            if (Input.IsKeyDown(AtomEngine.Key.W))
                inputDirection += Vector3.Normalize(editorCamera.Target - transform.Position);
            if (Input.IsKeyDown(AtomEngine.Key.S))
                inputDirection -= Vector3.Normalize(editorCamera.Target - transform.Position);
            if (Input.IsKeyDown(AtomEngine.Key.A))
                inputDirection -= Vector3.Normalize(Vector3.Cross(editorCamera.Target - transform.Position, camera.CameraUp));
            if (Input.IsKeyDown(AtomEngine.Key.D))
                inputDirection += Vector3.Normalize(Vector3.Cross(editorCamera.Target - transform.Position, camera.CameraUp));
            if (Input.IsKeyDown(AtomEngine.Key.Q))
                inputDirection += camera.CameraUp;
            if (Input.IsKeyDown(AtomEngine.Key.E))
                inputDirection -= camera.CameraUp;

            if (inputDirection != Vector3.Zero)
            {
                inputDirection = Vector3.Normalize(inputDirection);
                editorCamera.CurrentVelocity += inputDirection * editorCamera.MoveSpeed * deltaTime * 100.0f;
            }
        }

        private void ProcessMouseInput(ref TransformComponent transform, ref CameraComponent camera,
            ref EditorCameraComponent editorCamera, float deltaTime)
        {
            var currentMousePosition = new Point(Input.GetMousePosition().X, Input.GetMousePosition().Y);

            float deltaX = (float)(currentMousePosition.X - editorCamera.LastMousePosition.X);
            float deltaY = (float)(currentMousePosition.Y - editorCamera.LastMousePosition.Y);

            if (Input.IsMouseButtonDown(AtomEngine.MouseButton.Right))
            {
                editorCamera.CurrentRotation = new Vector2(
                    deltaX * editorCamera.RotationSpeedY,
                    deltaY * editorCamera.RotationSpeedX);

                ApplyRotation(ref transform, ref camera, ref editorCamera, deltaX, deltaY);
            }
            else if (Input.IsMouseButtonDown(AtomEngine.MouseButton.Middle))
            {
                PanCamera(ref transform, ref camera, ref editorCamera, deltaX, deltaY);
            }

            float wheelDelta = Input.GetMouseWheelDelta();
            if (wheelDelta != 0 && Input.IsMouseButtonDown(AtomEngine.MouseButton.Right))
            {
                ZoomCamera(ref transform, ref editorCamera, wheelDelta);
            }

            editorCamera.LastMousePosition = currentMousePosition;
        }

        private void ApplyRotation(ref TransformComponent transform, ref CameraComponent camera,
            ref EditorCameraComponent editorCamera, float deltaX, float deltaY)
        {
            deltaX = -deltaX;
            deltaY = -deltaY;

            var direction = editorCamera.Target - transform.Position;
            var right = Vector3.Cross(direction, camera.CameraUp);

            var rotationMatrixX = Matrix4x4.CreateFromAxisAngle(right, deltaY * editorCamera.RotationSpeedX);
            direction = Vector3.Transform(direction, rotationMatrixX);

            var rotationMatrixY = Matrix4x4.CreateFromAxisAngle(camera.CameraUp, deltaX * editorCamera.RotationSpeedY);
            direction = Vector3.Transform(direction, rotationMatrixY);

            editorCamera.Target = transform.Position + direction;
        }

        private void PanCamera(ref TransformComponent transform, ref CameraComponent camera,
            ref EditorCameraComponent editorCamera, float deltaX, float deltaY)
        {
            deltaX = -deltaX;
            deltaY = -deltaY;

            var direction = Vector3.Normalize(editorCamera.Target - transform.Position);
            var right = Vector3.Normalize(Vector3.Cross(direction, camera.CameraUp));
            var up = Vector3.Normalize(Vector3.Cross(right, direction));

            var movement = right * deltaX * editorCamera.MoveSpeed + up * deltaY * editorCamera.MoveSpeed;

            editorCamera.CurrentVelocity += movement * 0.1f;
        }

        private void ZoomCamera(ref TransformComponent transform, ref EditorCameraComponent editorCamera, float delta)
        {
            var direction = Vector3.Normalize(editorCamera.Target - transform.Position);
            var distance = Vector3.Distance(transform.Position, editorCamera.Target);
            var newDistance = Math.Max(0.1f, distance - delta * editorCamera.MoveSpeed);

            transform.Position = editorCamera.Target - direction * newDistance;
        }

        public void Resize(Vector2 size)
        {
            var cameras = _queryCameraController.Build();
            if (cameras.Length > 0)
            {
                ref var camera = ref World.GetComponent<CameraComponent>(cameras[0]);
                camera.AspectRatio = size.X / size.Y;
            }
        }
    }
}