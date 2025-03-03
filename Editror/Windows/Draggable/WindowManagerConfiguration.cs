using System.Collections.Generic;

namespace Editor
{
    public class WindowManagerConfiguration
    {
        public Dictionary<MainControllers, WindowConfiguration> Configurations { get; set; } = new Dictionary<MainControllers, WindowConfiguration>
        {
            { MainControllers.Hierarchy, new WindowConfiguration { Title = "Hierarchy" } },
            { MainControllers.World, new WindowConfiguration { Title = "World List" } },
            { MainControllers.Inspector, new WindowConfiguration { Title = "Inspector" } },
            { MainControllers.Explorer, new WindowConfiguration { Title = "Explorer" } },
            { MainControllers.Console, new WindowConfiguration { Title = "Console", IsOpen = true } },
            { MainControllers.SceneRender, new WindowConfiguration { Title = "Scene" } },
            { MainControllers.Game, new WindowConfiguration { Title = "Game" } },
        };
    }
}