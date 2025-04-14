[InterfaceName:IViewRender]
[ComponentName:ViewComponent]
[SystemName:ViewSetterRenderSystem]
[RequiredComponent:MeshComponent]

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

vec4 TransfomToHClip(vec3 position)
{
	return projection * view * model * vec4(position, 1.0);
}