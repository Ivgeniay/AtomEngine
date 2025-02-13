using AtomEngine;
using System.Numerics;

namespace AtomEngine
{
    // Тип тела для определения поведения в симуляции
    public enum BodyType
    {
        Static,     // Неподвижное тело
        Dynamic,    // Полностью симулируемое тело
        Kinematic   // Управляемое программно тело
    }

    public struct RigidBody : IComponent
    {
        public Entity Owner { get; set; }

        // Основные физические свойства
        public float Mass;                // Масса тела
        public float InverseMass;         // 1/Mass - для оптимизации
        public Vector3 Inertia;           // Тензор инерции
        public Vector3 InverseInertia;    // 1/Inertia
        public float Restitution;         // Коэффициент упругости [0,1]
        public float StaticFriction;      // Статическое трение
        public float DynamicFriction;     // Динамическое трение

        // Текущее состояние
        public Vector3 LinearVelocity;    // Линейная скорость
        public Vector3 AngularVelocity;   // Угловая скорость
        public Vector3 Force;             // Накопленная сила
        public Vector3 Torque;            // Накопленный крутящий момент

        // Тип тела и флаги
        public BodyType BodyType;
        public bool IsGravityEnabled;
        public bool IsSleeping;

        // Константы
        private const float SleepEpsilon = 0.001f;
        private const float SleepTimeout = 0.5f;

        // Время в состоянии покоя
        private float _sleepTime;

        public RigidBody(Entity owner, float mass, BodyType bodyType = BodyType.Dynamic)
        {
            Owner = owner;
            Mass = mass;
            InverseMass = mass > 0 ? 1.0f / mass : 0.0f;

            // Для простоты используем сферический тензор инерции
            float i = (2.0f * mass) / 5.0f;
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
            _sleepTime = 0;
        }

        // Методы для применения сил
        public void ApplyForce(Vector3 force)
        {
            if (BodyType != BodyType.Dynamic) return;
            Force += force;
            WakeUp();
        }

        public void ApplyTorque(Vector3 torque)
        {
            if (BodyType != BodyType.Dynamic) return;
            Torque += torque;
            WakeUp();
        }

        public void ApplyImpulse(Vector3 impulse, Vector3 point)
        {
            if (BodyType != BodyType.Dynamic) return;

            LinearVelocity += impulse * InverseMass;
            Vector3 arm = point; // Относительно центра масс
            AngularVelocity += Vector3.Cross(arm, impulse) * InverseInertia;

            WakeUp();
        }

        public void WakeUp()
        {
            IsSleeping = false;
            _sleepTime = 0;
        }

        public void UpdateSleepState(float deltaTime)
        {
            float speedSquared = Vector3.Dot(LinearVelocity, LinearVelocity) +
                               Vector3.Dot(AngularVelocity, AngularVelocity);

            if (speedSquared < SleepEpsilon)
            {
                _sleepTime += deltaTime;
                if (_sleepTime > SleepTimeout)
                {
                    IsSleeping = true;
                    LinearVelocity = Vector3.Zero;
                    AngularVelocity = Vector3.Zero;
                }
            }
            else
            {
                IsSleeping = false;
                _sleepTime = 0;
            }
        }

        public void Integrate(float deltaTime, Vector3 gravity)
        {
            if (BodyType != BodyType.Dynamic || IsSleeping)
                return;

            // Применяем гравитацию
            if (IsGravityEnabled)
                Force += Mass * gravity;

            // Интегрируем линейное движение
            Vector3 acceleration = Force * InverseMass;
            LinearVelocity += acceleration * deltaTime;

            // Интегрируем вращательное движение
            Vector3 angularAcceleration = Torque * InverseInertia;
            AngularVelocity += angularAcceleration * deltaTime;

            // Применяем затухание
            const float damping = 0.98f;
            LinearVelocity *= damping;
            AngularVelocity *= damping;

            // Сбрасываем накопленные силы
            Force = Vector3.Zero;
            Torque = Vector3.Zero;

            // Обновляем состояние сна
            UpdateSleepState(deltaTime);
        }
    }
}
