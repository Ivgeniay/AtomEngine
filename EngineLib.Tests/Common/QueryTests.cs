

namespace EngineLib.Tests
{
    public class QueryTests
    {
        private World _world;

        //public QueryTests()
        //{
        //    _world = new World();
        //}


        //public struct TestComponent : IComponent
        //{
        //    public Entity Owner { get; }
        //    public int Value;

        //    public TestComponent(Entity owner, int value)
        //    {
        //        Owner = owner;
        //        Value = value;
        //    }
        //}
        //public struct OtherComponent : IComponent
        //{
        //    public Entity Owner { get; }
        //    public string Value;

        //    public OtherComponent(Entity owner, string value)
        //    {
        //        Owner = owner;
        //        Value = value;
        //    }
        //}

        //[Fact]
        //public void With_ShouldReturnEntitiesWithComponent()
        //{
        //    // Arrange
        //    var entity1 = _world.CreateEntity();
        //    var entity2 = _world.CreateEntity();

        //    _world.AddComponent(entity1, new TestComponent(entity1, 1));
        //    _world.AddComponent(entity2, new OtherComponent(entity2, "test"));

        //    // Act
        //    var result = _world.CreateQuery()
        //        .With<TestComponent>()
        //        .Build()
        //        .ToList();

        //    // Assert
        //    Assert.Single(result);
        //    Assert.Equal(entity1.Id, result[0].Id);
        //}

        //[Fact]
        //public void WithMultiple_ShouldReturnEntitiesWithAllComponents()
        //{
        //    // Arrange
        //    var entity1 = _world.CreateEntity();
        //    var entity2 = _world.CreateEntity();

        //    _world.AddComponent(entity1, new TestComponent(entity1, 1));
        //    _world.AddComponent(entity1, new OtherComponent(entity1, "test"));
        //    _world.AddComponent(entity2, new TestComponent(entity2, 2));

        //    // Act
        //    var result = _world.CreateQuery()
        //        .With<TestComponent>()
        //        .With<OtherComponent>()
        //        .Build()
        //        .ToList();

        //    // Assert
        //    Assert.Single(result);
        //    Assert.Equal(entity1.Id, result[0].Id);
        //}

        //[Fact]
        //public void Without_ShouldExcludeEntitiesWithComponent()
        //{
        //    // Arrange
        //    var entity1 = _world.CreateEntity();
        //    var entity2 = _world.CreateEntity();

        //    _world.AddComponent(entity1, new TestComponent(entity1, 1));
        //    _world.AddComponent(entity2, new TestComponent(entity2, 2));
        //    _world.AddComponent(entity2, new OtherComponent(entity2, "test"));

        //    // Act
        //    var result = _world.CreateQuery()
        //        .With<TestComponent>()
        //        .Without<OtherComponent>()
        //        .Build()
        //        .ToList();

        //    // Assert
        //    Assert.Single(result);
        //    Assert.Equal(entity1.Id, result[0].Id);
        //}

        //[Fact]
        //public void Where_ShouldFilterByPredicate()
        //{
        //    // Arrange
        //    var entity1 = _world.CreateEntity();
        //    var entity2 = _world.CreateEntity();

        //    _world.AddComponent(entity1, new TestComponent(entity1, 1));
        //    _world.AddComponent(entity2, new TestComponent(entity2, 2));

        //    // Act
        //    var result = _world.CreateQuery()
        //        .With<TestComponent>()
        //        .Where<TestComponent>(c => c.Value > 1)
        //        .Build()
        //        .ToList();

        //    // Assert
        //    Assert.Single(result);
        //    Assert.Equal(entity2.Id, result[0].Id);
        //}

        //[Fact]
        //public void OrderBy_ShouldSortEntities()
        //{
        //    // Arrange
        //    var entity1 = _world.CreateEntity();
        //    var entity2 = _world.CreateEntity();

        //    _world.AddComponent(entity1, new TestComponent(entity1, 2));
        //    _world.AddComponent(entity2, new TestComponent(entity2, 1));

        //    // Act
        //    var result = _world.CreateQuery()
        //        .With<TestComponent>()
        //        .OrderBy(e => _world.GetComponent<TestComponent>(e).Unwrap().Value)
        //        .Build()
        //        .ToList();

        //    // Assert
        //    Assert.Equal(2, result.Count);
        //    Assert.Equal(entity2.Id, result[0].Id);
        //    Assert.Equal(entity1.Id, result[1].Id);
        //}

        //[Fact]
        //public void Limit_ShouldLimitNumberOfResults()
        //{
        //    // Arrange
        //    var entity1 = _world.CreateEntity();
        //    var entity2 = _world.CreateEntity();

        //    _world.AddComponent(entity1, new TestComponent(entity1, 1));
        //    _world.AddComponent(entity2, new TestComponent(entity2, 2));

        //    // Act
        //    var result = _world.CreateQuery()
        //        .With<TestComponent>()
        //        .Limit(1)
        //        .Build()
        //        .ToList();

        //    // Assert
        //    Assert.Single(result);
        //}

        //[Fact]
        //public void GroupBy_ShouldGroupEntitiesByKey()
        //{
        //    // Arrange
        //    var entity1 = _world.CreateEntity();
        //    var entity2 = _world.CreateEntity();

        //    _world.AddComponent(entity1, new TestComponent(entity1, 1));
        //    _world.AddComponent(entity2, new TestComponent(entity2, 1));

        //    // Act
        //    var result = _world.CreateQuery()
        //        .With<TestComponent>()
        //        .GroupBy(e => _world.GetComponent<TestComponent>(e).Unwrap().Value)
        //        .BuildGrouped()
        //        .ToList();

        //    // Assert
        //    Assert.Single(result);
        //    Assert.Equal(2, result[0].Count());
        //}

        //[Fact]
        //public void InvalidateCache_ShouldRebuildQueryResults()
        //{
        //    // Arrange
        //    var entity1 = _world.CreateEntity();
        //    _world.AddComponent(entity1, new TestComponent(entity1, 1));

        //    var query = _world.CreateQuery().With<TestComponent>();
        //    var firstResult = query.Build().ToList();

        //    // Act
        //    var entity2 = _world.CreateEntity();
        //    _world.AddComponent(entity2, new TestComponent(entity2, 2));

        //    var secondResult = query.Build().ToList();

        //    // Assert
        //    Assert.Single(firstResult);
        //    Assert.Equal(2, secondResult.Count);
        //}
    }

}
