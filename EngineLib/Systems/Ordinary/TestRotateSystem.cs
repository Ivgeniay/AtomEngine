using AtomEngine;
using System.Numerics;

namespace AtomEngine
{
    public class TestRotateSystem : IRenderSystem
    {
        public IWorld World { get ; set; }
        private QueryEntity queryEntity;

        public TestRotateSystem(IWorld world)
        {
            World = world;

            queryEntity = this.CreateEntityQuery()
                .With<TestRotateComponent>()
                .With<TransformComponent>()
                ;
        }

        public void Initialize()
        { 
        }

        public void Render(double deltaTime, object? context)
        {
            var entities = queryEntity.Build();
            if (entities.Count() == 0) return;

            foreach (var entity in entities)
            {
                ref TransformComponent transform = ref this.GetComponent<TransformComponent>(entity);
                ref TestRotateComponent rotate = ref this.GetComponent<TestRotateComponent>(entity);

                transform.Rotation += (rotate.Axis * rotate.Speed);
            }
        }

        public void Resize(Vector2 size)
        {
        }
    }
}
