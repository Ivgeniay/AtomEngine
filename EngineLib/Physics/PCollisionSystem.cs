using System.Numerics;
using static AtomEngine.GJKAlgorithm;

namespace AtomEngine
{
    public class PCollisionSystem : IPhysicSystem
    {
        private World _world;
        public IWorld World => _world;
        private BVHPool _bvhPool;
        private Dictionary<CollisionPair, CollisionState> _activeCollisions = new Dictionary<CollisionPair, CollisionState>();
        private int _currentFrame = 0;
        private QueryEntity dynamicBodyQuery;
        public PCollisionSystem(World world) {
            _world = world;
            _bvhPool = world.BvhPool;
            dynamicBodyQuery = new QueryEntity(world)
                .With<TransformComponent>()
                .With<BoundingComponent>()
                .With<ColliderComponent>()
                .With<RigidbodyComponent>();
        }

        public void FixedUpdate() { }
        public void Initialize() { }
        public void Update(double deltaTime) {
            _currentFrame++;
            HashSet<CollisionPair> currentFrameCollisions = new HashSet<CollisionPair>();
            Entity[] dynamicEntities = dynamicBodyQuery.Build();

            foreach (var dynamicEntity in dynamicEntities)
            {
                var potentialCollisions = CheckBroadPhase(dynamicEntity);

                foreach (var staticEntity in potentialCollisions)
                {
                    var pair = new CollisionPair(dynamicEntity, staticEntity);

                    if (_activeCollisions.TryGetValue(pair, out var state))
                    {
                        // Для активных коллизий используем только GJK
                        if (CheckGJKCollision(dynamicEntity, staticEntity))
                        {
                            // Коллизия продолжается
                            state.LastFrameUpdated = _currentFrame;
                            OnCollisionStay(state.LastManifold);
                        }
                        else
                        {
                            // Коллизия закончилась
                            OnCollisionExit(state.LastManifold);
                            state.IsActive = false;
                        }
                    }
                    else
                    {
                        // Для новых коллизий используем полную проверку
                        var manifold = CheckDetailedCollision(dynamicEntity, staticEntity);
                        if (manifold.HasContacts)
                        {
                            OnCollisionEnter(manifold);
                            _activeCollisions.Add(pair, new CollisionState(manifold, _currentFrame));
                        }
                    }
                }
            }

            var endedCollisions = _activeCollisions.Where(kvp =>
            kvp.Value.IsActive && kvp.Value.LastFrameUpdated != _currentFrame).ToList();

            foreach (var kvp in endedCollisions)
            {
                OnCollisionExit(kvp.Value.LastManifold);
                kvp.Value.IsActive = false;
            }
        }


        private List<Entity> CheckBroadPhase(Entity dynamicEntity)
        {
            var dynamicTransform = _world.GetComponent<TransformComponent>(dynamicEntity);
            var dynamicCollider = _world.GetComponent<ColliderComponent>(dynamicEntity);
            var dynamicBounds = dynamicCollider.ComputeBounds().Transform(dynamicTransform.GetModelMatrix());

            return _bvhPool.GetPotentialCollisions(dynamicBounds);
        }

        // Уровень 2: GJK без EPA
        private bool CheckGJKCollision(Entity dynamicEntity, Entity staticEntity)
        {
            var dynamicTransform = _world.GetComponent<TransformComponent>(dynamicEntity);
            var staticTransform = _world.GetComponent<TransformComponent>(staticEntity);
            var dynamicCollider = _world.GetComponent<ColliderComponent>(dynamicEntity);
            var staticCollider = _world.GetComponent<ColliderComponent>(staticEntity);

            SupportFunction supportA = direction =>
            {
                Matrix4x4 rotationMatrix = dynamicTransform.GetRotationMatrix();
                var localDir = Vector3.Transform(direction, Matrix4x4.Transpose(rotationMatrix));
                var localSupport = dynamicCollider.GetSupport(localDir);
                return Vector3.Transform(localSupport, dynamicTransform.GetModelMatrix());
            };

            SupportFunction supportB = direction =>
            {
                Matrix4x4 rotationMatrix = staticTransform.GetRotationMatrix();
                var localDir = Vector3.Transform(direction, Matrix4x4.Transpose(rotationMatrix));
                var localSupport = staticCollider.GetSupport(localDir);
                return Vector3.Transform(localSupport, staticTransform.GetModelMatrix());
            };

            GJKAlgorithm.Simplex simplex;
            return GJKAlgorithm.Intersect(supportA, supportB, out simplex);
        }

        // Уровень 3: Полная проверка с EPA
        private CollisionManifold CheckDetailedCollision(Entity dynamicEntity, Entity staticEntity)
        {
            var manifold = new CollisionManifold(dynamicEntity, staticEntity);

            var dynamicTransform = _world.GetComponent<TransformComponent>(dynamicEntity);
            var staticTransform = _world.GetComponent<TransformComponent>(staticEntity);
            var dynamicCollider = _world.GetComponent<ColliderComponent>(dynamicEntity);
            var staticCollider = _world.GetComponent<ColliderComponent>(staticEntity);

            SupportFunction supportA = direction =>
            {
                Matrix4x4 rotationMatrix = dynamicTransform.GetRotationMatrix();
                var localDir = Vector3.Transform(direction, Matrix4x4.Transpose(rotationMatrix));
                var localSupport = dynamicCollider.GetSupport(localDir);
                return Vector3.Transform(localSupport, dynamicTransform.GetModelMatrix());
            };

            SupportFunction supportB = direction =>
            {
                Matrix4x4 rotationMatrix = staticTransform.GetRotationMatrix();
                var localDir = Vector3.Transform(direction, Matrix4x4.Transpose(rotationMatrix));
                var localSupport = staticCollider.GetSupport(localDir);
                return Vector3.Transform(localSupport, staticTransform.GetModelMatrix());
            };

            GJKAlgorithm.Simplex simplex;
            if (!GJKAlgorithm.Intersect(supportA, supportB, out simplex))
            {
                return manifold;
            }

            Vector3 normal;
            float penetrationDepth;
            Vector3 contactPoint;

            if (EPAAlgorithm.GetContactInfo(simplex, supportA, supportB,
                out normal, out penetrationDepth, out contactPoint))
            {
                manifold.TryAddContact(contactPoint, normal, penetrationDepth);
            }

            return manifold;
        }

        private void OnCollisionEnter(CollisionManifold manifold)
        {
            DebLogger.Debug($"Collision Enter: {manifold}");
            if (manifold.ContactCount == 0)
            {
            }
        }

        private void OnCollisionStay(CollisionManifold manifold)
        {
            DebLogger.Debug($"Collision Stay: {manifold}");
        }

        private void OnCollisionExit(CollisionManifold manifold)
        {
            DebLogger.Debug($"Collision Exit: {manifold}");
        }
    }

    public readonly struct CollisionPair : IEquatable<CollisionPair>
    {
        public readonly Entity EntityA;
        public readonly Entity EntityB;

        public CollisionPair(Entity a, Entity b)
        {
            if (a.Id < b.Id)
            {
                EntityA = a;
                EntityB = b;
            }
            else
            {
                EntityA = b;
                EntityB = a;
            }
        }

        public bool Equals(CollisionPair other) => EntityA == other.EntityA && EntityB == other.EntityB;

        public override bool Equals(object obj) => obj is CollisionPair other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(EntityA, EntityB);
    }

    public class CollisionState
    {
        public CollisionManifold LastManifold;
        public bool IsActive;
        public int LastFrameUpdated;

        public CollisionState(CollisionManifold manifold, int currentFrame)
        {
            LastManifold = manifold;
            IsActive = true;
            LastFrameUpdated = currentFrame;
        }
    }
}
