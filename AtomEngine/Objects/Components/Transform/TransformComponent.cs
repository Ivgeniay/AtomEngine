using System.Text.Json.Nodes; 
using AtomEngine.Reactive; 
using AtomEngine.Math;

namespace AtomEngine
{
    public class TransformComponent : BaseComponent
    {
        public ReactiveProperty<Vector3D> AbsolutePositon { get; private set; } = new ReactiveProperty<Vector3D>();
        public ReactiveProperty<Vector3D> AbsoluteRotation { get; private set; } = new ReactiveProperty<Vector3D>();
        public ReactiveProperty<Vector3D> AbsoluteScale { get; private set; } = new ReactiveProperty<Vector3D>();

        public ReactiveProperty<Vector3D> RelativePositon { get; private set; } = new ReactiveProperty<Vector3D>();
        public ReactiveProperty<Vector3D> RelativeRotation { get; private set; } = new ReactiveProperty<Vector3D>();
        public ReactiveProperty<Vector3D> RelativeScale { get; private set; } = new ReactiveProperty<Vector3D>();


        public override void OnDeserialize(JsonObject json) {
            if (json.TryGetPropertyValue(nameof(AbsolutePositon), out var posNode)) 
                AbsolutePositon.Value = 
                    Vector3D.Parse(posNode.GetValue<string>());

            if (json.TryGetPropertyValue(nameof(AbsoluteRotation), out var rotNode)) 
                AbsoluteRotation.Value = 
                    Vector3D.Parse(rotNode.GetValue<string>());

            if (json.TryGetPropertyValue(nameof(AbsoluteScale), out var scaleNode)) 
                AbsoluteScale.Value = 
                    Vector3D.Parse(scaleNode.GetValue<string>());

            if (json.TryGetPropertyValue(nameof(RelativePositon), out var relPosNode))
                AbsolutePositon.Value = 
                    Vector3D.Parse(relPosNode.GetValue<string>());

            if (json.TryGetPropertyValue(nameof(RelativeRotation), out var relRotNode))
                AbsoluteRotation.Value = 
                    Vector3D.Parse(relRotNode.GetValue<string>());

            if (json.TryGetPropertyValue(nameof(RelativeScale), out var relScaleNode))
                AbsoluteScale.Value = 
                    Vector3D.Parse(relScaleNode.GetValue<string>());
        }

        public override JsonObject OnSerialize()
        {
            JsonObject jsonObject = new JsonObject();

            jsonObject.Add(nameof(AbsolutePositon),     AbsolutePositon.Value.ToString());
            jsonObject.Add(nameof(AbsoluteRotation),    AbsoluteRotation.Value.ToString());
            jsonObject.Add(nameof(AbsoluteScale),       AbsoluteScale.Value.ToString());
            jsonObject.Add(nameof(RelativePositon),     AbsolutePositon.Value.ToString());
            jsonObject.Add(nameof(RelativeRotation),    AbsoluteRotation.Value.ToString());
            jsonObject.Add(nameof(RelativeScale),       AbsoluteScale.Value.ToString());

            return jsonObject;
        }
    }
}
