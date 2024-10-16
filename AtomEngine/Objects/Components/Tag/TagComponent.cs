using System.Text.Json.Nodes; 

namespace AtomEngine.Objects.Components.Tag
{
    public class TagComponent : BaseComponent
    {
        private TagType _tags;
        public TagComponent()                       =>  _tags = TagType.None;
        public TagComponent(TagType initialTags)    =>  _tags = initialTags;
        public void AddTag(TagType tag)             =>  _tags |= tag; 
        public void RemoveTag(TagType tag)          =>  _tags &= ~tag; 
        public bool HasTag(TagType tag)             =>  (_tags & tag) == tag; 
        public void ToggleTag(TagType tag)          =>  _tags ^= tag; 
        public TagType GetTags()                    =>  _tags;  
        public void ClearTags()                     =>  _tags = TagType.None;  
        public override string ToString()           =>  _tags.ToString();

        public override void OnDeserialize(JsonObject json)
        {
            if (json.TryGetPropertyValue(nameof(_tags), out var tagsNode))
            {
                string? tagsString = tagsNode?.GetValue<string>();
                if (!string.IsNullOrEmpty(tagsString) &&
                    Enum.TryParse(typeof(TagType), tagsString, out object? result) &&
                    result != null)
                {
                    _tags = (TagType)result;
                }
                else _tags = TagType.None; 
            }
        } 
        public override JsonObject OnSerialize()
        {
            var jsonObj = new JsonObject
            {
                [nameof(_tags)] = _tags.ToString()
            };
            return jsonObj;
        }
    }

    [Flags]
    public enum TagType
    {
        None = 0,
        Player = 1,
        Enemy = 2,
        NPC = 4,
        Camera = 8,
    }
}
