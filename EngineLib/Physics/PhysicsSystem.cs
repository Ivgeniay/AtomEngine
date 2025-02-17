using BepuPhysics.CollisionDetection;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using System.Numerics;
using BepuUtilities;
using BepuPhysics;
using BepuPhysics.Constraints;
using System.Runtime.CompilerServices;

namespace AtomEngine
{
    public class PhysicsSystem : IPhysicSystem, IDisposable
    {
        private Simulation _simulation;
        private BufferPool _bufferPool;
        private World _world;
        private QueryEntity queryEntity;

        Dictionary<CollidableReference, Entity> _collidableToEntityMap = new Dictionary<CollidableReference, Entity>();
        public IWorld World => _world;

        private struct NarrowPhaseCallbacks : INarrowPhaseCallbacks
        {
            private PhysicsSystem _physicsSystem;

            public NarrowPhaseCallbacks(PhysicsSystem physicsSystem)
            {
                _physicsSystem = physicsSystem;
            }

            public void Initialize(Simulation simulation) { }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin)
            {
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
            {
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            bool INarrowPhaseCallbacks.ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties material)
            {
                float CombineMaterials(float a, float b, PhysicMaterialCombine combine)
                {
                    return combine switch
                    {
                        PhysicMaterialCombine.Average => (a + b) * 0.5f,
                        PhysicMaterialCombine.Minimum => MathF.Min(a, b),
                        PhysicMaterialCombine.Maximum => MathF.Max(a, b),
                        PhysicMaterialCombine.Multiply => a * b,
                        _ => (a + b) * 0.5f
                    };
                }

                var materialA = PhysicsMaterial.Default;
                var materialB = PhysicsMaterial.Default;

                bool hasMatA = false;
                bool hasMatB = false;

                if (_physicsSystem._collidableToEntityMap.TryGetValue(pair.A, out Entity entityA))
                {
                    if (_physicsSystem._world.HasComponent<PhysicsMaterialComponent>(entityA))
                    {
                        materialA = _physicsSystem._world.GetComponent<PhysicsMaterialComponent>(entityA).Material;
                        hasMatA = true;
                    }
                }

                if (_physicsSystem._collidableToEntityMap.TryGetValue(pair.B, out Entity entityB))
                {
                    if (_physicsSystem._world.HasComponent<PhysicsMaterialComponent>(entityB))
                    {
                        materialB = _physicsSystem._world.GetComponent<PhysicsMaterialComponent>(entityB).Material;
                        hasMatB = true;
                    }
                }

                float combinedFriction = CombineMaterials(
                    MathF.Sqrt(materialA.StaticFriction * materialA.DynamicFriction),
                    MathF.Sqrt(materialB.StaticFriction * materialB.DynamicFriction),
                    materialA.FrictionCombine
                );

                float combinedBounciness = CombineMaterials(
                    materialA.Bounciness,
                    materialB.Bounciness,
                    materialA.BounceCombine
                );

                material = new PairMaterialProperties(
                    frictionCoefficient: combinedFriction,
                    maximumRecoveryVelocity: 2f + (combinedBounciness * 4f),
                    springSettings: new SpringSettings(
                        (materialA.AngularFrequency + materialB.AngularFrequency) * 0.5f,
                        (materialA.DampingRatio + materialB.DampingRatio) * 0.5f
                    )
                );

                DebLogger.Debug($"Contact between {entityA} and {entityB}:");
                DebLogger.Debug($"Material A (exists: {hasMatA}): SF={materialA.StaticFriction}, DF={materialA.DynamicFriction}");
                DebLogger.Debug($"Material B (exists: {hasMatB}): SF={materialB.StaticFriction}, DF={materialB.DynamicFriction}");
                DebLogger.Debug($"Combined friction: {material.FrictionCoefficient}");

                _physicsSystem.HandleCollision(pair, manifold);

                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold)
            {
                return true;
            }
            public void Dispose() { }
        }

        private struct PoseIntegratorCallbacks : IPoseIntegratorCallbacks
        {
            private PhysicsSystem _physicsSystem;
            public readonly Vector3 Gravity;

            Vector3Wide gravityWideDt;
            Vector<float> linearDampingDt;
            Vector<float> angularDampingDt;

            public float LinearDamping;
            public float AngularDamping;

            public PoseIntegratorCallbacks(PhysicsSystem physicsSystem, Vector3 gravity, float linearDamping = .03f, float angularDamping = .03f)
            {
                Gravity = gravity;
                LinearDamping = linearDamping;
                AngularDamping = angularDamping;
                _physicsSystem = physicsSystem;
            }

            public void Initialize(Simulation simulation)
            {
                gravityWideDt = Vector3Wide.Broadcast(Gravity);
            }

            public void PrepareForIntegration(float dt)
            {
                linearDampingDt = new Vector<float>(MathF.Pow(MathHelper.Clamp(1 - LinearDamping, 0, 1), dt));
                angularDampingDt = new Vector<float>(MathF.Pow(MathHelper.Clamp(1 - AngularDamping, 0, 1), dt));
                gravityWideDt = Vector3Wide.Broadcast(Gravity * dt);
            }

            
            public void IntegrateVelocity(Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation, BodyInertiaWide localInertia, Vector<int> integrationMask, int workerIndex, Vector<float> dt, ref BodyVelocityWide velocity)
            {
                velocity.Linear = (velocity.Linear + gravityWideDt) * linearDampingDt;
                velocity.Angular = velocity.Angular * angularDampingDt;
            }

            public bool AllowSubstepsForUnconstrainedBodies => false;

            public void IntegrateVelocityForKinematics(Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation, Vector<int> integrationMask, int workerIndex, Vector<float> dt, ref BodyVelocityWide velocity, ref Vector<int> inertias)
            { }

            public AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;

            bool IPoseIntegratorCallbacks.IntegrateVelocityForKinematics => false;
        }

        public PhysicsSystem(World world)
        {
            _world = world;
            _bufferPool = new BufferPool();

            var narrowPhaseCallbacks = new NarrowPhaseCallbacks(this);
            var poseIntegratorCallbacks = new PoseIntegratorCallbacks(this, new Vector3(0, Physic.GRAVITY, 0));

            queryEntity = _world.CreateEntityQuery()
                .With<BepuBodyComponent>();

            _simulation = Simulation.Create(
                _bufferPool,
                narrowPhaseCallbacks,
                poseIntegratorCallbacks,
                new SolveDescription(8, 1));
        }

        public void FixedUpdate()
        {
            _simulation.Timestep(Time.FIXED_TIME_STEP);
            Entity[] entities = queryEntity.Build();

            foreach (var entity in entities)
            {
                ref var body = ref _world.GetComponent<BepuBodyComponent>(entity);
                ref var transform = ref _world.GetComponent<TransformComponent>(entity);

                switch (body.BodyType)
                {
                    case BodyType.Dynamic when body.DynamicHandle.HasValue:
                        var dynamicBody = _simulation.Bodies[body.DynamicHandle.Value];
                        transform.Position = new Vector3(
                            dynamicBody.Pose.Position.X,
                            dynamicBody.Pose.Position.Y,
                            dynamicBody.Pose.Position.Z
                        );

                        transform.Rotation = dynamicBody.Pose.Orientation.ToEuler() ;
                        break;

                    case BodyType.Kinematic when body.DynamicHandle.HasValue:
                        var kinematicBody = _simulation.Bodies[body.DynamicHandle.Value];
                        kinematicBody.Pose.Position = new Vector3(
                            transform.Position.X,
                            transform.Position.Y,
                            transform.Position.Z
                        );

                        kinematicBody.Pose.Orientation = Quaternion.CreateFromYawPitchRoll(
                            transform.Rotation.Y,
                            transform.Rotation.X,
                            transform.Rotation.Z
                        );
                        break;

                    case BodyType.Static when body.StaticHandle.HasValue:
                        var staticBody = _simulation.Statics[body.StaticHandle.Value];
                        //var staticBody = _simulation.Bodies[body.DynamicHandle.Value];
                        staticBody.Pose.Position = new Vector3(
                            transform.Position.X,
                            transform.Position.Y,
                            transform.Position.Z
                        );

                        staticBody.Pose.Orientation = Quaternion.CreateFromYawPitchRoll(
                            transform.Rotation.Y.DegreesToRadians(),
                            transform.Rotation.X.DegreesToRadians(),
                            transform.Rotation.Z.DegreesToRadians()
                        );
                        break;
                }
            }
        }

        public void Update(double deltaTime) { }

        public void Initialize() { }


        public void HandleCollision<TManifold>(CollidablePair pair, TManifold manifold) where TManifold : unmanaged, IContactManifold<TManifold>
        {
            var entityA = GetEntityFromCollidable(pair.A);
            var entityB = GetEntityFromCollidable(pair.B);

            if (entityA == Entity.Null || entityB == Entity.Null)
                return;

            AddCollisionEvent(entityA, entityB, manifold);
            AddCollisionEvent(entityB, entityA, manifold);
        }
        
        private Entity GetEntityFromCollidable(CollidableReference collidable) =>
            _collidableToEntityMap[collidable];

        private void AddCollisionEvent<TManifold>(Entity entity, Entity otherEntity, TManifold manifold)
            where TManifold : unmanaged, IContactManifold<TManifold>
        {
            if (!_world.HasComponent<CollisionComponent>(entity)) return;

            ref var collisionComponent = ref _world.GetComponent<CollisionComponent>(entity);

            for(int i =0, count = manifold.Count; i < count; i++)
            {
                manifold.GetContact(i, out Contact contactData);

                collisionComponent.Collisions.Enqueue(new CollisionEvent
                {
                    OtherEntity = otherEntity,
                    ContactPoint = contactData.Offset,
                    Normal = contactData.Normal,
                    Depth = contactData.Depth
                });
            }
        }

        public void CreateDynamicBox(ref TransformComponent transform, Vector3 size, float mass)
        {
            var box = new Box(size.X, size.Y, size.Z);
            var shapeIndex = _simulation.Shapes.Add(box);

            var inertia = box.ComputeInertia(mass);
            var pose = new RigidPose(
                new Vector3(transform.Position.X, transform.Position.Y, transform.Position.Z),
                transform.Rotation.ToQuaternion());

            var description = BodyDescription.CreateDynamic(
                pose, 
                inertia, 
                new CollidableDescription(shapeIndex), 
                0.01f);

            var handle = _simulation.Bodies.Add(description);

            CollidableReference collidableReference = new CollidableReference(CollidableMobility.Dynamic,handle);

            _collidableToEntityMap.Add(collidableReference, transform.Owner);

            _world.AddComponent(transform.Owner, new BepuBodyComponent(transform.Owner, handle, BodyType.Dynamic));
            _world.AddComponent(transform.Owner, new BepuColliderComponent(transform.Owner, shapeIndex));
        }

        public void CreateStaticBox(ref TransformComponent transform, Vector3 size)
        {
            var box = new Box(size.X, size.Y, size.Z);
            var shapeIndex = _simulation.Shapes.Add(box);

            var pose = new RigidPose(
                new Vector3(transform.Position.X, transform.Position.Y, transform.Position.Z),
                transform.Rotation.ToQuaternion());

            var description = new StaticDescription(
                pose, 
                shapeIndex);

            var handle = _simulation.Statics.Add(description);

            CollidableReference collidableReference = new CollidableReference(handle);
            _collidableToEntityMap.Add(collidableReference, transform.Owner);

            _world.AddComponent(transform.Owner, new BepuBodyComponent(transform.Owner, handle));
            _world.AddComponent(transform.Owner, new BepuColliderComponent(transform.Owner, shapeIndex));
        }

        public void Dispose()
        {
            _simulation.Dispose();
            _bufferPool.Clear();
        }

    }

}
