using AtomEngine;

namespace Editor
{
    public struct EditorCameraControllerComponent : IComponent
    {
        public Entity Owner { get; set; }
        public bool IsActive;
    }
}