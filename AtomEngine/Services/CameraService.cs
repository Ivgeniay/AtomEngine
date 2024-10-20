using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtomEngine.Services
{
    public class CameraService
    {
        public static CameraComponent? CurrentCamera { get; private set; }

        private readonly List<CameraComponent> cameraComponents = new List<CameraComponent>();

        public void SetCurrentCamera(CameraComponent cameraComponent)
        {
            if (cameraComponents.Contains(cameraComponent))
            {
                CurrentCamera = cameraComponent;
            }
        }

        public void RegisterCamera(CameraComponent cameraComponent)
        {
            if (!cameraComponents.Contains(cameraComponent))
            {
                cameraComponents.Add(cameraComponent);
            }
        }

        public void UnregisterCamera(CameraComponent cameraComponent)
        {
            if (cameraComponents.Contains(cameraComponent))
            {
                cameraComponents.Remove(cameraComponent);
            }
        }


    }

    public interface ICameraService
    {
        public void Initialize();
        public void Update();
        public void Render();
    }
}
