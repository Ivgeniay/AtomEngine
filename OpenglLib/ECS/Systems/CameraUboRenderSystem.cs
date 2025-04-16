using OpenglLib.Buffers;
using System.Numerics;
using AtomEngine;
using EngineLib;

namespace OpenglLib
{
    public class CameraUboRenderSystem : IRenderSystem
    {
        public IWorld World { get; set; }

        private QueryEntity queryCameraEntities;
        private UboService _uboService;
        private CamerasUboData _camerasUboData;
        private bool _isDirty = true;

        public CameraUboRenderSystem(IWorld world)
        {
            World = world;

            queryCameraEntities = this.CreateEntityQuery()
                .With<CameraComponent>()
                .With<TransformComponent>()
                ;

            _camerasUboData = new CamerasUboData();
            _camerasUboData.ActiveCameraIndex = 0;
        }

        public void Initialize()
        {
            _uboService = ServiceHub.Get<UboService>();
        }

        public void Render(double deltaTime, object? context)
        {
            if (!_uboService.HasUboByBindingPoint(3))
                return;

            Entity[] cameraEntities = queryCameraEntities.Build();
            if (cameraEntities.Length == 0)
                return;

            bool dataChanged = false;

            for (int i = 0; i < LightParams.MAX_CAMERAS; i++)
            {
                CameraData emptyCamera = new CameraData();
                emptyCamera.Enabled = 0.0f;

                switch (i)
                {
                    case 0: _camerasUboData.Camera0 = emptyCamera; break;
                    case 1: _camerasUboData.Camera1 = emptyCamera; break;
                    case 2: _camerasUboData.Camera2 = emptyCamera; break;
                    case 3: _camerasUboData.Camera3 = emptyCamera; break;
                }
            }

            int activeCameraIndex = -1;
            int cameraCount = Math.Min(cameraEntities.Length, LightParams.MAX_CAMERAS);

            for (int i = 0; i < cameraCount; i++)
            {
                ref var camera = ref this.GetComponent<CameraComponent>(cameraEntities[i]);
                ref var transform = ref this.GetComponent<TransformComponent>(cameraEntities[i]);

                CameraData cameraData = new CameraData();
                cameraData.Position = transform.Position;
                cameraData.Front = camera.CameraFront;
                cameraData.Up = camera.CameraUp;
                cameraData.Fov = camera.FieldOfView * (MathF.PI / 180f);
                cameraData.AspectRatio = camera.AspectRatio;
                cameraData.NearPlane = camera.NearPlane;
                cameraData.FarPlane = camera.FarPlane;
                cameraData.Enabled = camera.IsActive ? 1.0f : 0.0f;
                cameraData.ViewMatrix = camera.ViewMatrix;
                cameraData.ProjectionMatrix = camera.CreateProjectionMatrix();

                if (camera.IsActive && activeCameraIndex == -1)
                {
                    activeCameraIndex = i;
                }

                switch (i)
                {
                    case 0: _camerasUboData.Camera0 = cameraData; break;
                    case 1: _camerasUboData.Camera1 = cameraData; break;
                    case 2: _camerasUboData.Camera2 = cameraData; break;
                    case 3: _camerasUboData.Camera3 = cameraData; break;
                }

                dataChanged = true;
            }

            if (activeCameraIndex == -1 && cameraCount > 0)
            {
                activeCameraIndex = 0;
            }

            if (_camerasUboData.ActiveCameraIndex != activeCameraIndex && activeCameraIndex != -1)
            {
                _camerasUboData.ActiveCameraIndex = activeCameraIndex;
                dataChanged = true;
            }

            if (dataChanged || _isDirty)
            {
                _uboService.SetUboDataByBindingPoint(3, _camerasUboData);
                _isDirty = false;
            }
        }

        public void Resize(Vector2 size)
        {
            float aspectRatio = size.X / size.Y;

            Entity[] cameraEntities = queryCameraEntities.Build();
            foreach (var entity in cameraEntities)
            {
                ref var camera = ref this.GetComponent<CameraComponent>(entity);
                camera.AspectRatio = aspectRatio;
            }

            _isDirty = true;
        }
    }
}
