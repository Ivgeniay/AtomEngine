using System.Collections.Concurrent;

namespace EngineLib.Tests
{
    public class WorldMultithreadingTests
    {
        [Fact]
        public void ParallelEntityCreation_ShouldCreateUniqueEntities()
        {
            // Arrange
            var world = new World();
            var entityCount = 10000;
            var entities = new ConcurrentBag<Entity>();

            // Act
            Parallel.For(0, entityCount, _ =>
            {
                var entity = world.CreateEntity();
                entities.Add(entity);
            });

            // Assert
            Assert.Equal(entityCount, entities.Count);
            var uniqueIds = entities.Select(e => e.Id).Distinct().Count();
            Assert.Equal(entityCount, uniqueIds);
        }

        [Fact]
        public void ParallelComponentAddition_ShouldBeThreadSafe()
        {
            // Arrange
            var world = new World();
            var entity = world.CreateEntity();
            var componentCount = 1000;

            // Act
            Parallel.For(0, componentCount, i =>
            {
                var component = new TestComponent(entity, i);
                world.AddComponent(entity, component);
            });

            // Assert
            var query = world.CreateQuery()
                .With<TestComponent>()
                .Build()
                .ToList();

            Assert.Single(query); // Только одна сущность должна иметь компонент
        }

        [Fact]
        public async Task ParallelSystemExecution_ShouldRespectDependencies()
        {
            // Arrange
            var world = new World();
            var executionOrder = new ConcurrentQueue<string>();

            var system1 = new TestSystem(world, "System1", executionOrder);
            var system2 = new TestSystem(world, "System2", executionOrder);
            var system3 = new TestSystem(world, "System3", executionOrder);

            world.AddSystem(system1);
            world.AddSystem(system2);
            world.AddSystem(system3);

            world.AddSystemDependency(system2, system1); // system2 зависит от system1
            world.AddSystemDependency(system3, system2); // system3 зависит от system2

            // Act
            await world.UpdateAsync(0.1f);

            // Assert
            var orderList = executionOrder.ToList();
            Assert.Equal(3, orderList.Count);
            Assert.Equal("System1", orderList[0]);
            Assert.Equal("System2", orderList[1]);
            Assert.Equal("System3", orderList[2]);
        }

        [Fact]
        public void ParallelQueryExecution_ShouldBeThreadSafe()
        {
            // Arrange
            var world = new World();
            var entityCount = 1000;
            var queries = new List<Query>();

            // Create entities with components
            for (int i = 0; i < entityCount; i++)
            {
                var entity = world.CreateEntity();
                world.AddComponent(entity, new TestComponent(entity, i));
            }

            // Act
            Parallel.For(0, 100, _ =>
            {
                var query = world.CreateQuery()
                    .With<TestComponent>()
                    .Where<TestComponent>(c => c.Value % 2 == 0)
                    .Build()
                    .ToList();

                Assert.Equal(entityCount / 2, query.Count);
            });
        }

        
        public struct TestComponent : IComponent
        {
            public Entity Owner { get; }
            public int Value;

            public TestComponent(Entity owner, int value)
            {
                Owner = owner;
                Value = value;
            }
        }

        public class TestSystem : System
        {
            private readonly string _name;
            private readonly ConcurrentQueue<string> _executionOrder;

            public TestSystem(World world, string name, ConcurrentQueue<string> executionOrder)
                : base(world)
            {
                _name = name;
                _executionOrder = executionOrder;
            }

            public override void Update(float deltaTime)
            {
                Thread.Sleep(10); // Имитация работы
                _executionOrder.Enqueue(_name);
            }
        }
    }
}
