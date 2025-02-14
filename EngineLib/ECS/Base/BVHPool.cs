using System.Numerics;

namespace AtomEngine
{
    public class BVHPool : IDisposable
    {
        private class BVHNode
        {
            public IBoundingVolume Bounds;    
            public BVHNode Left;              
            public BVHNode Right;             
            public Entity? ContainedEntity;   
            public bool IsLeaf => ContainedEntity.HasValue;
        }

        private readonly Dictionary<uint, BVHNode> _leafNodes = new(); 
        private readonly List<uint> _dirtyNodes = new();
        private BVHNode _root;                                         
        private bool _needsRebuild;

        public void AddEntity(Entity entity, IBoundingVolume bounds)
        {
            var node = new BVHNode
            {
                ContainedEntity = entity,
                Bounds = bounds
            };
            _leafNodes.Add(entity.Id, node);
            _needsRebuild = true;
        }

        public void RemoveEntity(Entity entity)
        {
            if (_leafNodes.Remove(entity.Id))
            {
                _needsRebuild = true;
            }
        }

        public void Update(World world)
        {
            // Обновляем границы всех листовых узлов
            foreach (var node in _leafNodes.Values)
            {
                UpdateLeafBounds(node, world);
            }

            // Перестраиваем дерево если нужно
            if (_needsRebuild)
            {
                RebuildTree();
                _needsRebuild = false;
            }
        }

        private void UpdateLeafBounds(BVHNode node, World world)
        {
            if (!node.ContainedEntity.HasValue) return;

            ref var transform = ref world.GetComponent<TransformComponent>(node.ContainedEntity.Value);
            ref var bounding = ref world.GetComponent<BoundingComponent>(node.ContainedEntity.Value);
            node.Bounds = bounding.Transform(transform.GetModelMatrix());
        }
        
        private void RebuildTree()
        {
            if (_leafNodes.Count == 0)
            {
                _root = null;
                return;
            }

            var nodes = new List<BVHNode>(_leafNodes.Values);
            _root = BuildTreeFromNodes(nodes);
        }

        public void MarkNodeDirty(Entity entity)
        {
            if (_leafNodes.ContainsKey(entity.Id))
            {
                _dirtyNodes.Add(entity.Id);
            }
        }

        private BVHNode BuildTreeFromNodes(List<BVHNode> nodes)
        {
            while (nodes.Count > 1)
            {
                // Находим лучшую пару узлов для объединения
                float bestCost = float.MaxValue;
                int bestI = 0;
                int bestJ = 1;

                for (int i = 0; i < nodes.Count; i++)
                {
                    for (int j = i + 1; j < nodes.Count; j++)
                    {
                        float cost = CalculateMergeCost(nodes[i], nodes[j]);
                        if (cost < bestCost)
                        {
                            bestCost = cost;
                            bestI = i;
                            bestJ = j;
                        }
                    }
                }

                // Создаем новый внутренний узел
                var internalNode = new BVHNode
                {
                    Left = nodes[bestI],
                    Right = nodes[bestJ],
                    Bounds = MergeBounds(nodes[bestI].Bounds, nodes[bestJ].Bounds),
                    ContainedEntity = null  // Внутренние узлы не содержат Entity
                };

                // Удаляем использованные узлы и добавляем новый
                nodes.RemoveAt(bestJ);  // Удаляем сначала больший индекс
                nodes.RemoveAt(bestI);
                nodes.Add(internalNode);
            }

            return nodes[0];
        }

        private float CalculateMergeCost(BVHNode a, BVHNode b)
        {
            var merged = MergeBounds(a.Bounds, b.Bounds);
            var size = merged.Max - merged.Min;
            return size.X * size.Y * size.Z;  
        }

        private static IBoundingVolume MergeBounds(in IBoundingVolume a, in IBoundingVolume b)
        {
            return new BoundingBox(
                Vector3.Min(a.Min, b.Min),
                Vector3.Max(a.Max, b.Max)
            );
        }

        public ReadOnlySpan<(Entity, Entity)> GatherCollisionPairs(World world)
        {
            var pairs = new List<(Entity, Entity)>();
            if (_root == null) return pairs.ToArray();

            // Проверяем каждый лист со всем деревом
            foreach (var leaf in _leafNodes.Values)
            {
                CheckCollisionsAgainstTree(leaf, _root, pairs);
            }

            return pairs.ToArray();
        }

        private void CheckCollisionsAgainstTree(BVHNode leaf, BVHNode treeNode, List<(Entity, Entity)> pairs)
        {
            // Если боксы не пересекаются, пропускаем
            if (!leaf.Bounds.Intersects(treeNode.Bounds))
                return;

            if (treeNode.IsLeaf)
            {
                // Избегаем дубликатов и самопересечений
                if (leaf.ContainedEntity.Value.Id < treeNode.ContainedEntity.Value.Id)
                {
                    pairs.Add((leaf.ContainedEntity.Value, treeNode.ContainedEntity.Value));
                }
            }
            else
            {
                // Рекурсивно проверяем дочерние узлы
                CheckCollisionsAgainstTree(leaf, treeNode.Left, pairs);
                CheckCollisionsAgainstTree(leaf, treeNode.Right, pairs);
            }
        }

        public void Dispose()
        {
            _leafNodes.Clear();
            _root = null;
        }
    }
}