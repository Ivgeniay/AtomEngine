using System.Numerics;
using AtomEngine;


namespace AtomEngine.Tests
{
    public class ArchetypePoolTests : IDisposable
    {
        private readonly ArchetypePool _pool;

        public ArchetypePoolTests()
        {
            _pool = new ArchetypePool();
        }

        public void Dispose()
        {
            _pool.Dispose();
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

        [Fact]
        public void AddEntityToArchetype_SingleComponent_Success()
        {
            // Arrange
            using (World world = new World())
            {
                var entity = world.CreateEntity();
                var component = new TestComponent1(entity, 42);
                var components = new IComponent[] { component };
                var types = new[] { typeof(TestComponent1) };

                // Act
                _pool.AddEntityToArchetype(entity.Id, components, types);

                // Assert
                var entities = _pool.GetEntitiesWith<TestComponent1>().ToArray();
                Assert.Single(entities);
                Assert.Equal(entity.Id, entities[0]);
            }
        }

        [Fact]
        public void AddEntityToArchetype_MultipleComponents_Success()
        {
            // Arrange
            using (World world = new World())
            {
                var entity = world.CreateEntity();
                var component1 = new TestComponent1(entity, 42);
                var component2 = new TestComponent2(entity, 3.14f);
                var components = new IComponent[] { component1, component2 };
                var types = new[] { typeof(TestComponent1), typeof(TestComponent2) };

                // Act
                _pool.AddEntityToArchetype(entity.Id, components, types);

                // Assert
                var entities = _pool.GetEntitiesWith<TestComponent1, TestComponent2>().ToArray();
                Assert.Single(entities);
                Assert.Equal(entity.Id, entities[0]);
            }
        }

        [Fact]
        public void RemoveEntity_ExistingEntity_Success()
        {
            // Arrange
            using (World world = new World())
            {
                var entity = world.CreateEntity();
                var component = new TestComponent1(entity, 42);
                var components = new IComponent[] { component };
                var types = new[] { typeof(TestComponent1) };
                _pool.AddEntityToArchetype(entity.Id, components, types);

                // Act
                _pool.RemoveEntity(entity.Id);

                // Assert
                var entities = _pool.GetEntitiesWith<TestComponent1>().ToArray();
                Assert.Empty(entities);
            }
        }

        [Fact]
        public void GetComponent_ExistingComponent_ReturnsCorrectValue()
        {
            // Arrange
            using (World world = new World())
            {
                var entity = world.CreateEntity();
                var component = new TestComponent1(entity, 42);
                var components = new IComponent[] { component };
                var types = new[] { typeof(TestComponent1) };
                _pool.AddEntityToArchetype(entity.Id, components, types);

                // Act
                ref var retrievedComponent = ref _pool.GetComponent<TestComponent1>(entity.Id, 0);

                // Assert
                Assert.Equal(42, retrievedComponent.Value);
            }
        }

        [Fact]
        public void GetEntitiesWith_MultipleEntities_ReturnsCorrectEntities()
        {
            // Arrange
            using (World world = new World())
            {
                var entity1 = world.CreateEntity();
                var entity2 = world.CreateEntity();
                var component1 = new TestComponent1(entity1, 42);
                var component2 = new TestComponent1(entity2, 43);

                _pool.AddEntityToArchetype(entity1.Id, new IComponent[] { component1 }, new[] { typeof(TestComponent1) });
                _pool.AddEntityToArchetype(entity2.Id, new IComponent[] { component2 }, new[] { typeof(TestComponent1) });

                // Act
                var entities = _pool.GetEntitiesWith<TestComponent1>();

                // Assert
                Assert.Equal(2, entities.Length);
                Assert.Contains(entity1.Id, entities.ToArray());
                Assert.Contains(entity2.Id, entities.ToArray());
            }
        }

        [Fact]
        public void AddComponent_ToExistingEntity_UpdatesArchetype()
        {
            // Arrange
            using (World world = new World())
            {
                var entity = world.CreateEntity();
                var component1 = new TestComponent1(entity, 42);
                _pool.AddEntityToArchetype(entity.Id, new IComponent[] { component1 }, new[] { typeof(TestComponent1) });

                // Act
                var component2 = new TestComponent2(entity, 3.14f);
                var newComponents = new IComponent[] { component1, component2 };
                var newTypes = new[] { typeof(TestComponent1), typeof(TestComponent2) };
                _pool.AddEntityToArchetype(entity.Id, newComponents, newTypes);

                // Assert
                var entities1 = _pool.GetEntitiesHaving<TestComponent1>().ToArray();
                var entities2 = _pool.GetEntitiesHaving<TestComponent2>().ToArray();
                var entitiesBoth = _pool.GetEntitiesHaving<TestComponent1, TestComponent2>().ToArray();

                Assert.Single(entities1);
                Assert.Single(entities2);
                Assert.Single(entitiesBoth);
                Assert.Equal(entity.Id, entitiesBoth[0]);
            }
        }

        [Fact]
        public void GetEntitiesHaving_ReturnsCorrectEntities()
        {
            // Arrange
            using (World world = new World())
            {
                var entity1 = world.CreateEntity();
                var entity2 = world.CreateEntity();
                var component1 = new TestComponent1(entity1, 42);
                var component2 = new TestComponent2(entity1, 3.14f);
                var component3 = new TestComponent1(entity2, 43);

                _pool.AddEntityToArchetype(entity1.Id, new IComponent[] { component1, component2 },
                    new[] { typeof(TestComponent1), typeof(TestComponent2) });
                _pool.AddEntityToArchetype(entity2.Id, new IComponent[] { component3 },
                    new[] { typeof(TestComponent1) });

                // Act
                var entitiesWithComp1 = _pool.GetEntitiesHaving<TestComponent1>();
                var entitiesWithBoth = _pool.GetEntitiesHaving<TestComponent1, TestComponent2>();

                // Assert
                Assert.Equal(2, entitiesWithComp1.Count());
                Assert.Single(entitiesWithBoth);
                Assert.Contains(entity1.Id, entitiesWithBoth);
            }
        }
        

        [Fact]
        public void AddEntityToArchetype_DuplicateComponents_ThrowsException()
        {
            // Arrange
            using (World world = new World())
            {
                var entity = world.CreateEntity();
                var component1 = new TestComponent1(entity, 42);
                var component2 = new TestComponent1(entity, 43);
                var components = new IComponent[] { component1, component2 };
                var types = new[] { typeof(TestComponent1), typeof(TestComponent1) };

                // Act & Assert
                Assert.Throws<ArgumentException>(() =>
                    _pool.AddEntityToArchetype(entity.Id, components, types));
            }
        }

        [Fact]
        public void GetComponent_NonExistentEntity_ThrowsException()
        {
            // Arrange
            var nonExistentEntityId = 999u;

            // Act & Assert
            Assert.Throws<KeyNotFoundException>(() =>
                _pool.GetComponent<TestComponent1>(nonExistentEntityId, 0));
        }

        [Fact]
        public void RemoveEntity_NonExistentEntity_DoesNotThrow()
        {
            // Arrange
            var nonExistentEntityId = 999u;

            // Act & Assert
            var exception = Record.Exception(() => _pool.RemoveEntity(nonExistentEntityId));
            Assert.Null(exception);
        }
    }
}