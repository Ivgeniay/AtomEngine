using Moq;

namespace AtomEngine.Tests
{
    public class SystemDependencyGraphTests
    {
        private class TestSystem : ISystem
        {
            public IWorld World { get; }
            public TestSystem(IWorld world) { World = world; }
            public void Update(float deltaTime) { }
        }

        [Fact]
        public void AddSystem_WhenSystemIsNull_ThrowsArgumentNullException()
        {
            var graph = new SystemDependencyGraph();
            Assert.Throws<ArgumentNullException>(() => graph.AddSystem(null));
        }

        [Fact]
        public void AddSystem_WhenSystemIsValid_AddsToGraph()
        {
            var graph = new SystemDependencyGraph();
            var worldMock = new Mock<IWorld>();
            var system = new TestSystem(worldMock.Object);

            graph.AddSystem(system);

            var levels = graph.GetExecutionLevels();
            Assert.Single(levels);
            Assert.Single(levels[0]);
            Assert.Equal(system, levels[0][0]);
        }

        [Fact]
        public void AddDependency_WhenDependentIsNull_ThrowsArgumentNullException()
        {
            var graph = new SystemDependencyGraph();
            var worldMock = new Mock<IWorld>();
            var dependency = new TestSystem(worldMock.Object);

            Assert.Throws<ArgumentNullException>(() => graph.AddDependency(null, dependency));
        }

        [Fact]
        public void AddDependency_WhenDependencyIsNull_ThrowsArgumentNullException()
        {
            var graph = new SystemDependencyGraph();
            var worldMock = new Mock<IWorld>();
            var dependent = new TestSystem(worldMock.Object);

            Assert.Throws<ArgumentNullException>(() => graph.AddDependency(dependent, null));
        }

        [Fact]
        public void AddDependency_WhenSystemDependsOnItself_ThrowsArgumentException()
        {
            var graph = new SystemDependencyGraph();
            var worldMock = new Mock<IWorld>();
            var system = new TestSystem(worldMock.Object);

            Assert.Throws<ArgumentException>(() => graph.AddDependency(system, system));
        }

        [Fact]
        public void AddDependency_CreatesValidExecutionOrder()
        {
            var graph = new SystemDependencyGraph();
            var worldMock = new Mock<IWorld>();
            var system1 = new TestSystem(worldMock.Object);
            var system2 = new TestSystem(worldMock.Object);
            var system3 = new TestSystem(worldMock.Object);

            graph.AddDependency(system2, system1); // system2 depends on system1
            graph.AddDependency(system3, system2); // system3 depends on system2

            var levels = graph.GetExecutionLevels();
            Assert.Equal(3, levels.Count);
            Assert.Contains(system1, levels[0]); // system1 should execute first
            Assert.Contains(system2, levels[1]); // system2 should execute second
            Assert.Contains(system3, levels[2]); // system3 should execute last
        }

        [Fact]
        public void AddDependency_WhenCreatingCircularDependency_ThrowsInvalidOperationException()
        {
            var graph = new SystemDependencyGraph();
            var worldMock = new Mock<IWorld>();
            var system1 = new TestSystem(worldMock.Object);
            var system2 = new TestSystem(worldMock.Object);
            var system3 = new TestSystem(worldMock.Object);

            graph.AddDependency(system2, system1);
            graph.AddDependency(system3, system2);

            Assert.Throws<InvalidOperationException>(() =>
                graph.AddDependency(system1, system3));
        }

        [Fact]
        public void GetExecutionLevels_WithParallelSystems_CreatesCorrectLevels()
        {
            var graph = new SystemDependencyGraph();
            var worldMock = new Mock<IWorld>();
            var system1 = new TestSystem(worldMock.Object);
            var system2 = new TestSystem(worldMock.Object);
            var system3 = new TestSystem(worldMock.Object);
            var system4 = new TestSystem(worldMock.Object);

            // system3 and system4 both depend on system1 and system2
            graph.AddDependency(system3, system1);
            graph.AddDependency(system3, system2);
            graph.AddDependency(system4, system1);
            graph.AddDependency(system4, system2);

            var levels = graph.GetExecutionLevels();
            Assert.Equal(2, levels.Count);
            Assert.Equal(2, levels[0].Count); // system1 and system2 can run in parallel
            Assert.Equal(2, levels[1].Count); // system3 and system4 can run in parallel
        }

        [Fact]
        public void GraphChanged_EventIsRaised_WhenSystemIsAdded()
        {
            var graph = new SystemDependencyGraph();
            var worldMock = new Mock<IWorld>();
            var system = new TestSystem(worldMock.Object);
            var eventRaised = false;

            graph.GraphChanged += () => eventRaised = true;
            graph.AddSystem(system);

            Assert.True(eventRaised);
        }

        [Fact]
        public void GraphChanged_EventIsRaised_WhenDependencyIsAdded()
        {
            var graph = new SystemDependencyGraph();
            var worldMock = new Mock<IWorld>();
            var system1 = new TestSystem(worldMock.Object);
            var system2 = new TestSystem(worldMock.Object);
            var eventRaised = false;

            graph.AddSystem(system1);
            graph.AddSystem(system2);
            graph.GraphChanged += () => eventRaised = true;
            graph.AddDependency(system2, system1);

            Assert.True(eventRaised);
        }

        [Fact]
        public void GetDependencyGraphInfo_ReturnsCorrectString()
        {
            var graph = new SystemDependencyGraph();
            var worldMock = new Mock<IWorld>();
            var system1 = new TestSystem(worldMock.Object);
            var system2 = new TestSystem(worldMock.Object);

            graph.AddSystem(system1);
            graph.AddSystem(system2);
            graph.AddDependency(system2, system1);

            var info = graph.GetDependencyGraphInfo();
            Assert.Contains("TestSystem", info);
            Assert.Contains("->", info);
        }
    }
}