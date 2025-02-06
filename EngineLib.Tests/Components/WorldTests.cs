
namespace EngineLib.Tests
{
    public class WorldTests : IDisposable
    {
        private readonly World _world;

        public WorldTests()
        {
            _world = new World();
        }

        public void Dispose()
        {
            _world.Dispose();
        }

        private struct TestComponent1 : IComponent
        {
            public Entity Owner { get; }
            public int Value;

            public TestComponent1(Entity owner, int value)
            {
                Owner = owner;
                Value = value;
            }
        }

        private struct TestComponent2 : IComponent
        {
            public Entity Owner { get; }
            public float Value;

            public TestComponent2(Entity owner, float value)
            {
                Owner = owner;
                Value = value;
            }
        }

        // CreateEntity tests
        [Fact]
        public void CreateEntity_ReturnsValidEntity()
        {
            // Act
            var entity = _world.CreateEntity();

            // Assert
            Assert.True(_world.IsEntityValid(entity.Id, entity.Version));
        }

        [Fact]
        public void CreateEntity_MultipleEntities_HaveUniqueIds()
        {
            // Act
            var entity1 = _world.CreateEntity();
            var entity2 = _world.CreateEntity();
            var entity3 = _world.CreateEntity();

            // Assert
            Assert.NotEqual(entity1.Id, entity2.Id);
            Assert.NotEqual(entity2.Id, entity3.Id);
            Assert.NotEqual(entity1.Id, entity3.Id);
        }

        [Fact]
        public void CreateEntity_AfterDestroy_HasDifferentVersion()
        {
            // Arrange
            var entity = _world.CreateEntity();
            uint originalVersion = entity.Version;

            // Act
            _world.DestroyEntity(entity);
            var newEntity = _world.CreateEntity(); // Should reuse the ID

            // Assert
            Assert.Equal(entity.Id, newEntity.Id);
            Assert.NotEqual(originalVersion, newEntity.Version);
        }

        // DestroyEntity tests
        [Fact]
        public void DestroyEntity_MakesEntityInvalid()
        {
            // Arrange
            var entity = _world.CreateEntity();

            // Act
            _world.DestroyEntity(entity);

            // Assert
            Assert.False(_world.IsEntityValid(entity.Id, entity.Version));
        }

        [Fact]
        public void DestroyEntity_RemovesAllComponents()
        {
            // Arrange
            var entity = _world.CreateEntity();
            _world.AddComponent(entity, new TestComponent1(entity, 42));
            _world.AddComponent(entity, new TestComponent2(entity, 3.14f));

            // Act
            _world.DestroyEntity(entity);

            // Assert
            Assert.False(_world.HasComponent<TestComponent1>(entity));
            Assert.False(_world.HasComponent<TestComponent2>(entity));
        }

        // AddComponent tests
        [Fact]
        public void AddComponent_AddsComponentSuccessfully()
        {
            // Arrange
            var entity = _world.CreateEntity();
            var component = new TestComponent1(entity, 42);

            // Act
            _world.AddComponent(entity, component);

            // Assert
            Assert.True(_world.HasComponent<TestComponent1>(entity));
        }

        [Fact]
        public void AddComponent_CanAddMultipleComponents()
        {
            // Arrange
            var entity = _world.CreateEntity();
            var component1 = new TestComponent1(entity, 42);
            var component2 = new TestComponent2(entity, 3.14f);

            // Act
            _world.AddComponent(entity, component1);
            _world.AddComponent(entity, component2);

            // Assert
            Assert.True(_world.HasComponent<TestComponent1>(entity));
            Assert.True(_world.HasComponent<TestComponent2>(entity));
        }


        // GetComponent tests
        [Fact]
        public void GetComponent_ReturnsCorrectComponent()
        {
            // Arrange
            var entity = _world.CreateEntity();
            var component = new TestComponent1(entity, 42);
            _world.AddComponent(entity, component);

            // Act
            ref var retrievedComponent = ref _world.GetComponent<TestComponent1>(entity);

            // Assert
            Assert.Equal(42, retrievedComponent.Value);
        }

        [Fact]
        public void GetComponent_ModifiesComponentByRef()
        {
            // Arrange
            var entity = _world.CreateEntity();
            var component = new TestComponent1(entity, 42);
            _world.AddComponent(entity, component);

            // Act
            ref var retrievedComponent = ref _world.GetComponent<TestComponent1>(entity);
            retrievedComponent.Value = 100;

            // Assert
            Assert.Equal(100, _world.GetComponent<TestComponent1>(entity).Value);
        }

        [Fact]
        public void GetComponent_NonExistentComponent_ThrowsException()
        {
            // Arrange
            var entity = _world.CreateEntity();

            // Act & Assert
            Assert.Throws<ComponentError>(() => _world.GetComponent<TestComponent1>(entity));
        }

        // GetEntitiesByArchetype tests
        [Fact]
        public void GetEntitiesByArchetype_SingleComponent_ReturnsCorrectEntities()
        {
            // Arrange
            var entity1 = _world.CreateEntity();
            var entity2 = _world.CreateEntity();
            _world.AddComponent(entity1, new TestComponent1(entity1, 42));
            _world.AddComponent(entity2, new TestComponent1(entity2, 43));

            // Act
            var entities = _world.GetEntitiesByArchetype<TestComponent1>().ToArray();

            // Assert
            Assert.Equal(2, entities.Length);
            Assert.Contains(entities, e => e.Id == entity1.Id);
            Assert.Contains(entities, e => e.Id == entity2.Id);
        }

        [Fact]
        public void GetEntitiesByArchetype_MultipleComponents_ReturnsCorrectEntities()
        {
            // Arrange
            var entity1 = _world.CreateEntity();
            var entity2 = _world.CreateEntity();

            _world.AddComponent(entity1, new TestComponent1(entity1, 42));
            _world.AddComponent(entity1, new TestComponent2(entity1, 3.14f));
            _world.AddComponent(entity2, new TestComponent1(entity2, 43));

            // Act
            var entities = _world.GetEntitiesByArchetype<TestComponent1, TestComponent2>().ToArray();

            // Assert
            Assert.Single(entities);
            Assert.Equal(entity1.Id, entities[0].Id);
        }

        [Fact]
        public void GetEntitiesByArchetype_NoMatchingEntities_ReturnsEmpty()
        {
            // Arrange
            var entity = _world.CreateEntity();
            _world.AddComponent(entity, new TestComponent1(entity, 42));

            // Act
            var entities = _world.GetEntitiesByArchetype<TestComponent2>().ToArray();

            // Assert
            Assert.Empty(entities);
        }

        // GetEntitiesByArchetypeHaving tests
        [Fact]
        public void GetEntitiesByArchetypeHaving_ReturnsEntitiesWithComponent()
        {
            // Arrange
            var entity1 = _world.CreateEntity();
            var entity2 = _world.CreateEntity();
            _world.AddComponent(entity1, new TestComponent1(entity1, 42));
            _world.AddComponent(entity1, new TestComponent2(entity1, 3.14f));
            _world.AddComponent(entity2, new TestComponent1(entity2, 43));

            // Act
            var entities = _world.GetEntitiesByArchetypeHaving<TestComponent1>().ToArray();

            // Assert
            Assert.Equal(2, entities.Length);
            Assert.Contains(entities, e => e.Id == entity1.Id);
            Assert.Contains(entities, e => e.Id == entity2.Id);
        }

        [Fact]
        public void GetEntitiesByArchetypeHaving_MultipleComponents_ReturnsCorrectEntities()
        {
            // Arrange
            var entity1 = _world.CreateEntity();
            var entity2 = _world.CreateEntity();

            _world.AddComponent(entity1, new TestComponent1(entity1, 42));
            _world.AddComponent(entity1, new TestComponent2(entity1, 3.14f));
            _world.AddComponent(entity2, new TestComponent1(entity2, 43));

            // Act
            var entities = _world.GetEntitiesByArchetypeHaving<TestComponent1, TestComponent2>().ToArray();

            // Assert
            Assert.Single(entities);
            Assert.Equal(entity1.Id, entities[0].Id);
        }

        [Fact]
        public void GetEntitiesByArchetypeHaving_NoMatchingEntities_ReturnsEmpty()
        {
            // Arrange
            var entity = _world.CreateEntity();
            _world.AddComponent(entity, new TestComponent1(entity, 42));

            // Act
            var entities = _world.GetEntitiesByArchetypeHaving<TestComponent2>().ToArray();

            // Assert
            Assert.Empty(entities);
        }
    }
}
