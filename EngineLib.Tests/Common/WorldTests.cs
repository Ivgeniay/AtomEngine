using EngineLib.Componentns;

namespace EngineLib.Tests
{
    public class WorldTests
    {
        [Fact]
        public void CreateEntity_ShouldReturnValidEntity()
        {
            // Arrange
            var world = new World();

            // Act
            var entity = world.CreateEntity();

            // Assert
            Assert.True(world.IsEntityValid(entity));
        }

        [Fact]
        public void AddComponent_ShouldAddComponentToEntity()
        {
            // Arrange
            var world = new World();
            var entity = world.CreateEntity();
            var component = new TransformComponent(entity);

            // Act
            world.AddComponent(entity, component);

            // Assert
            var result = world.GetComponentOrNone<TransformComponent>(entity);
            Assert.True(result.IsSome());
        }

        [Fact]
        public void DestroyEntity_ShouldInvalidateEntity()
        {
            // Arrange
            var world = new World();
            var entity = world.CreateEntity();

            // Act
            world.DestroyEntity(entity);

            // Assert
            Assert.False(world.IsEntityValid(entity));
        }
    }
}
