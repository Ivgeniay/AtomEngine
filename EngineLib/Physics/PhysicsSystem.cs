using System.Numerics;

namespace AtomEngine
{
    public class PhysicsSystem : ISystem
    {
        public IWorld World { get; }

        //private readonly Vector3 _gravity = new(0, -9.81f, 0);
        private readonly Vector3 _gravity = new(0, -1.5f, 0);
        private const int SOLVER_ITERATIONS = 10;
        private const float DAMPING = 0.98f;

        private readonly QueryEntity _dynamicBodiesQuery;
        private readonly QueryEntity _kinematicBodiesQuery;

        public PhysicsSystem(IWorld world)
        {
            World = world;

            // Создаем запросы для разных типов тел
            _dynamicBodiesQuery = world.CreateEntityQuery()
                .With<RigidbodyComponent, TransformComponent>()
                .Where<RigidbodyComponent>(rb => rb.BodyType == BodyType.Dynamic);

            _kinematicBodiesQuery = world.CreateEntityQuery()
                .With<RigidbodyComponent, TransformComponent>()
                .Where<RigidbodyComponent>(rb => rb.BodyType == BodyType.Kinematic);
        }

        public void Initialize() { }

        public void Update(double deltaTime)
        {
            float dt = (float)deltaTime;

            IntegrateBodies(dt);

            var collisions = ((World)World).GetCurrentCollisions();

            for (int i = 0; i < SOLVER_ITERATIONS; i++)
            {
                foreach (var manifold in collisions)
                {
                    ResolveCollision(manifold, dt);
                }
            }

            UpdateTransforms(dt);
        }

        private void IntegrateBodies(float dt)
        {
            var bodies = _dynamicBodiesQuery.Build();

            foreach (var entity in bodies)
            {
                ref var rb = ref World.GetComponent<RigidbodyComponent>(entity);

                if (rb.IsSleeping)
                    continue;

                if (rb.IsGravityEnabled)
                {
                    rb.Force += rb.Mass * _gravity;
                }

                Vector3 acceleration = rb.Force * rb.InverseMass;
                rb.LinearVelocity += acceleration * dt;

                Vector3 angularAcceleration = rb.Torque * rb.InverseInertia;
                rb.AngularVelocity += angularAcceleration * dt;

                rb.LinearVelocity *= DAMPING;
                rb.AngularVelocity *= DAMPING;

                UpdateSleepState(ref rb, dt);

                rb.Force = Vector3.Zero;
                rb.Torque = Vector3.Zero;
            }
        }

        private void UpdateTransforms(float dt)
        {
            var bodies = _dynamicBodiesQuery.Build();

            foreach (var entity in bodies)
            {
                ref var rb = ref World.GetComponent<RigidbodyComponent>(entity);
                ref var transform = ref World.GetComponent<TransformComponent>(entity);

                if (rb.IsSleeping)
                    continue;

                transform.Position += rb.LinearVelocity * dt;

                Vector3 rotationDelta = rb.AngularVelocity * dt;
                transform.Rotation += rotationDelta;
            }
        }

        private void ResolveCollision(CollisionManifold manifold, float dt)
        {
            var entityA = manifold.BodyA;
            var entityB = manifold.BodyB;

            ref var rbA = ref World.GetComponent<RigidbodyComponent>(entityA);
            ref var rbB = ref World.GetComponent<RigidbodyComponent>(entityB);

            if (rbA.BodyType == BodyType.Static && rbB.BodyType == BodyType.Static)
                return;

            Vector3 normal = manifold.GetAverageNormal();
            float penetration = manifold.GetMaxPenetration();
            Vector3 contactPoint = manifold.GetContactPoint();

            foreach (var contact in manifold.GetContacts())
            {
                ResolveContact(ref rbA, ref rbB, contact, manifold.RestitutionCoefficient, manifold.FrictionCoefficient);
            }
        }

        private void ResolveContact(ref RigidbodyComponent rbA, ref RigidbodyComponent rbB,
                                  ContactPoint contact, float restitution, float friction)
        {
            Vector3 relativeVelocity = rbB.LinearVelocity - rbA.LinearVelocity;

            float normalImpulse = -(1.0f + restitution) * Vector3.Dot(relativeVelocity, contact.Normal);
            normalImpulse /= rbA.InverseMass + rbB.InverseMass;

            Vector3 impulse = contact.Normal * normalImpulse;
            ApplyImpulse(ref rbA, ref rbB, impulse, contact.Position);

            ApplyFriction(ref rbA, ref rbB, normalImpulse, relativeVelocity, contact.Normal, friction);
        }

        private void ApplyImpulse(ref RigidbodyComponent rbA, ref RigidbodyComponent rbB,
                                Vector3 impulse, Vector3 point)
        {
            if (rbA.BodyType == BodyType.Dynamic)
            {
                rbA.LinearVelocity -= impulse * rbA.InverseMass;
                Vector3 torque = Vector3.Cross(point, -impulse);
                rbA.AngularVelocity -= torque * rbA.InverseInertia;
                rbA.IsSleeping = false;
            }

            if (rbB.BodyType == BodyType.Dynamic)
            {
                rbB.LinearVelocity += impulse * rbB.InverseMass;
                Vector3 torque = Vector3.Cross(point, impulse);
                rbB.AngularVelocity += torque * rbB.InverseInertia;
                rbB.IsSleeping = false;
            }
        }

        public void ApplyImpulse(ref RigidbodyComponent rbA, Vector3 impulse, Vector3 point)
        {
            if (rbA.BodyType != BodyType.Dynamic) return;

            rbA.LinearVelocity += impulse * rbA.InverseMass;
            Vector3 arm = point;
            rbA.AngularVelocity += Vector3.Cross(arm, impulse) * rbA.InverseInertia;
            rbA.IsSleeping = false;
        }

        private void ApplyFriction(ref RigidbodyComponent rbA, ref RigidbodyComponent rbB,
                                 float normalImpulse, Vector3 relativeVelocity, Vector3 normal, float friction)
        {
            Vector3 tangent = relativeVelocity - Vector3.Dot(relativeVelocity, normal) * normal;

            if (tangent.LengthSquared() > float.Epsilon)
            {
                tangent = Vector3.Normalize(tangent);
                float tangentImpulse = -Vector3.Dot(relativeVelocity, tangent);
                tangentImpulse /= rbA.InverseMass + rbB.InverseMass;

                Vector3 frictionImpulse = tangent * MathF.Min(tangentImpulse, friction * normalImpulse);

                if (rbA.BodyType == BodyType.Dynamic)
                    rbA.LinearVelocity -= frictionImpulse * rbA.InverseMass;

                if (rbB.BodyType == BodyType.Dynamic)
                    rbB.LinearVelocity += frictionImpulse * rbB.InverseMass;
            }
        }

        private void UpdateSleepState(ref RigidbodyComponent rb, float dt)
        {
            if (rb.BodyType != BodyType.Dynamic)
                return;

            float speedSquared = rb.LinearVelocity.LengthSquared() +
                               rb.AngularVelocity.LengthSquared();

            if (speedSquared < RigidbodyComponent.SleepTimeout)
            {
                rb.IsSleeping = true;
                rb.LinearVelocity = Vector3.Zero;
                rb.AngularVelocity = Vector3.Zero;
            }
            else
            {
                rb.IsSleeping = false;
            }
        }

        public void ApplyForce(Entity entity, Vector3 force)
        {
            ref var rb = ref World.GetComponent<RigidbodyComponent>(entity);
            if (rb.BodyType != BodyType.Dynamic)
                return;

            rb.Force += force;
            rb.IsSleeping = false;
        }

        public void ApplyTorque(Entity entity, Vector3 torque)
        {
            ref var rb = ref World.GetComponent<RigidbodyComponent>(entity);
            if (rb.BodyType != BodyType.Dynamic)
                return;

            rb.Torque += torque;
            rb.IsSleeping = false;
        }
    }
}
