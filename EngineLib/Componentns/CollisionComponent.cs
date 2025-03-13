using System.Numerics;

namespace AtomEngine
{

    public struct CollisionComponent : IComponent
    {
        public Entity Owner { get; set; }
        public Queue<CollisionEvent> Collisions;

        public CollisionComponent(Entity owner)
        {
            Owner = owner;
            Collisions = new Queue<CollisionEvent>();
        }
    }

    public struct CollisionEvent
    {
        public Entity OtherEntity;      // Сущность, с которой произошла коллизия
        public Vector3 ContactPoint;    // Точка контакта
        public Vector3 Normal;          // Нормаль коллизии
        public float Depth;           // Импульс коллизии
    }
}
