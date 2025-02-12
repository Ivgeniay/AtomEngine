

using System.Numerics;

namespace AtomEngine.Tests
{
   
    public struct TestTransformComponent : IComponent
    {
        private Entity _owner;
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
        public Vector3 Scale { get; set; }

        public TestTransformComponent(Entity owner, Vector3 position, Vector3 rotation, Vector3 scale)
        {
            this._owner = owner;
            Position = position;
            Rotation = rotation;
            Scale = scale;
        }

        public Entity Owner => _owner;
    }
    public struct TestPositionComponent : IComponent 
    {

        private Entity _owner;
        public Vector3 Position { get; set; }
        public TestPositionComponent(Entity owner, Vector3 position)
        {
            this._owner = owner;
            Position = position;
        }
        public Entity Owner => _owner;
    }
    
    
    public class QueryTests : IDisposable
    {
        private readonly World world;
        public QueryTests()
        {
            world = new World();
        }
        public void Dispose()
        {
            world.Dispose();
        }

        [Fact]
        public void With_AddComponent_ToRequired()
        {
            var entity = world.CreateEntity();
            world.AddComponent<TestPositionComponent>(entity, new TestPositionComponent());

            var query = world
                .CreateEntityQuery()
                .With<TestPositionComponent>();
            var queryResult = query.Build();

            Assert.Contains(entity, queryResult);
            
        }

        [Fact]
        public void CreateEntityAddComponent_RequestEntity()
        {
            var entity = world.CreateEntity();
            var entity2 = world.CreateEntity();

            world.AddComponent<TestPositionComponent>(entity, new TestPositionComponent());
            world.AddComponent<TestPositionComponent>(entity2, new TestPositionComponent());
            world.AddComponent<TestTransformComponent>(entity2, new TestTransformComponent());

            var queryResultWithoutTransformComponent = world
                .CreateEntityQuery()
                .With<TestPositionComponent>()
                .Without<TestTransformComponent>()
                .Build();

            var queryResultWithPositionComponent = world
                .CreateEntityQuery()
                .With<TestPositionComponent>()
                .Build();

            var queryResultWithTransformComponent = world
                .CreateEntityQuery()
                .With<TestTransformComponent>()
                .Build();

            Assert.Single(queryResultWithoutTransformComponent);
            Assert.Single(queryResultWithTransformComponent);
            Assert.Equal(2, queryResultWithPositionComponent.Length);
            Assert.Contains(entity, queryResultWithPositionComponent);
            Assert.Contains(entity2, queryResultWithPositionComponent);
            Assert.Contains(entity2, queryResultWithTransformComponent);
            
        }

        [Fact]
        public void OrderBy_RequestEntityOrderBy()
        {
            var entity = world.CreateEntity();
            var entity2 = world.CreateEntity();
            var entity3 = world.CreateEntity();
            var entity4 = world.CreateEntity();
            var entity5 = world.CreateEntity();
            world.AddComponent<TestPositionComponent>(entity, new TestPositionComponent() { Position = new Vector3(0, 0, 0)});
            world.AddComponent<TestPositionComponent>(entity2, new TestPositionComponent() { Position = new Vector3(1, 0, 0)});
            world.AddComponent<TestPositionComponent>(entity3, new TestPositionComponent() { Position = new Vector3(200, 0, 0)});
            world.AddComponent<TestPositionComponent>(entity4, new TestPositionComponent() { Position = new Vector3(300, 0, 0)});
            world.AddComponent<TestPositionComponent>(entity5, new TestPositionComponent() { Position = new Vector3(100, 0, 0)});

            var query = world
                .CreateEntityQuery()
                .With<TestPositionComponent>()
                .OrderBy(e => world.GetComponent<TestPositionComponent>(e).Position.X);
            ;
            var queryResult = query.Build();

            Assert.Equal(5, queryResult.Length);
            Assert.Equal(entity, queryResult[0]);
            Assert.Equal(entity2, queryResult[1]);
            Assert.Equal(entity5, queryResult[2]);
            Assert.Equal(entity3, queryResult[3]);
            Assert.Equal(entity4, queryResult[4]);
        }

        [Fact]
        public void OrderByDesc_RequestEntityOrderByDesc()
        {
            var entity = world.CreateEntity();
            var entity2 = world.CreateEntity();
            var entity3 = world.CreateEntity();
            var entity4 = world.CreateEntity();
            var entity5 = world.CreateEntity();
            world.AddComponent<TestPositionComponent>(entity, new TestPositionComponent() { Position = new Vector3(0, 0, 0) });
            world.AddComponent<TestPositionComponent>(entity2, new TestPositionComponent() { Position = new Vector3(1, 0, 0) });
            world.AddComponent<TestPositionComponent>(entity3, new TestPositionComponent() { Position = new Vector3(200, 0, 0) });
            world.AddComponent<TestPositionComponent>(entity4, new TestPositionComponent() { Position = new Vector3(300, 0, 0) });
            world.AddComponent<TestPositionComponent>(entity5, new TestPositionComponent() { Position = new Vector3(100, 0, 0) });

            var query = world
                .CreateEntityQuery()
                .With<TestPositionComponent>()
                .OrderByDescending(e => world.GetComponent<TestPositionComponent>(e).Position.X);
            ;
            var queryResult = query.Build();

            Assert.Equal(5, queryResult.Length);
            Assert.Equal(entity, queryResult[4]);
            Assert.Equal(entity2, queryResult[3]);
            Assert.Equal(entity5, queryResult[2]);
            Assert.Equal(entity3, queryResult[1]);
            Assert.Equal(entity4, queryResult[0]);
        }

        [Fact]
        public void OrderByDescWithWeherePosMore1_RequestEntityOrderByDesc()
        {
            var entity = world.CreateEntity();
            var entity2 = world.CreateEntity();
            var entity3 = world.CreateEntity();
            var entity4 = world.CreateEntity();
            var entity5 = world.CreateEntity();
            world.AddComponent<TestPositionComponent>(entity, new TestPositionComponent() { Position = new Vector3(0, 0, 0) });
            world.AddComponent<TestPositionComponent>(entity2, new TestPositionComponent() { Position = new Vector3(1, 0, 0) });
            world.AddComponent<TestPositionComponent>(entity3, new TestPositionComponent() { Position = new Vector3(200, 0, 0) });
            world.AddComponent<TestPositionComponent>(entity4, new TestPositionComponent() { Position = new Vector3(300, 0, 0) });
            world.AddComponent<TestPositionComponent>(entity5, new TestPositionComponent() { Position = new Vector3(100, 0, 0) });

            var query = world
                .CreateEntityQuery()
                .With<TestPositionComponent>()
                .OrderByDescending(e => world.GetComponent<TestPositionComponent>(e).Position.X)
                .Where(e => world.GetComponent<TestPositionComponent>(e).Position.X > 1);
            ;
            var queryResult = query.Build();

            Assert.Equal(3, queryResult.Length);
            Assert.Equal(entity5, queryResult[2]);
            Assert.Equal(entity3, queryResult[1]);
            Assert.Equal(entity4, queryResult[0]);
        }

        [Fact]
        public void OrderByDescWithLimit2_RequestEntityOrderByDesc()
        {
            var entity = world.CreateEntity();
            var entity2 = world.CreateEntity();
            var entity3 = world.CreateEntity();
            var entity4 = world.CreateEntity();
            var entity5 = world.CreateEntity();
            world.AddComponent<TestPositionComponent>(entity, new TestPositionComponent() { Position = new Vector3(0, 0, 0) });
            world.AddComponent<TestPositionComponent>(entity2, new TestPositionComponent() { Position = new Vector3(1, 0, 0) });
            world.AddComponent<TestPositionComponent>(entity3, new TestPositionComponent() { Position = new Vector3(200, 0, 0) });
            world.AddComponent<TestPositionComponent>(entity4, new TestPositionComponent() { Position = new Vector3(300, 0, 0) });
            world.AddComponent<TestPositionComponent>(entity5, new TestPositionComponent() { Position = new Vector3(100, 0, 0) });

            var query = world
                .CreateEntityQuery()
                .With<TestPositionComponent>()
                .OrderByDescending(e => world.GetComponent<TestPositionComponent>(e).Position.X)
                .Limit(2);
            ;
            var queryResult = query.Build();

            Assert.Equal(2, queryResult.Length);
            Assert.Equal(entity4, queryResult[0]);
            Assert.Equal(entity3, queryResult[1]);
        }
    }
}
