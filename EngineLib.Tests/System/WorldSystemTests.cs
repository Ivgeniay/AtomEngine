using Moq;


namespace AtomEngine.Tests
{
    public class WorldSystemTests : IDisposable
    {
        private readonly World _world;
        private readonly Mock<ISystem> _mockSystem1;
        private readonly Mock<ISystem> _mockSystem2;
        private float _updateDeltaTime = 1.0f;

        public WorldSystemTests()
        {
            _world = new World();
            _mockSystem1 = new Mock<ISystem>();
            _mockSystem2 = new Mock<ISystem>();

            // Настраиваем базовые свойства моков
            _mockSystem1.Setup(s => s.World).Returns(_world);
            _mockSystem2.Setup(s => s.World).Returns(_world);
        }

        public void Dispose()
        {
            _world.Dispose();
        }

        [Fact]
        public void AddSystem_SystemIsAdded_CanBeUpdated()
        {
            // Arrange
            var updateCalled = false;
            _mockSystem1.Setup(s => s.Update(_updateDeltaTime))
                        .Callback(() => updateCalled = true);

            // Act
            _world.AddSystem(_mockSystem1.Object);
            _world.Update(_updateDeltaTime);

            // Assert
            Assert.True(updateCalled);
        }

        [Fact]
        public async Task UpdateAsync_MultipleSystems_UpdatedInCorrectOrder()
        {
            // Arrange
            var executionOrder = new List<int>();
            _mockSystem1.Setup(s => s.Update(_updateDeltaTime))
                        .Callback(() => executionOrder.Add(1));
            _mockSystem2.Setup(s => s.Update(_updateDeltaTime))
                        .Callback(() => executionOrder.Add(2));

            // Act
            _world.AddSystem(_mockSystem1.Object);
            _world.AddSystem(_mockSystem2.Object);
            _world.AddSystemDependency(_mockSystem2.Object, _mockSystem1.Object);
            await _world.UpdateAsync(_updateDeltaTime);

            // Assert
            Assert.Equal(new[] { 1, 2 }, executionOrder);
        }

        [Fact]
        public void Update_SystemThrowsException_OtherSystemsContinueExecution()
        {
            // Arrange
            var system2Updated = false;
            _mockSystem1.Setup(s => s.Update(_updateDeltaTime))
                        .Throws(new Exception("Test exception"));
            _mockSystem2.Setup(s => s.Update(_updateDeltaTime))
                        .Callback(() => system2Updated = true);

            // Act
            _world.AddSystem(_mockSystem1.Object);
            _world.AddSystem(_mockSystem2.Object);
            _world.Update(_updateDeltaTime);

            // Assert
            Assert.True(system2Updated);
        }

        [Fact]
        public async Task UpdateAsync_ParallelSystems_ExecutedConcurrently()
        {
            // Arrange
            var execution1 = new TaskCompletionSource<bool>();
            var execution2 = new TaskCompletionSource<bool>();
            var system1Started = false;
            var system2Started = false;

            _mockSystem1.Setup(s => s.Update(_updateDeltaTime))
                        .Callback(() =>
                        {
                            system1Started = true;
                            // Ждем, пока второй поток тоже начнет выполнение
                            while (!system2Started) Thread.Sleep(1);
                            execution1.SetResult(true);
                        });

            _mockSystem2.Setup(s => s.Update(_updateDeltaTime))
                        .Callback(() =>
                        {
                            system2Started = true;
                            // Ждем, пока первый поток тоже начнет выполнение
                            while (!system1Started) Thread.Sleep(1);
                            execution2.SetResult(true);
                        });

            // Act
            _world.AddSystem(_mockSystem1.Object);
            _world.AddSystem(_mockSystem2.Object);
            var updateTask = _world.UpdateAsync(_updateDeltaTime);

            // Assert
            await Task.WhenAll(execution1.Task, execution2.Task);
            await updateTask;
            Assert.True(system1Started && system2Started);
        }

        [Fact]
        public void AddSystemDependency_ValidDependency_SystemsExecuteInCorrectOrder()
        {
            // Arrange
            var executionOrder = new List<int>();
            _mockSystem1.Setup(s => s.Update(_updateDeltaTime))
                        .Callback(() => executionOrder.Add(1));
            _mockSystem2.Setup(s => s.Update(_updateDeltaTime))
                        .Callback(() => executionOrder.Add(2));

            // Act
            _world.AddSystem(_mockSystem1.Object);
            _world.AddSystem(_mockSystem2.Object);
            _world.AddSystemDependency(_mockSystem2.Object, _mockSystem1.Object);
            _world.Update(_updateDeltaTime);

            // Assert
            Assert.Equal(new[] { 1, 2 }, executionOrder);
        }

        [Fact]
        public void AddSystemDependency_CircularDependency_ThrowsException()
        {
            // Arrange
            _world.AddSystem(_mockSystem1.Object);
            _world.AddSystem(_mockSystem2.Object);

            // Act & Assert
            _world.AddSystemDependency(_mockSystem2.Object, _mockSystem1.Object);
            Assert.Throws<InvalidOperationException>(() =>
                _world.AddSystemDependency(_mockSystem1.Object, _mockSystem2.Object));
        }

        private class TestSystem : ISystem
        {
            public IWorld World { get; }
            private readonly Action<double> _updateAction;

            public TestSystem(IWorld world, Action<double> updateAction)
            {
                World = world;
                _updateAction = updateAction;
            }

            public void Update(double deltaTime)
            {
                _updateAction(deltaTime);
            } 
        }

        [Fact]
        public void Update_WithLongRunningSystem_CompletesSuccessfully()
        {
            // Arrange
            var longRunningCompleted = false;
            var fastSystemCompleted = false;

            var longRunningSystem = new TestSystem(_world, _ =>
            {
                Thread.Sleep(100); // Имитируем длительную работу
                longRunningCompleted = true;
            });

            var fastSystem = new TestSystem(_world, _ =>
            {
                fastSystemCompleted = true;
            });

            // Act
            _world.AddSystem(longRunningSystem);
            _world.AddSystem(fastSystem);
            _world.Update(_updateDeltaTime);

            // Assert
            Assert.True(longRunningCompleted);
            Assert.True(fastSystemCompleted);
        }
    }
}
