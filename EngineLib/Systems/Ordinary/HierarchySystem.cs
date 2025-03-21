namespace AtomEngine
{
    [ExecutionOnScene]
    public class HierarchySystem : ISystem
    {
        public IWorld World { get; set; }

        private QueryEntity _hierarchyEntitiesQuery;
        private QueryEntity _transformOnlyEntitiesQuery;

        public HierarchySystem(IWorld world)
        {
            World = world;

            _hierarchyEntitiesQuery = this.CreateEntityQuery()
                .With<HierarchyComponent>()
                .With<TransformComponent>();

            _transformOnlyEntitiesQuery = this.CreateEntityQuery()
                .Without<HierarchyComponent>()
                .With<TransformComponent>();
        }

        public void Initialize()
        {
            Update(0);
        }

        public void Update(double deltaTime)
        {
            ProcessHierarchicalEntities();
            ProcessNonHierarchicalEntities();
        }

        private void ProcessHierarchicalEntities()
        {
            Entity[] entities = _hierarchyEntitiesQuery.Build();

            Dictionary<uint, Entity> entityLookup = new Dictionary<uint, Entity>();
            foreach (var entity in entities)
            {
                entityLookup[entity.Id] = entity;
            }

            foreach (var entity in entities)
            {
                ref var hierarchy = ref this.GetComponent<HierarchyComponent>(entity);
                ref var transform = ref this.GetComponent<TransformComponent>(entity);

                if (hierarchy.Parent != uint.MaxValue)
                {
                    if (entityLookup.TryGetValue(hierarchy.Parent, out Entity parentEntity))
                    {
                        if (World.IsEntityValid(parentEntity.Id, parentEntity.Version))
                        {
                            ref var parentTransform = ref this.GetComponent<TransformComponent>(parentEntity);
                            transform.parentWorldMatrix = parentTransform.GetModelMatrix();
                        }
                        else
                        {
                            hierarchy.Parent = uint.MaxValue;
                            transform.parentWorldMatrix = null;
                        }
                    }
                }
                else
                {
                    transform.parentWorldMatrix = null;
                }
            }
        }

        private void ProcessNonHierarchicalEntities()
        {
            Entity[] entities = _transformOnlyEntitiesQuery.Build();
            foreach (var entity in entities)
            {
                ref var transform = ref this.GetComponent<TransformComponent>(entity);
                transform.parentWorldMatrix = null;
            }
        }
    }
}
