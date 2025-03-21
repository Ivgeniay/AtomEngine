namespace AtomEngine
{
    public class TransformRequirementSystem : ISystem
    {
        public IWorld World { get; set; }

        private QueryEntity _missingTransformQuery;

        public TransformRequirementSystem(IWorld world)
        {
            World = world;

            _missingTransformQuery = this.CreateEntityQuery()
                .With<HierarchyComponent>()
                .Without<TransformComponent>();
        }

        public void Initialize()
        {
            Update(0);
        }

        public void Update(double deltaTime)
        {
            Entity[] entities = _missingTransformQuery.Build();

            foreach (var entity in entities)
            {
                World.AddComponent(entity, new TransformComponent(entity));
            }
        }
    }
}
