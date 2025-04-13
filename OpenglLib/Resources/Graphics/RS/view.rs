[InterfaceName:IViewRender]
[ComponentName:ViewComponent]
[SystemName:ViewSetterRenderSystem]
[RequiredComponent:MeshComponent]
[PlaceTarget:Vertex]
uniform mat4 model;
[PlaceTarget:Vertex]
uniform mat4 view;
[PlaceTarget:Vertex]
uniform mat4 projection;

vec4 TransfomToHClip(vec3 position)
{
	return projection * view * model * vec4(position, 1.0);
}