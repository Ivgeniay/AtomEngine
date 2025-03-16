using System.Collections.Generic;

namespace Editor
{
    public class EntityHierarchyItemTree
    {
        public EntityHierarchyItem Root;
        public List<EntityHierarchyItemTree> Children { get; set; }

        public EntityHierarchyItemTree(EntityHierarchyItem root)
        {
            Root = root;
            Children = new List<EntityHierarchyItemTree>();
        }

        public void AddChild(EntityHierarchyItemTree child)
        {
            Children.Add(child);

            var childItem = child.Root;
            childItem.ParentId = Root.Id;
            childItem.Level = Root.Level + 1;

            Root.Children.Add(childItem.Id);
        }

        public List<EntityHierarchyItem> FlattenTree()
        {
            var result = new List<EntityHierarchyItem>();
            result.Add(Root);

            foreach (var child in Children)
            {
                var childRoot = child.Root;
                childRoot.ParentId = Root.Id;
                childRoot.Level = Root.Level + 1;

                child.Root = childRoot;

                result.AddRange(child.FlattenTree());
            }

            return result;
        }
    }

}