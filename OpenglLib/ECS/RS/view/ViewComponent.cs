using AtomEngine;
using EngineLib;
using Silk.NET.Maths;

namespace OpenglLib
{
    [TooltipCategoryComponent(category: ComponentCategory.Render)]
    public partial struct ViewComponent : IComponent
    {
        public Entity Owner { get; set; }

        public Matrix4X4<float> model;
        public Matrix4X4<float> view;
        public Matrix4X4<float> projection;

        public ViewComponent(Entity owner)
        {
            Owner = owner;
        }

        public void MakeClean()
        { }
    }
}
