using EngineLib.Componentns;

namespace EngineLib
{
    public class RenderSystem : System
    {
        public RenderSystem(World world) : base(world) { }

        public override void Update(float deltaTime)
        {
            foreach (Entity entity in World.GetEntitiesWith<MeshComponent, ShaderComponent, TransformComponent>())
            {
                Option<MeshComponent> mb_meshComp = World.GetComponentOrNone<MeshComponent>(entity);
                Option<ShaderComponent> mb_shaderComp = World.GetComponentOrNone<ShaderComponent>(entity);
                Option<TransformComponent> mb_transformComp = World.GetComponentOrNone<TransformComponent>(entity);

                if (mb_meshComp.IsNone() || mb_shaderComp.IsNone() || mb_transformComp.IsNone())
                    continue;

                var mesh = mb_meshComp.Unwrap();
                var shader = mb_shaderComp.Unwrap();
                var transform = mb_transformComp.Unwrap();

                shader.Shader.Use();
                shader.Shader.SetUniform("transform", transform.WorldMatrix);
                mesh.Mesh.Draw(shader.Shader);
            }
        }
    }
}
