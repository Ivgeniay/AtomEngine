using OpenglLib.Buffers;
using System.Numerics;
using AtomEngine;
using EngineLib;
using Silk.NET.Assimp;

namespace OpenglLib
{
    public class CameraUboRenderSystem : IRenderSystem
    {
        const uint UBO_BINDING_POINT = 3;

        const string UBO_NAME = "CamerasUBO";

        private readonly string[] CAMERA_DOMAINS = new string[LightParams.MAX_CAMERAS];

        const string POSITION_SUBDOMAIN = "position";
        const string FRONT_SUBDOMAIN = "front";
        const string UP_SUBDOMAIN = "up";
        const string FOV_SUBDOMAIN = "fov";
        const string ASPECT_RATIO_SUBDOMAIN = "aspectRatio";
        const string NEAR_PLANE_SUBDOMAIN = "nearPlane";
        const string FAR_PLANE_SUBDOMAIN = "farPlane";
        const string ENABLED_SUBDOMAIN = "enabled";
        const string VIEW_MATRIX_SUBDOMAIN = "viewMatrix";
        const string PROJECTION_MATRIX_SUBDOMAIN = "projectionMatrix";

        const string ACTIVE_CAMERA_INDEX_DOMAIN = "activeCameraIndex";

        public IWorld World { get; set; }

        private QueryEntity queryCameraEntities;
        private UboService _uboService;
        private Dictionary<string, object> valuePairs = new Dictionary<string, object>();
        private bool _isDirty = true;

        public CameraUboRenderSystem(IWorld world)
        {
            World = world;

            queryCameraEntities = this.CreateEntityQuery()
                .With<CameraComponent>()
                .With<TransformComponent>()
                ;

            for (int i = 0; i < LightParams.MAX_CAMERAS; i++)
            {
                CAMERA_DOMAINS[i] = $"{UBO_NAME}.cameras[{i}]";
            }
            valuePairs[$"{UBO_NAME}.{ACTIVE_CAMERA_INDEX_DOMAIN}"] = 0;
        }

        public void Initialize()
        {
            _uboService = ServiceHub.Get<UboService>();
            InitializeCameras();
        }

        private void InitializeCameras()
        {
            for (int i = 0; i < LightParams.MAX_CAMERAS; i++)
            {
                string cameraRoot = CAMERA_DOMAINS[i];

                valuePairs[$"{cameraRoot}.{POSITION_SUBDOMAIN}"] = Vector3.Zero;
                valuePairs[$"{cameraRoot}.{FRONT_SUBDOMAIN}"] = new Vector3(0, 0, 1);
                valuePairs[$"{cameraRoot}.{UP_SUBDOMAIN}"] = new Vector3(0, 1, 0);
                valuePairs[$"{cameraRoot}.{FOV_SUBDOMAIN}"] = MathF.PI / 4f;
                valuePairs[$"{cameraRoot}.{ASPECT_RATIO_SUBDOMAIN}"] = 16f / 9f;
                valuePairs[$"{cameraRoot}.{NEAR_PLANE_SUBDOMAIN}"] = 0.1f;
                valuePairs[$"{cameraRoot}.{FAR_PLANE_SUBDOMAIN}"] = 200f;
                valuePairs[$"{cameraRoot}.{ENABLED_SUBDOMAIN}"] = 0.0f;
                valuePairs[$"{cameraRoot}.{VIEW_MATRIX_SUBDOMAIN}"] = Matrix4x4.Identity;
                valuePairs[$"{cameraRoot}.{PROJECTION_MATRIX_SUBDOMAIN}"] = Matrix4x4.Identity;
            }

            _uboService.SetUboDataByBindingPoint(UBO_BINDING_POINT, valuePairs);
            _uboService.Update(UBO_BINDING_POINT);
        }

        public void Render(double deltaTime, object? context)
        {
            if (!_uboService.HasUboByBindingPoint(UBO_BINDING_POINT))
                return;

            Entity[] cameraEntities = queryCameraEntities.Build();
            if (cameraEntities.Length == 0)
                return;

            bool dataChanged = false;

            for (int i = 0; i < LightParams.MAX_CAMERAS; i++)
            {
                string cameraRoot = CAMERA_DOMAINS[i];
                valuePairs[$"{cameraRoot}.{ENABLED_SUBDOMAIN}"] = 0.0f;
            }

            int activeCameraIndex = -1;
            int cameraCount = Math.Min(cameraEntities.Length, LightParams.MAX_CAMERAS);

            for (int i = 0; i < cameraCount; i++)
            {
                ref var camera = ref this.GetComponent<CameraComponent>(cameraEntities[i]);
                ref var transform = ref this.GetComponent<TransformComponent>(cameraEntities[i]);

                string cameraRoot = CAMERA_DOMAINS[i];

                //var viewMatrix = Matrix4x4.CreateLookAt(transform.Position, transform.Position + camera.CameraFront, camera.CameraUp);

                valuePairs[$"{cameraRoot}.{POSITION_SUBDOMAIN}"] = transform.Position;
                valuePairs[$"{cameraRoot}.{FRONT_SUBDOMAIN}"] = camera.CameraFront;
                valuePairs[$"{cameraRoot}.{UP_SUBDOMAIN}"] = camera.CameraUp;
                valuePairs[$"{cameraRoot}.{FOV_SUBDOMAIN}"] = camera.FieldOfView * (MathF.PI / 180f);
                valuePairs[$"{cameraRoot}.{ASPECT_RATIO_SUBDOMAIN}"] = camera.AspectRatio;
                valuePairs[$"{cameraRoot}.{NEAR_PLANE_SUBDOMAIN}"] = camera.NearPlane;
                valuePairs[$"{cameraRoot}.{FAR_PLANE_SUBDOMAIN}"] = camera.FarPlane;
                valuePairs[$"{cameraRoot}.{ENABLED_SUBDOMAIN}"] = camera.IsActive ? 1.0f : 0.0f;
                valuePairs[$"{cameraRoot}.{VIEW_MATRIX_SUBDOMAIN}"] = camera.ViewMatrix;
                valuePairs[$"{cameraRoot}.{PROJECTION_MATRIX_SUBDOMAIN}"] = camera.CreateProjectionMatrix();

                if (camera.IsActive && activeCameraIndex == -1)
                {
                    activeCameraIndex = i;
                }

                dataChanged = true;
            }

            if (activeCameraIndex == -1 && cameraCount > 0)
            {
                activeCameraIndex = 0;
            }

            if (dataChanged ||
                (activeCameraIndex != -1 && !valuePairs[$"{UBO_NAME}.{ACTIVE_CAMERA_INDEX_DOMAIN}"].Equals(activeCameraIndex)) ||
                _isDirty)
            {
                valuePairs[$"{UBO_NAME}.{ACTIVE_CAMERA_INDEX_DOMAIN}"] = activeCameraIndex;
                _uboService.SetUboDataByBindingPoint(UBO_BINDING_POINT, valuePairs);
                _uboService.Update(UBO_BINDING_POINT);
                _isDirty = false;
            }
        }

        public void Resize(Vector2 size)
        {
        }
    }


}
