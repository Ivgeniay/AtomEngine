using Avalonia.Controls;
using EngineLib;

namespace Editor
{
    internal class TestShaderComponentView : GLDependableViewBase
    {
        SceneManager sceneManager;
        SceneEntityComponentProvider entityProvider;

        public TestShaderComponentView(PropertyDescriptor descriptor) : base(descriptor) { 
            sceneManager = ServiceHub.Get<SceneManager>();
            entityProvider = SceneManager.EntityCompProvider;
        }

        public override Control GetView()
        {
            EntityInspectorContext context = (EntityInspectorContext)descriptor.Context;
            //context.EntityId;


            return null;
        }
    }
}
