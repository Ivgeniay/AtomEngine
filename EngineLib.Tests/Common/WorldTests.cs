
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
            Assert.True(world.IsEntityValid(ref entity));
        }

        [Fact]
        public void AddComponent_ShouldAddComponentToEntity()
        {
            // Arrange
            var world = new World();
            var entity = world.CreateEntity();
            var component = new TransformComponent(entity);

            // Act
            world.AddComponent(ref entity, component);

            // Assert
            var result = world.GetComponent<TransformComponent>(ref entity);
            var exception = Record.Exception(() => world.GetComponent<TransformComponent>(ref entity));
            Assert.Null(exception);
        }

        [Fact]
        public void DestroyEntity_ShouldInvalidateEntity()
        {
            // Arrange
            var world = new World();
            var entity = world.CreateEntity();

            // Act
            world.DestroyEntity(ref entity);

            // Assert
            Assert.False(world.IsEntityValid(ref entity));
        }
    }
}
