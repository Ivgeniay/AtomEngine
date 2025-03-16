using System;
using Newtonsoft.Json;

namespace Editor
{
    public class EntityReorderEventArgs : EventArgs
    {
        public EntityHierarchyItem Entity { get; }
        public int OldIndex { get; }           
        public int NewIndex { get; }           
        public int OldLocalIndex { get; }      
        public int NewLocalIndex { get; }      
        public uint? NewParentId { get; }
        public uint? OldParentId { get; }

        public EntityReorderEventArgs(
            EntityHierarchyItem entity,
            int oldIndex,
            int newIndex,
            int oldLocalIndex,
            int newLocalIndex,
            uint? newParentId = null,
            uint? oldParentId = null)
        {
            Entity = entity;
            OldIndex = oldIndex;
            NewIndex = newIndex;
            OldLocalIndex = oldLocalIndex;
            NewLocalIndex = newLocalIndex;
            NewParentId = newParentId;
            OldParentId = oldParentId;
        }

        public override string ToString() => JsonConvert.SerializeObject(this);
    }

}