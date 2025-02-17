using System.Numerics;

namespace AtomEngine
{
    //public class PPhysicsSystem : IPhysicSystem
    //{
    //    public IWorld World { get; }
    //    public bool IsSimulating { get; set; } = true;
    //    // Физические константы
    //    private readonly Vector3 _gravity = new(0, -9.81f, 0);
    //    private const int SOLVER_ITERATIONS = 10;
    //    private const float VELOCITY_DAMPING = 0.995f;          // Уменьшили затухание
    //    private const float ANGULAR_DAMPING  = 0.995f;          // Уменьшили затухание
    //    private const float SLEEP_EPSILON    = 0.02f;           // Пороговая скорость для сна
    //    private const float SLEEP_TIMEOUT    = 0.5f;            // Время в секундах до засыпания
    //    private const float MAX_LINEAR_VELOCITY = 100.0f;       // Максимальная линейная скорость
    //    private const float MAX_ANGULAR_VELOCITY = 30.0f;       // Максимальная угловая скорость
    //    private readonly Dictionary<Entity, float> _sleepTimers = new();

    //    // Кешированные запросы
    //    private readonly QueryEntity _dynamicBodiesQuery;
    //    private readonly QueryEntity _kinematicBodiesQuery;

    //    public PPhysicsSystem(IWorld world)
    //    {
    //        World = world;

    //        _dynamicBodiesQuery = world.CreateEntityQuery()
    //            .With<RigidbodyComponent, TransformComponent>()
    //            .Where<RigidbodyComponent>(rb => rb.BodyType == BodyType.Dynamic);

    //        _kinematicBodiesQuery = world.CreateEntityQuery()
    //            .With<RigidbodyComponent, TransformComponent>()
    //            .Where<RigidbodyComponent>(rb => rb.BodyType == BodyType.Kinematic);
    //    }

    //    public void Initialize() { }

    //    public void Update(double deltaTime) { }

    //    public void FixedUpdate()
    //    {
    //        IntegrateBodies();

    //        List<CollisionManifold> collisions = new();// World.GetCurrentCollisions();

    //        for (int i = 0; i < SOLVER_ITERATIONS; i++)
    //        {
    //            foreach (var manifold in collisions)
    //            {
    //                ResolveCollision(manifold);
    //            }
    //        }
    //        if (!IsSimulating) return;
    //        UpdateTransforms();
    //    }

    //    private void IntegrateBodies()
    //    {
    //        var bodies = _dynamicBodiesQuery.Build();

    //        foreach (var entity in bodies)
    //        {
    //            ref var rb = ref World.GetComponent<RigidbodyComponent>(entity);

    //            //if (rb.IsSleeping)
    //            //    continue;

    //            //// Применяем гравитацию
    //            //if (rb.IsGravityEnabled)
    //            //{
    //            //    rb.Force += rb.Mass * _gravity;
    //            //}

    //            //// Обновляем линейную скорость
    //            //Vector3 acceleration = rb.Force * rb.InverseMass;
    //            //rb.LinearVelocity += acceleration * Time.FIXED_TIME_STEP;
    //            //rb.LinearVelocity *= VELOCITY_DAMPING;

    //            //// Ограничиваем максимальную скорость
    //            //if (rb.LinearVelocity.LengthSquared() > MAX_LINEAR_VELOCITY * MAX_LINEAR_VELOCITY)
    //            //{
    //            //    rb.LinearVelocity = Vector3.Normalize(rb.LinearVelocity) * MAX_LINEAR_VELOCITY;
    //            //}

    //            //// Обновляем угловую скорость
    //            //Vector3 angularAcceleration = rb.Torque * rb.InverseInertia;
    //            //rb.AngularVelocity += angularAcceleration * Time.FIXED_TIME_STEP;
    //            //rb.AngularVelocity *= ANGULAR_DAMPING;

    //            //// Ограничиваем максимальную угловую скорость
    //            //if (rb.AngularVelocity.LengthSquared() > MAX_ANGULAR_VELOCITY * MAX_ANGULAR_VELOCITY)
    //            //{
    //            //    rb.AngularVelocity = Vector3.Normalize(rb.AngularVelocity) * MAX_ANGULAR_VELOCITY;
    //            //}

    //            //UpdateSleepState(ref rb);

    //            //// Сбрасываем силы
    //            //rb.Force = Vector3.Zero;
    //            //rb.Torque = Vector3.Zero;
    //        }
    //    }

    //    private void UpdateTransforms()
    //    {
    //        var bodies = _dynamicBodiesQuery.Build();

    //        foreach (var entity in bodies)
    //        {
    //            ref var rb = ref World.GetComponent<RigidbodyComponent>(entity);
    //            ref var transform = ref World.GetComponent<TransformComponent>(entity);

    //            //if (rb.IsSleeping)
    //            //    continue;

    //            //transform.Position += rb.LinearVelocity * Time.FIXED_TIME_STEP;
    //            //Vector3 rotationDelta = rb.AngularVelocity * Time.FIXED_TIME_STEP;
    //            //transform.Rotation = new Vector3(
    //            //    NormalizeAngle(transform.Rotation.X + rotationDelta.X),
    //            //    NormalizeAngle(transform.Rotation.Y + rotationDelta.Y),
    //            //    NormalizeAngle(transform.Rotation.Z + rotationDelta.Z)
    //            //);
    //        }
    //    }

    //    private float NormalizeAngle(float angle)
    //    {
    //        const float TWO_PI = MathF.PI * 2;
    //        angle = angle % TWO_PI;
    //        if (angle < 0)
    //            angle += TWO_PI;
    //        return angle;
    //    }

    //    private void ResolveCollision(CollisionManifold manifold)
    //    {
    //        var entityA = manifold.BodyA;
    //        var entityB = manifold.BodyB;

    //        bool hasRigidbodyA = World.HasComponent<RigidbodyComponent>(entityA);
    //        bool hasRigidbodyB = World.HasComponent<RigidbodyComponent>(entityB);

    //        if (!hasRigidbodyA && !hasRigidbodyB) return;

    //        if (hasRigidbodyA && !hasRigidbodyB)
    //        {
    //            ref var rbA = ref World.GetComponent<RigidbodyComponent>(entityA);
    //            if (rbA.BodyType == BodyType.Dynamic)
    //            {
    //                ProcessOneBodyCollision(ref rbA, manifold, true);
    //            }
    //            return;
    //        }

    //        if (!hasRigidbodyA && hasRigidbodyB)
    //        {
    //            ref var rbB = ref World.GetComponent<RigidbodyComponent>(entityB);
    //            if (rbB.BodyType == BodyType.Dynamic)
    //            {
    //                ProcessOneBodyCollision(ref rbB, manifold, false);
    //            }
    //            return;
    //        }

    //        ref var rigidbodyA = ref World.GetComponent<RigidbodyComponent>(entityA);
    //        ref var rigidbodyB = ref World.GetComponent<RigidbodyComponent>(entityB);
    //        if (rigidbodyA.BodyType == BodyType.Static && rigidbodyB.BodyType == BodyType.Static)
    //            return;

    //        Vector3 normal = manifold.GetAverageNormal();
    //        float penetration = manifold.GetMaxPenetration();

    //        if (penetration > 0.01f)
    //        {
    //            float totalMass = rigidbodyA.InverseMass + rigidbodyB.InverseMass;
    //            if (totalMass > 0)
    //            {
    //                const float percent = 0.4f;
    //                const float slop = 0.01f;
    //                Vector3 correction = normal * (Math.Max(penetration - slop, 0.0f) * percent);

    //                float ratioA = rigidbodyA.InverseMass / totalMass;
    //                float ratioB = rigidbodyB.InverseMass / totalMass;

    //                if (rigidbodyA.BodyType == BodyType.Dynamic)
    //                    World.GetComponent<TransformComponent>(entityA).Position -= correction * ratioA;
    //                if (rigidbodyB.BodyType == BodyType.Dynamic)
    //                    World.GetComponent<TransformComponent>(entityB).Position += correction * ratioB;
    //            }
    //        }

    //        foreach (var contact in manifold.GetContacts())
    //        {
    //            ResolveDynamicContact(ref rigidbodyA, ref rigidbodyB, contact, normal, manifold.RestitutionCoefficient, manifold.FrictionCoefficient);
    //        }
    //    }

    //    private void ProcessOneBodyCollision(ref RigidbodyComponent rb, CollisionManifold manifold, bool isBodyA)
    //    {
    //        if (rb.BodyType != BodyType.Dynamic)
    //            return;

    //        Vector3 normal = manifold.GetAverageNormal();
    //        if (!isBodyA) normal = -normal; // Инвертируем нормаль если это второе тело

    //        float penetration = manifold.GetMaxPenetration();

    //        // Позиционная коррекция
    //        if (penetration > 0.01f)
    //        {
    //            const float percent = 0.4f;
    //            const float slop = 0.01f;
    //            Vector3 correction = normal * (Math.Max(penetration - slop, 0.0f) * percent);
    //            World.GetComponent<TransformComponent>(rb.Owner).Position -= correction;
    //        }

    //        foreach (var contact in manifold.GetContacts())
    //        {
    //            //Vector3 relativeVelocity = rb.LinearVelocity;

    //            // Проекция скорости на нормаль
    //            //float velocityAlongNormal = Vector3.Dot(relativeVelocity, normal);

    //            // Обрабатываем столкновение только если тела сближаются
    //            // При падении на платформу velocityAlongNormal будет отрицательным
    //            // т.к. нормаль направлена вверх, а скорость вниз

    //            // Вычисляем импульс с учетом коэффициента упругости
    //            float restitution = manifold.RestitutionCoefficient;

    //            // Базовый импульс для остановки движения
    //            float j = -velocityAlongNormal;

    //            // Добавляем упругость только если скорость достаточно велика
    //            if (Math.Abs(velocityAlongNormal) > 2.0f)
    //            {
    //                j *= (1.0f + restitution);
    //            }

    //            j /= rb.InverseMass;

    //            Vector3 impulse = j * normal;

    //            // Применяем импульс
    //            rb.LinearVelocity += impulse * rb.InverseMass;

    //            // Обработка трения
    //            {
    //                Vector3 tangent = relativeVelocity - velocityAlongNormal * normal;
    //                if (tangent.LengthSquared() > float.Epsilon)
    //                {
    //                    tangent = Vector3.Normalize(tangent);
    //                    float frictionImpulse = -Vector3.Dot(relativeVelocity, tangent);
    //                    frictionImpulse /= rb.InverseMass;

    //                    float maxFriction = Math.Abs(j) * manifold.FrictionCoefficient;
    //                    frictionImpulse = Math.Clamp(frictionImpulse, -maxFriction, maxFriction);

    //                    rb.LinearVelocity += tangent * frictionImpulse * rb.InverseMass;
    //                }
    //            }

    //            // Применяем дополнительное затухание для низких скоростей
    //            if (rb.LinearVelocity.LengthSquared() < 1.0f)
    //            {
    //                rb.LinearVelocity *= 0.8f; // Сильное затухание для низких скоростей
    //            }
    //            else
    //            {
    //                rb.LinearVelocity *= 0.98f; // Обычное затухание
    //            }
    //        }

    //        // Ограничение максимальной скорости
    //        const float MAX_VELOCITY = 20.0f;
    //        if (rb.LinearVelocity.LengthSquared() > MAX_VELOCITY * MAX_VELOCITY)
    //        {
    //            rb.LinearVelocity = Vector3.Normalize(rb.LinearVelocity) * MAX_VELOCITY;
    //        }

    //        rb.IsSleeping = false;
    //    }

    //    private void ResolveDynamicContact(ref RigidbodyComponent rbA, ref RigidbodyComponent rbB,
    //                             ContactPoint contact, Vector3 normal, float restitution, float friction)
    //    {
    //        const float CONTACT_OFFSET = 0.01f;      // Расстояние до начала обработки контакта
    //        const float BOUNCE_THRESHOLD = 0.2f;      // Минимальная скорость для отскока
    //        const float MIN_VELOCITY = 0.01f;         // Минимальная скорость для обработки

    //        ref var transformA = ref World.GetComponent<TransformComponent>(rbA.Owner);
    //        ref var transformB = ref World.GetComponent<TransformComponent>(rbB.Owner);

    //        // 1. Позиционная коррекция
    //        float penetration = contact.Penetration;
    //        float totalInverseMass = rbA.InverseMass + rbB.InverseMass;
    //        if (penetration > CONTACT_OFFSET)
    //        {
    //            if (totalInverseMass > 0)
    //            {
    //                // Unity использует более осторожную коррекцию
    //                float correctionMagnitude = (penetration - CONTACT_OFFSET) * 0.2f;
    //                Vector3 correction = normal * correctionMagnitude;

    //                if (rbA.BodyType == BodyType.Dynamic)
    //                    transformA.Position -= correction * (rbA.InverseMass / totalInverseMass);
    //                if (rbB.BodyType == BodyType.Dynamic)
    //                    transformB.Position += correction * (rbB.InverseMass / totalInverseMass);
    //            }
    //        }

    //        // 2. Вычисление импульса
    //        Vector3 relativeVelocity = rbB.LinearVelocity - rbA.LinearVelocity;
    //        float velocityAlongNormal = Vector3.Dot(relativeVelocity, normal);
    //        // Если скорость слишком мала, пропускаем обработку
    //        if (MathF.Abs(velocityAlongNormal) < MIN_VELOCITY)
    //            return;

    //        // Используем restitution только если скорость достаточно высока
    //        float effectiveRestitution = velocityAlongNormal < -BOUNCE_THRESHOLD ? restitution : 0.0f;

    //        // Вычисляем импульс
    //        float j = -(1.0f + effectiveRestitution) * velocityAlongNormal;
    //        j /= totalInverseMass;

    //        Vector3 impulse = normal * j;

    //        // 3. Применяем импульс
    //        if (rbA.BodyType == BodyType.Dynamic)
    //            rbA.LinearVelocity -= impulse * rbA.InverseMass;
    //        if (rbB.BodyType == BodyType.Dynamic)
    //            rbB.LinearVelocity += impulse * rbB.InverseMass;

    //        // 4. Трение (как в Unity)
    //        if (friction > 0)
    //        {
    //            relativeVelocity = rbB.LinearVelocity - rbA.LinearVelocity;
    //            Vector3 tangent = relativeVelocity - Vector3.Dot(relativeVelocity, normal) * normal;

    //            if (tangent.LengthSquared() > MIN_VELOCITY * MIN_VELOCITY)
    //            {
    //                tangent = Vector3.Normalize(tangent);
    //                float jt = -Vector3.Dot(relativeVelocity, tangent);
    //                jt /= totalInverseMass;

    //                // Unity использует статическое/динамическое трение
    //                float maxFriction = MathF.Abs(j) * friction;
    //                if (MathF.Abs(jt) < maxFriction)
    //                {
    //                    // Статическое трение
    //                    if (rbA.BodyType == BodyType.Dynamic)
    //                        rbA.LinearVelocity -= tangent * (jt * rbA.InverseMass);
    //                    if (rbB.BodyType == BodyType.Dynamic)
    //                        rbB.LinearVelocity += tangent * (jt * rbB.InverseMass);
    //                }
    //                else
    //                {
    //                    // Динамическое трение
    //                    if (rbA.BodyType == BodyType.Dynamic)
    //                        rbA.LinearVelocity -= tangent * (maxFriction * rbA.InverseMass);
    //                    if (rbB.BodyType == BodyType.Dynamic)
    //                        rbB.LinearVelocity += tangent * (maxFriction * rbB.InverseMass);
    //                }
    //            }
    //        }

    //        // 5. Применяем демпфинг (как в Unity)
    //        const float LINEAR_DAMPING = 0.99f;
    //        if (rbA.BodyType == BodyType.Dynamic)
    //            rbA.LinearVelocity *= LINEAR_DAMPING;
    //        if (rbB.BodyType == BodyType.Dynamic)
    //            rbB.LinearVelocity *= LINEAR_DAMPING;
    //    }

    //    private void ApplyImpulse(ref RigidbodyComponent rbA, ref RigidbodyComponent rbB,
    //                    Vector3 impulse, Vector3 point)
    //    {
    //        if (rbA.BodyType == BodyType.Dynamic)
    //        {
    //            rbA.LinearVelocity -= impulse * rbA.InverseMass;
    //            Vector3 torque = Vector3.Cross(point, -impulse);
    //            rbA.AngularVelocity -= torque * rbA.InverseInertia;
    //            rbA.IsSleeping = false;
    //        }

    //        if (rbB.BodyType == BodyType.Dynamic)
    //        {
    //            rbB.LinearVelocity += impulse * rbB.InverseMass;
    //            Vector3 torque = Vector3.Cross(point, impulse);
    //            rbB.AngularVelocity += torque * rbB.InverseInertia;
    //            rbB.IsSleeping = false;
    //        }
    //    }

    //    private void ApplyFriction(ref RigidbodyComponent rbA, ref RigidbodyComponent rbB,
    //                             float normalImpulse, Vector3 relativeVelocity, Vector3 normal, float friction)
    //    {
    //        Vector3 tangent = relativeVelocity - Vector3.Dot(relativeVelocity, normal) * normal;

    //        if (tangent.LengthSquared() > float.Epsilon)
    //        {
    //            tangent = Vector3.Normalize(tangent);
    //            float tangentImpulse = -Vector3.Dot(relativeVelocity, tangent);
    //            tangentImpulse /= rbA.InverseMass + rbB.InverseMass;

    //            Vector3 frictionImpulse = tangent * MathF.Min(tangentImpulse, friction * normalImpulse);
    //            if (rbA.BodyType == BodyType.Dynamic)
    //                rbA.LinearVelocity -= frictionImpulse * rbA.InverseMass;
    //            if (rbB.BodyType == BodyType.Dynamic)
    //                rbB.LinearVelocity += frictionImpulse * rbB.InverseMass;
    //        }
    //    }

    //    private void UpdateSleepState(ref RigidbodyComponent rb)
    //    {
    //        if (rb.BodyType != BodyType.Dynamic || rb.IsGravityEnabled)
    //            return;

    //        float speedSquared = rb.LinearVelocity.LengthSquared() +
    //                           rb.AngularVelocity.LengthSquared();

    //        // Проверяем скорость объекта
    //        if (speedSquared < SLEEP_EPSILON)
    //        {
    //            // Если объект уже почти неподвижен, увеличиваем таймер
    //            if (!_sleepTimers.ContainsKey(rb.Owner))
    //            {
    //                _sleepTimers[rb.Owner] = 0f;
    //            }

    //            _sleepTimers[rb.Owner] += Time.FIXED_TIME_STEP;

    //            // Если объект был достаточно долго неподвижен, усыпляем его
    //            if (_sleepTimers[rb.Owner] >= SLEEP_TIMEOUT)
    //            {
    //                rb.IsSleeping = true;
    //                rb.LinearVelocity = Vector3.Zero;
    //                rb.AngularVelocity = Vector3.Zero;
    //                _sleepTimers.Remove(rb.Owner);
    //            }
    //        }
    //        else
    //        {
    //            // Если объект движется, сбрасываем таймер
    //            rb.IsSleeping = false;
    //            _sleepTimers.Remove(rb.Owner);
    //        }
    //    }

    //    private void WakeOnCollision(ref RigidbodyComponent rb)
    //    {
    //        if (rb.IsSleeping)
    //        {
    //            rb.IsSleeping = false;
    //            _sleepTimers.Remove(rb.Owner);
    //        }
    //    }

    //    public void ApplyForce(Entity entity, Vector3 force)
    //    {
    //        ref var rb = ref World.GetComponent<RigidbodyComponent>(entity);
    //        if (rb.BodyType != BodyType.Dynamic)
    //            return;

    //        rb.Force += force;
    //        rb.IsSleeping = false;
    //    }

    //    public void ApplyTorque(Entity entity, Vector3 torque)
    //    {
    //        ref var rb = ref World.GetComponent<RigidbodyComponent>(entity);
    //        if (rb.BodyType != BodyType.Dynamic)
    //            return;

    //        rb.Torque += torque;
    //        rb.IsSleeping = false;
    //    }

    //    public void ApplyImpulse(ref RigidbodyComponent rb, Vector3 impulse, Vector3 point)
    //    {
    //        if (rb.BodyType != BodyType.Dynamic) return;

    //        rb.LinearVelocity += impulse * rb.InverseMass;
    //        Vector3 arm = point;
    //        rb.AngularVelocity += Vector3.Cross(arm, impulse) * rb.InverseInertia;
    //        rb.IsSleeping = false;
    //    }
    //}
}
