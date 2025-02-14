using System.Numerics;

namespace AtomEngine
{
    public enum BodyType
    {
        Static,     
        Dynamic,    
        Kinematic   
    }

    public struct RigidbodyComponent : IComponent
    {
        public Entity Owner { get; set; }

        public float Mass;                // Масса тела
        public float InverseMass;         // 1/Mass - для оптимизации
        public Vector3 Inertia;           // Тензор инерции
        public Vector3 InverseInertia;    // 1/Inertia
        public float Restitution;         // Коэффициент упругости [0,1]
        public float StaticFriction;      // Статическое трение
        public float DynamicFriction;     // Динамическое трение

        public Vector3 LinearVelocity;    // Линейная скорость
        public Vector3 AngularVelocity;   // Угловая скорость
        public Vector3 Force;             // Накопленная сила
        public Vector3 Torque;            // Накопленный крутящий момент

        public BodyType BodyType;
        public bool IsGravityEnabled;
        public bool IsSleeping;

        public const float SleepEpsilon = 0.001f;
        public const float SleepTimeout = 0.5f;

        public RigidbodyComponent(Entity owner, float mass, BodyType bodyType = BodyType.Dynamic)
        {
            Owner = owner;
            Mass = mass;
            InverseMass = mass > 0 ? 1.0f / mass : 0.0f;

            float i = 2.0f * mass / 5.0f;
            Inertia = new Vector3(i, i, i);
            InverseInertia = mass > 0 ? new Vector3(1.0f / i, 1.0f / i, 1.0f / i) : Vector3.Zero;

            Restitution = 0.5f;
            StaticFriction = 0.5f;
            DynamicFriction = 0.3f;

            LinearVelocity = Vector3.Zero;
            AngularVelocity = Vector3.Zero;
            Force = Vector3.Zero;
            Torque = Vector3.Zero;

            BodyType = bodyType;
            IsGravityEnabled = bodyType == BodyType.Dynamic;
            IsSleeping = false;
        }
    }


}
