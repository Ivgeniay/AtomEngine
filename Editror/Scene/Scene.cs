using AtomEngine;

namespace Editor
{
    internal class Scene
    {
        public SceneData SceneData { get; set; }
        public bool IsDirty { get; set; }

        public void AddEntity(EntityData entityData)
        {
            if (!SceneData.Entities.Contains(entityData))
            {

            }
            SceneData.Entities
            IsDirty = true;
        }
    }
}
