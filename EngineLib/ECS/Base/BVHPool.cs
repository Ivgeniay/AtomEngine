using System.Numerics;

namespace AtomEngine
{
    public class BVHPool : IDisposable
    {
        private class VertexArrays
        {
            public Vector3[] BoxVertices;
            public Vector3[] SphereVertices;

            public VertexArrays()
            {
                BoxVertices = new Vector3[8];
                SphereVertices = new Vector3[8 * 4];
            }
        }
        private class BVHNode
        {
            public IBoundingVolume OriginalBounds;
            public IBoundingVolume Bounds;
            public BVHNode Left;
            public BVHNode Right;
            public BVHNode Parent;
            public Entity? ContainedEntity;
            public bool IsLeaf => ContainedEntity.HasValue;
            public override string ToString() => $"BVHNode: {ContainedEntity}, Bounds={Bounds.Min} to {Bounds.Max}";
        }

        private BVHNode _root;
        private World _world;
        private Dictionary<Entity, BVHNode> _entityToNode;
        private HashSet<Entity> _dirtyNodes;


        public BVHPool(World world)
        {
            _world = world;
            _entityToNode = new Dictionary<Entity, BVHNode>();
            _dirtyNodes = new HashSet<Entity>();
            _root = null;
        }

        public void AddEntity(Entity entity, IBoundingVolume bounds)
        {
            if (!_world.HasComponent<TransformComponent>(entity))
                return;

            var transform = _world.GetComponent<TransformComponent>(entity);

            DebLogger.Debug($"Adding static entity {entity}");
            DebLogger.Debug($"Initial position: {transform.Position}");
            DebLogger.Debug($"Initial bounds: {bounds.Min} to {bounds.Max}");
            DebLogger.Debug($"Initial scale: {transform.Scale}");

            var newNode = new BVHNode
            {
                OriginalBounds = bounds,
                Bounds = bounds.Transform(transform.GetModelMatrix()),
                ContainedEntity = entity,
                Left = null,
                Right = null,
                Parent = null
            };

            _entityToNode[entity] = newNode;
            if (_root == null)
            {
                _root = newNode;
                return;
            }
            InsertNode(newNode);
        }

        public void RemoveEntity(Entity entity)
        {
            if (!_entityToNode.TryGetValue(entity, out var nodeToRemove))
                return;

            if (nodeToRemove == _root && nodeToRemove.IsLeaf)
            {
                _root = null;
                _entityToNode.Remove(entity);
                return;
            }

            var parent = nodeToRemove.Parent;
            var sibling = parent.Left == nodeToRemove ? parent.Right : parent.Left;

            sibling.Parent = parent.Parent;

            if (parent == _root)
            {
                _root = sibling;
            }
            else
            {
                if (parent.Parent.Left == parent)
                    parent.Parent.Left = sibling;
                else
                    parent.Parent.Right = sibling;

                UpdateParentBounds(sibling);
            }

            _entityToNode.Remove(entity);
            if (_dirtyNodes.Contains(entity))
                _dirtyNodes.Remove(entity);
        }

        private void InsertNode(BVHNode newNode)
        {
            BVHNode current = _root;

            while (!current.IsLeaf)
            {
                current.Bounds = CombineBounds(current.Bounds, newNode.Bounds);

                float leftCost = CalculateInsertionCost(current.Left, newNode);
                float rightCost = CalculateInsertionCost(current.Right, newNode);

                current = leftCost < rightCost ? current.Left : current.Right;
            }

            var internalNode = new BVHNode
            {
                Bounds = CombineBounds(current.Bounds, newNode.Bounds),
                ContainedEntity = null,
                Parent = current.Parent
            };

            if (current.Parent != null)
            {
                if (current.Parent.Left == current)
                    current.Parent.Left = internalNode;
                else
                    current.Parent.Right = internalNode;
            }
            else
            {
                _root = internalNode;
            }

            internalNode.Left = current;
            internalNode.Right = newNode;
            current.Parent = internalNode;
            newNode.Parent = internalNode;
        }
        private readonly object _dirtyNodesLock = new object();
        public void MarkNodeDirty(Entity entity)
        {
            if (!_entityToNode.ContainsKey(entity))
                return;

            lock (_dirtyNodesLock)
            {
                if (!_dirtyNodes.Contains(entity))
                {
                    _dirtyNodes.Add(entity);
                }
            }
        }

        public void UpdateDirtyNodes()
        {
            foreach (var entity in _dirtyNodes)
            {
                if (_entityToNode.TryGetValue(entity, out var node))
                {
                    var transform = _world.GetComponent<TransformComponent>(entity);
                    node.Bounds = node.OriginalBounds.Transform(transform.GetModelMatrix());
                    UpdateParentBounds(node);
                }
            }
            _dirtyNodes.Clear();
        }
        public List<Entity> GetPotentialCollisions(IBoundingVolume queryVolume)
        {
            List<Entity> potentialCollisions = new List<Entity>();

            if (_root == null)
                return potentialCollisions;

            Stack<BVHNode> nodesToCheck = new Stack<BVHNode>();
            nodesToCheck.Push(_root);

            while (nodesToCheck.Count > 0)
            {
                BVHNode current = nodesToCheck.Pop();

                if (!queryVolume.Intersects(current.Bounds))
                {
                    continue;
                }

                if (current.IsLeaf)
                {
                    potentialCollisions.Add(current.ContainedEntity.Value);
                    continue;
                }

                if (current.Right != null)
                    nodesToCheck.Push(current.Right);
                if (current.Left != null)
                    nodesToCheck.Push(current.Left);
            }

            return potentialCollisions;
        }

        private void UpdateParentBounds(BVHNode node)
        {
            var current = node.Parent;
            while (current != null)
            {
                current.Bounds = CombineBounds(
                    current.Left.Bounds,
                    current.Right.Bounds
                );
                current = current.Parent;
            }
        }

        private float CalculateInsertionCost(BVHNode existingNode, BVHNode newNode)
        {
            var combinedBounds = CombineBounds(existingNode.Bounds, newNode.Bounds);
            return CalculateVolume(combinedBounds) - CalculateVolume(existingNode.Bounds);
        }

        private IBoundingVolume CombineBounds(IBoundingVolume a, IBoundingVolume b)
        {
            Vector3 min = Vector3.Min(a.Min, b.Min);
            Vector3 max = Vector3.Max(a.Max, b.Max);
            return new BoundingBox(min, max);
        }

        private float CalculateVolume(IBoundingVolume bounds)
        {
            Vector3 dimensions = bounds.Max - bounds.Min;
            return dimensions.X * dimensions.Y * dimensions.Z;
        }

        public void Dispose()
        {
            _entityToNode.Clear();
            _root = null;
        }
    }




    //    public class BVHPool : IDisposable {
    //    private class VertexArrays
    //    {
    //        public Vector3[] BoxVertices;
    //        public Vector3[] SphereVertices;

    //        public VertexArrays()
    //        {
    //            BoxVertices = new Vector3[8];
    //            SphereVertices = new Vector3[8 * 4];
    //        }
    //    }
    //    private class BVHNode
    //    {
    //        public IBoundingVolume Bounds;
    //        public BVHNode Left;
    //        public BVHNode Right;
    //        public Entity? ContainedEntity;
    //        public bool IsLeaf => ContainedEntity.HasValue;

    //        public override string ToString()
    //        {
    //            return $"BVHNode: {ContainedEntity}, Bounds={Bounds.Min} to {Bounds.Max}";
    //        }
    //    }

    //    private readonly ThreadLocal<VertexArrays> _vertexArrays = new(() => new VertexArrays());
    //    private readonly List<(Entity, Entity)> _collisionPairs = new();
    //    private readonly List<CollisionManifold> _activeManifolds = new();
    //    private readonly Dictionary<uint, BVHNode> _leafNodes = new();
    //    private readonly HashSet<uint> _processingNodes = new();
    //    private readonly HashSet<uint> _dirtyNodes = new();
    //    private BVHNode _root;
    //    private bool _needsRebuild;
    //    private readonly World _world;

    //    public BVHPool(World world)
    //    {
    //        _world = world;
    //    }

    //    public void AddEntity(Entity entity, IBoundingVolume bounds)
    //    {
    //        ref var transform = ref _world.GetComponent<TransformComponent>(entity);

    //        // Трансформируем границы в мировое пространство
    //        var worldBounds = bounds.Transform(transform.GetModelMatrix());

    //        //DebLogger.Debug($"Entity {entity} position: {transform.Position}");
    //        //DebLogger.Debug($"Local bounds: Min={bounds.Min}, Max={bounds.Max}");
    //        //DebLogger.Debug($"World bounds: Min={worldBounds.Min}, Max={worldBounds.Max}");

    //        var node = new BVHNode
    //        {
    //            ContainedEntity = entity,
    //            Bounds = worldBounds
    //        };

    //        //DebLogger.Debug($"Adding entity {entity} to BVH");
    //        //DebLogger.Debug($"Local bounds: Min={bounds.Min}, Max={bounds.Max}");
    //        //DebLogger.Debug($"World bounds: Min={worldBounds.Min}, Max={worldBounds.Max}");

    //        _leafNodes.Add(entity.Id, node);
    //        _needsRebuild = true;
    //    }

    //    public void RemoveEntity(Entity entity)
    //    {
    //        if (_leafNodes.Remove(entity.Id))
    //        {
    //            _needsRebuild = true;
    //        }
    //    }

    //    public ReadOnlySpan<CollisionManifold> Update(World world)
    //    {
    //        _activeManifolds.Clear();

    //        //DebLogger.Debug("Starting BVHPool update");

    //        // Обновляем bounds и дерево
    //        if (_needsRebuild)
    //        {
    //            //DebLogger.Debug("Rebuilding tree");
    //            RebuildTree();
    //            _needsRebuild = false;
    //        }
    //        else
    //        {
    //            //DebLogger.Debug("Updating dirty nodes");
    //            UpdateDirtyNodes(world);
    //        }

    //        //DebLogger.Debug("Gathering collision pairs");
    //        var potentialPairs = GatherCollisionPairs(world);
    //        //DebLogger.Debug($"Found {potentialPairs.Length} potential pairs");

    //        foreach (var (entityA, entityB) in potentialPairs)
    //        {
    //            //DebLogger.Debug($"Processing potential collision between {entityA} and {entityB}");
    //            ref var boundingA = ref world.GetComponent<BoundingComponent>(entityA);
    //            ref var boundingB = ref world.GetComponent<BoundingComponent>(entityB);
    //            ref var transformA = ref world.GetComponent<TransformComponent>(entityA);
    //            ref var transformB = ref world.GetComponent<TransformComponent>(entityB);

    //            var manifold = new CollisionManifold(entityA, entityB);

    //            if (CheckDetailedCollision(
    //                boundingA.BoundingVolume, transformA.GetModelMatrix(),
    //                boundingB.BoundingVolume, transformB.GetModelMatrix(),
    //                ref manifold))
    //            {
    //                //DebLogger.Debug($"Confirmed collision between {entityA} and {entityB}");
    //                _activeManifolds.Add(manifold);
    //            }
    //        }

    //        return _activeManifolds.ToArray();
    //    }

    //    public void MarkNodeDirty(Entity entity)
    //    {
    //        if (_leafNodes.ContainsKey(entity.Id))
    //        {
    //            if (!_dirtyNodes.Contains(entity.Id))
    //                _dirtyNodes.Add(entity.Id);
    //        }
    //    }

    //    private void UpdateDirtyNodes(World world)
    //    {
    //        if (_dirtyNodes.Count == 0) return;

    //        // Копируем dirty nodes во временный set
    //        _processingNodes.Clear();
    //        foreach (var nodeId in _dirtyNodes)
    //        {
    //            _processingNodes.Add(nodeId);
    //        }
    //        _dirtyNodes.Clear();

    //        foreach (var nodeId in _processingNodes)
    //        {
    //            if (_leafNodes.TryGetValue(nodeId, out var node))
    //            {
    //                UpdateLeafBounds(node, world);
    //            }
    //        }
    //    }

    //    private void UpdateLeafBounds(BVHNode node, World world)
    //    {
    //        if (!node.ContainedEntity.HasValue) return;

    //        ref var transform = ref world.GetComponent<TransformComponent>(node.ContainedEntity.Value);
    //        ref var bounding = ref world.GetComponent<BoundingComponent>(node.ContainedEntity.Value);

    //        var oldBounds = node.Bounds;
    //        node.Bounds = bounding.BoundingVolume.Transform(transform.GetModelMatrix());

    //        //DebLogger.Debug($"Updated bounds for entity {node.ContainedEntity.Value}:");
    //        //DebLogger.Debug($"Old: Min={oldBounds.Min}, Max={oldBounds.Max}");
    //        //DebLogger.Debug($"New: Min={node.Bounds.Min}, Max={node.Bounds.Max}");
    //    }

    //    private void RebuildTree()
    //    {
    //        if (_leafNodes.Count == 0)
    //        {
    //            _root = null;
    //            return;
    //        }

    //        var nodes = new List<BVHNode>(_leafNodes.Values);
    //        _root = BuildTreeFromNodes(nodes);

    //        //DebLogger.Debug("Final BVH structure:");
    //        LogBVHStructure(_root);
    //    }

    //    private void LogBVHStructure(BVHNode node, string prefix = "")
    //    {
    //        if (node == null) return;

    //        if (node.ContainedEntity.HasValue)
    //        {
    //            DebLogger.Debug($"{prefix}Leaf: Entity={node.ContainedEntity.Value}, Bounds: Min={node.Bounds.Min}, Max={node.Bounds.Max}");
    //        }
    //        else
    //        {
    //            DebLogger.Debug($"{prefix}Internal: Bounds: Min={node.Bounds.Min}, Max={node.Bounds.Max}");
    //            LogBVHStructure(node.Left, prefix + "  L-");
    //            LogBVHStructure(node.Right, prefix + "  R-");
    //        }
    //    }

    //    private BVHNode BuildTreeFromNodes(List<BVHNode> nodes)
    //    {
    //        if (nodes.Count == 0) return null;
    //        if (nodes.Count == 1) return nodes[0];

    //        // 1. Выбираем ось разделения (x, y или z)
    //        int axis = ChooseSplitAxis(nodes);

    //        // 2. Сортируем узлы по центру их bounds вдоль выбранной оси
    //        nodes.Sort((a, b) =>
    //        {
    //            Vector3 centerA = (a.Bounds.Max + a.Bounds.Min) * 0.5f;
    //            Vector3 centerB = (b.Bounds.Max + b.Bounds.Min) * 0.5f;
    //            return axis switch
    //            {
    //                0 => centerA.X.CompareTo(centerB.X),
    //                1 => centerA.Y.CompareTo(centerB.Y),
    //                _ => centerA.Z.CompareTo(centerB.Z)
    //            };
    //        });

    //        // 3. Разделяем на две группы
    //        int mid = nodes.Count / 2;
    //        var leftNodes = nodes.GetRange(0, mid);
    //        var rightNodes = nodes.GetRange(mid, nodes.Count - mid);

    //        return new BVHNode
    //        {
    //            Left = BuildTreeFromNodes(leftNodes),
    //            Right = BuildTreeFromNodes(rightNodes),
    //            Bounds = MergeBounds(leftNodes[0].Bounds, rightNodes[0].Bounds),
    //            ContainedEntity = null
    //        };
    //    }

    //    private int ChooseSplitAxis(List<BVHNode> nodes)
    //    {
    //        // Находим размеры всего множества объектов
    //        Vector3 min = new(float.MaxValue);
    //        Vector3 max = new(float.MinValue);

    //        foreach (var node in nodes)
    //        {
    //            min = Vector3.Min(min, node.Bounds.Min);
    //            max = Vector3.Max(max, node.Bounds.Max);
    //        }

    //        Vector3 extent = max - min;

    //        // Выбираем самую длинную ось
    //        if (extent.X > extent.Y && extent.X > extent.Z)
    //            return 0;
    //        if (extent.Y > extent.Z)
    //            return 1;
    //        return 2;
    //    }

    //    private bool CheckDetailedCollision(
    //            IBoundingVolume volumeA, Matrix4x4 transformA,
    //            IBoundingVolume volumeB, Matrix4x4 transformB,
    //            ref CollisionManifold manifold)
    //    {
    //        // Сначала преобразуем объемы в мировые координаты
    //        var worldVolumeA = volumeA.Transform(transformA);
    //        var worldVolumeB = volumeB.Transform(transformB);

    //        // Получаем вершины из объемов
    //        Vector3[] verticesA = GetVerticesFromVolume(worldVolumeA);
    //        Vector3[] verticesB = GetVerticesFromVolume(worldVolumeB);

    //        // Используем GJK для проверки пересечения
    //        if (!GJKAlgorithm.Intersect(verticesA, verticesB))
    //            return false;

    //        // Если GJK обнаружил пересечение, генерируем точки контакта
    //        GenerateContactPoints(worldVolumeA, worldVolumeB, ref manifold);

    //        return true;
    //    }

    //    private Vector3[] GetVerticesFromVolume(IBoundingVolume volume)
    //    {
    //        var arrays = _vertexArrays.Value;

    //        if (volume is BoundingBox box)
    //        {
    //            var vertices = arrays.BoxVertices;

    //            // Front face (Y-min)
    //            vertices[0] = new Vector3(box.Min.X, box.Min.Y, box.Min.Z);
    //            vertices[1] = new Vector3(box.Max.X, box.Min.Y, box.Min.Z);
    //            vertices[2] = new Vector3(box.Min.X, box.Min.Y, box.Max.Z);
    //            vertices[3] = new Vector3(box.Max.X, box.Min.Y, box.Max.Z);

    //            // Back face (Y-max)
    //            vertices[4] = new Vector3(box.Min.X, box.Max.Y, box.Min.Z);
    //            vertices[5] = new Vector3(box.Max.X, box.Max.Y, box.Min.Z);
    //            vertices[6] = new Vector3(box.Min.X, box.Max.Y, box.Max.Z);
    //            vertices[7] = new Vector3(box.Max.X, box.Max.Y, box.Max.Z);

    //            return vertices;
    //        }

    //        if (volume is BoundingSphere sphere)
    //        {
    //            var vertices = arrays.SphereVertices;
    //            int vertexCount = 0;
    //            const int segments = 8;

    //            for (int i = 0; i < segments; i++)
    //            {
    //                float theta = (2 * MathF.PI * i) / segments;
    //                float cosTheta = MathF.Cos(theta);
    //                float sinTheta = MathF.Sin(theta);

    //                for (int j = 0; j < segments / 2; j++)
    //                {
    //                    float phi = (MathF.PI * j) / (segments / 2);
    //                    float sinPhi = MathF.Sin(phi);
    //                    float cosPhi = MathF.Cos(phi);

    //                    vertices[vertexCount++] = sphere.Position + new Vector3(
    //                        sphere.Radius * sinPhi * cosTheta,
    //                        sphere.Radius * sinPhi * sinTheta,
    //                        sphere.Radius * cosPhi
    //                    );
    //                }
    //            }

    //            return vertices;
    //        }

    //        throw new ArgumentException($"Unsupported volume type: {volume.GetType()}");
    //    }

    //    private void GenerateContactPoints(IBoundingVolume volumeA, IBoundingVolume volumeB, ref CollisionManifold manifold)
    //    {
    //        if (volumeA is BoundingBox boxA && volumeB is BoundingBox boxB)
    //        {
    //            GenerateBoxBoxContacts(boxA, boxB, ref manifold);
    //        }
    //        else if (volumeA is BoundingSphere sphereA && volumeB is BoundingSphere sphereB)
    //        {
    //            GenerateSphereSphereContacts(sphereA, sphereB, ref manifold);
    //        }
    //        else if (volumeA is BoundingBox && volumeB is BoundingSphere)
    //        {
    //            GenerateBoxSphereContacts((BoundingBox)volumeA, (BoundingSphere)volumeB, ref manifold, false);
    //        }
    //        else if (volumeA is BoundingSphere && volumeB is BoundingBox)
    //        {
    //            GenerateBoxSphereContacts((BoundingBox)volumeB, (BoundingSphere)volumeA, ref manifold, true);
    //        }
    //    }

    //    private void GenerateBoxBoxContacts(BoundingBox boxA, BoundingBox boxB, ref CollisionManifold manifold)
    //    {
    //        // Находим ось с минимальным перекрытием
    //        var overlap = new Vector3(
    //            MathF.Min(boxA.Max.X, boxB.Max.X) - MathF.Max(boxA.Min.X, boxB.Min.X),
    //            MathF.Min(boxA.Max.Y, boxB.Max.Y) - MathF.Max(boxA.Min.Y, boxB.Min.Y),
    //            MathF.Min(boxA.Max.Z, boxB.Max.Z) - MathF.Max(boxA.Min.Z, boxB.Min.Z)
    //        );

    //        // Определяем ось с минимальным перекрытием
    //        Vector3 normal;
    //        float penetration;
    //        if (overlap.X < overlap.Y && overlap.X < overlap.Z)
    //        {
    //            penetration = overlap.X;
    //            normal = new Vector3(1, 0, 0) * Math.Sign(boxB.GetCenter().X - boxA.GetCenter().X);
    //        }
    //        else if (overlap.Y < overlap.Z)
    //        {
    //            penetration = overlap.Y;
    //            normal = new Vector3(0, 1, 0) * Math.Sign(boxB.GetCenter().Y - boxA.GetCenter().Y);
    //        }
    //        else
    //        {
    //            penetration = overlap.Z;
    //            normal = new Vector3(0, 0, 1) * Math.Sign(boxB.GetCenter().Z - boxA.GetCenter().Z);
    //        }

    //        // Находим точки контакта на гранях, перпендикулярных нормали
    //        var contacts = GetContactPointsOnFace(boxA, boxB, normal);
    //        foreach (var contact in contacts)
    //        {
    //            manifold.TryAddContact(contact, normal, penetration);
    //        }
    //    }

    //    private void GenerateSphereSphereContacts(BoundingSphere sphereA, BoundingSphere sphereB, ref CollisionManifold manifold)
    //    {
    //        var direction = sphereB.Position - sphereA.Position;
    //        var distance = direction.Length();

    //        if (distance < float.Epsilon)
    //        {
    //            // Сферы в одной точке
    //            manifold.TryAddContact(
    //                sphereA.Position,
    //                Vector3.UnitY,
    //                sphereA.Radius + sphereB.Radius
    //            );
    //            return;
    //        }

    //        var normal = direction / distance;
    //        var penetration = sphereA.Radius + sphereB.Radius - distance;
    //        var position = sphereA.Position + normal * sphereA.Radius;

    //        manifold.TryAddContact(position, normal, penetration);
    //    }

    //    private void GenerateBoxSphereContacts(BoundingBox box, BoundingSphere sphere, ref CollisionManifold manifold, bool flip)
    //    {
    //        // Находим ближайшую точку на боксе к центру сферы
    //        var closestPoint = new Vector3(
    //            MathF.Max(box.Min.X, MathF.Min(sphere.Position.X, box.Max.X)),
    //            MathF.Max(box.Min.Y, MathF.Min(sphere.Position.Y, box.Max.Y)),
    //            MathF.Max(box.Min.Z, MathF.Min(sphere.Position.Z, box.Max.Z))
    //        );

    //        var normal = sphere.Position - closestPoint;
    //        var distance = normal.Length();

    //        if (distance < float.Epsilon)
    //        {
    //            // Центр сферы внутри бокса
    //            var faceNormal = DetermineBoxFaceNormal(box, sphere.Position);
    //            manifold.TryAddContact(
    //                sphere.Position,
    //                flip ? -faceNormal : faceNormal,
    //                sphere.Radius
    //            );
    //            return;
    //        }

    //        normal /= distance;
    //        var penetration = sphere.Radius - distance;

    //        if (flip)
    //            normal = -normal;

    //        manifold.TryAddContact(closestPoint, normal, penetration);
    //    }

    //    private Vector3 DetermineBoxFaceNormal(BoundingBox box, Vector3 point)
    //    {
    //        var center = box.GetCenter();
    //        var d = point - center;
    //        var extents = box.GetExtents();

    //        float maxComponent = 0;
    //        Vector3 normal = Vector3.Zero;

    //        if (MathF.Abs(d.X) / extents.X > maxComponent)
    //        {
    //            maxComponent = MathF.Abs(d.X) / extents.X;
    //            normal = new Vector3(Math.Sign(d.X), 0, 0);
    //        }
    //        if (MathF.Abs(d.Y) / extents.Y > maxComponent)
    //        {
    //            maxComponent = MathF.Abs(d.Y) / extents.Y;
    //            normal = new Vector3(0, Math.Sign(d.Y), 0);
    //        }
    //        if (MathF.Abs(d.Z) / extents.Z > maxComponent)
    //        {
    //            normal = new Vector3(0, 0, Math.Sign(d.Z));
    //        }

    //        return normal;
    //    }

    //    private float CalculateMergeCost(BVHNode a, BVHNode b)
    //    {
    //        var merged = MergeBounds(a.Bounds, b.Bounds);
    //        var sizeA = a.Bounds.Max - a.Bounds.Min;
    //        var sizeB = b.Bounds.Max - b.Bounds.Min;
    //        var sizeMerged = merged.Max - merged.Min;

    //        // Считаем объем
    //        float volumeA = sizeA.X * sizeA.Y * sizeA.Z;
    //        float volumeB = sizeB.X * sizeB.Y * sizeB.Z;
    //        float volumeMerged = sizeMerged.X * sizeMerged.Y * sizeMerged.Z;

    //        // Стоимость = увеличение объема
    //        return volumeMerged - (volumeA + volumeB);
    //    }

    //    private static IBoundingVolume MergeBounds(in IBoundingVolume a, in IBoundingVolume b)
    //    {
    //        return new BoundingBox(
    //            Vector3.Min(a.Min, b.Min),
    //            Vector3.Max(a.Max, b.Max)
    //        );
    //    }

    //    private void GatherCollisions(BVHNode nodeA, BVHNode nodeB, List<(Entity, Entity)> pairs)
    //    {
    //        // Базовая проверка пересечения bounds
    //        if (!nodeA.Bounds.Intersects(nodeB.Bounds))
    //            return;

    //        // Если оба узла - листья
    //        if (nodeA.IsLeaf && nodeB.IsLeaf)
    //        {
    //            if (nodeA.ContainedEntity == nodeB.ContainedEntity)
    //                return;

    //            bool hasRigidbodyA = _world.HasComponent<RigidbodyComponent>(nodeA.ContainedEntity.Value);
    //            bool hasRigidbodyB = _world.HasComponent<RigidbodyComponent>(nodeB.ContainedEntity.Value);

    //            if (hasRigidbodyA && hasRigidbodyB)
    //            {
    //                ref var rbA = ref _world.GetComponent<RigidbodyComponent>(nodeA.ContainedEntity.Value);
    //                ref var rbB = ref _world.GetComponent<RigidbodyComponent>(nodeB.ContainedEntity.Value);

    //                if (rbA.BodyType == BodyType.Dynamic || rbB.BodyType == BodyType.Dynamic)
    //                {
    //                    pairs.Add((nodeA.ContainedEntity.Value, nodeB.ContainedEntity.Value));
    //                }
    //            }
    //            return;
    //        }

    //        // Определяем, какой узел разделить
    //        if (!nodeA.IsLeaf && (nodeB.IsLeaf || (nodeA.Bounds.Max - nodeA.Bounds.Min).LengthSquared() >
    //                                             (nodeB.Bounds.Max - nodeB.Bounds.Min).LengthSquared()))
    //        {
    //            GatherCollisions(nodeA.Left, nodeB, pairs);
    //            GatherCollisions(nodeA.Right, nodeB, pairs);
    //        }
    //        else
    //        {
    //            GatherCollisions(nodeA, nodeB.Left, pairs);
    //            GatherCollisions(nodeA, nodeB.Right, pairs);
    //        }
    //    }

    //    public ReadOnlySpan<(Entity, Entity)> GatherCollisionPairs(World world)
    //    {
    //        _collisionPairs.Clear();
    //        if (_root == null) return _collisionPairs.ToArray();

    //        // Получаем все узлы-листья
    //        var leafNodes = _leafNodes.Values.ToList();

    //        // Проверяем каждую возможную пару листьев
    //        for (int i = 0; i < leafNodes.Count; i++)
    //        {
    //            for (int j = i + 1; j < leafNodes.Count; j++)
    //            {
    //                var nodeA = leafNodes[i];
    //                var nodeB = leafNodes[j];

    //                // Проверяем пересечение bounds
    //                if (!nodeA.Bounds.Intersects(nodeB.Bounds))
    //                    continue;

    //                Entity entityA = nodeA.ContainedEntity.Value;
    //                Entity entityB = nodeB.ContainedEntity.Value;

    //                //DebLogger.Debug($"Checking leaf pair: {entityA} and {entityB}");

    //                bool hasRigidbodyA = _world.HasComponent<RigidbodyComponent>(entityA);
    //                bool hasRigidbodyB = _world.HasComponent<RigidbodyComponent>(entityB);

    //                //DebLogger.Debug($"RigidBody check: A={hasRigidbodyA}, B={hasRigidbodyB}");

    //                // Если оба имеют RigidbodyComponent
    //                if (hasRigidbodyA && hasRigidbodyB)
    //                {
    //                    ref var rbA = ref _world.GetComponent<RigidbodyComponent>(entityA);
    //                    ref var rbB = ref _world.GetComponent<RigidbodyComponent>(entityB);

    //                    if (rbA.BodyType == BodyType.Dynamic || rbB.BodyType == BodyType.Dynamic)
    //                    {
    //                        //DebLogger.Debug($"Found collision between rigidbodies: {entityA} (Type: {rbA.BodyType}) and {entityB} (Type: {rbB.BodyType})");
    //                        _collisionPairs.Add((entityA, entityB));
    //                    }
    //                }
    //                // Один имеет RigidbodyComponent, другой нет
    //                else if (hasRigidbodyA && !hasRigidbodyB)
    //                {
    //                    ref var rbA = ref _world.GetComponent<RigidbodyComponent>(entityA);
    //                    if (rbA.BodyType == BodyType.Dynamic)
    //                    {
    //                        //DebLogger.Debug($"Found collision between dynamic {entityA} and static {entityB}");
    //                        _collisionPairs.Add((entityA, entityB));
    //                    }
    //                }
    //                else if (!hasRigidbodyA && hasRigidbodyB)
    //                {
    //                    ref var rbB = ref _world.GetComponent<RigidbodyComponent>(entityB);
    //                    if (rbB.BodyType == BodyType.Dynamic)
    //                    {
    //                        //DebLogger.Debug($"Found collision between static {entityA} and dynamic {entityB}");
    //                        _collisionPairs.Add((entityA, entityB));
    //                    }
    //                }
    //            }
    //        }

    //        return _collisionPairs.ToArray();
    //    }

    //    private void CheckCollisionsAgainstTree(BVHNode leaf, BVHNode treeNode, List<(Entity, Entity)> pairs)
    //    {
    //        // Пропускаем, если bounds не пересекаются
    //        if (!leaf.Bounds.Intersects(treeNode.Bounds))
    //            return;

    //        if (treeNode.IsLeaf)
    //        {
    //            // Пропускаем тот же самый объект
    //            if (leaf.ContainedEntity.Value == treeNode.ContainedEntity.Value)
    //                return;

    //            Entity entityA = leaf.ContainedEntity.Value;
    //            Entity entityB = treeNode.ContainedEntity.Value;

    //            //DebLogger.Debug($"Checking collision pair: {entityA} and {entityB}");

    //            // Проверяем порядок ID только если решили добавить пару
    //            if (entityA.Id > entityB.Id)
    //            {
    //                //DebLogger.Debug($"Swapping entities because {entityA.Id} > {entityB.Id}");
    //                (entityA, entityB) = (entityB, entityA);
    //            }

    //            bool hasRigidbodyA = _world.HasComponent<RigidbodyComponent>(entityA);
    //            bool hasRigidbodyB = _world.HasComponent<RigidbodyComponent>(entityB);

    //            //DebLogger.Debug($"RigidBody check: A={hasRigidbodyA}, B={hasRigidbodyB}");

    //            // Если оба имеют RigidbodyComponent, проверяем их типы
    //            if (hasRigidbodyA && hasRigidbodyB)
    //            {
    //                //DebLogger.Debug("Both entities have RigidBody!");
    //                ref var rbA = ref _world.GetComponent<RigidbodyComponent>(entityA);
    //                ref var rbB = ref _world.GetComponent<RigidbodyComponent>(entityB);

    //                // Добавляем пару, если хотя бы один из них динамический
    //                if (rbA.BodyType == BodyType.Dynamic || rbB.BodyType == BodyType.Dynamic)
    //                {
    //                    //DebLogger.Debug($"Found collision between rigidbodies: {entityA} (Type: {rbA.BodyType}) and {entityB} (Type: {rbB.BodyType})");
    //                    pairs.Add((entityA, entityB));
    //                }
    //                else
    //                {
    //                    //DebLogger.Debug($"Skipping because both bodies are static: {rbA.BodyType} and {rbB.BodyType}");
    //                }
    //            }
    //            else
    //            {
    //                //if (hasRigidbodyA)
    //                    //DebLogger.Debug($"Only entityA {entityA} has RigidBody");
    //                //if (hasRigidbodyB)
    //                    //DebLogger.Debug($"Only entityB {entityB} has RigidBody");

    //                // Один имеет RigidbodyComponent, другой нет
    //                if (hasRigidbodyA && !hasRigidbodyB)
    //                {
    //                    ref var rbA = ref _world.GetComponent<RigidbodyComponent>(entityA);
    //                    if (rbA.BodyType == BodyType.Dynamic)
    //                    {
    //                        //DebLogger.Debug($"Found collision between dynamic {entityA} and static {entityB}");
    //                        pairs.Add((entityA, entityB));
    //                    }
    //                }
    //                else if (!hasRigidbodyA && hasRigidbodyB)
    //                {
    //                    ref var rbB = ref _world.GetComponent<RigidbodyComponent>(entityB);
    //                    if (rbB.BodyType == BodyType.Dynamic)
    //                    {
    //                        //DebLogger.Debug($"Found collision between static {entityA} and dynamic {entityB}");
    //                        pairs.Add((entityA, entityB));
    //                    }
    //                }
    //            }
    //        }
    //        else
    //        {
    //            if (treeNode.Left != null)
    //                CheckCollisionsAgainstTree(leaf, treeNode.Left, pairs);
    //            if (treeNode.Right != null)
    //                CheckCollisionsAgainstTree(leaf, treeNode.Right, pairs);
    //        }
    //    }


    //    //private void CheckCollisionsAgainstTree(BVHNode leaf, BVHNode treeNode, List<(Entity, Entity)> pairs)
    //    //{
    //    //    //DebLogger.Debug($"Checking collision: Leaf entity {leaf.ContainedEntity} against tree node {treeNode.ContainedEntity}");
    //    //    //DebLogger.Debug($"Leaf bounds: Min={leaf.Bounds.Min}, Max={leaf.Bounds.Max}");
    //    //    //DebLogger.Debug($"Tree bounds: Min={treeNode.Bounds.Min}, Max={treeNode.Bounds.Max}");

    //    //    // Проверяем пересечение
    //    //    bool intersects = leaf.Bounds.Intersects(treeNode.Bounds);
    //    //    //DebLogger.Debug($"Bounds intersect: {intersects}");

    //    //    if (!intersects)
    //    //        return;

    //    //    if (treeNode.IsLeaf)
    //    //    {
    //    //        // Проверяем, что это не тот же самый узел
    //    //        if (leaf.ContainedEntity.Value == treeNode.ContainedEntity.Value)
    //    //        {
    //    //            //DebLogger.Debug("Same entity, skipping");
    //    //            return;
    //    //        }

    //    //        // Проверяем порядок ID для избежания дубликатов
    //    //        if (leaf.ContainedEntity.Value.Id < treeNode.ContainedEntity.Value.Id)
    //    //        {
    //    //            //DebLogger.Debug($"Adding collision pair: {leaf.ContainedEntity.Value.Id} - {treeNode.ContainedEntity.Value.Id}");
    //    //            pairs.Add((leaf.ContainedEntity.Value, treeNode.ContainedEntity.Value));
    //    //        }
    //    //    }
    //    //    else
    //    //    {
    //    //        // Проверяем дочерние узлы, если они есть
    //    //        if (treeNode.Left != null)
    //    //        {
    //    //            //DebLogger.Debug("Checking left child");
    //    //            CheckCollisionsAgainstTree(leaf, treeNode.Left, pairs);
    //    //        }
    //    //        if (treeNode.Right != null)
    //    //        {
    //    //            //DebLogger.Debug("Checking right child");
    //    //            CheckCollisionsAgainstTree(leaf, treeNode.Right, pairs);
    //    //        }
    //    //    }
    //    //}

    //    private Vector3[] GetContactPointsOnFace(BoundingBox boxA, BoundingBox boxB, Vector3 normal)
    //    {
    //        // Определяем какие грани взаимодействуют
    //        var faceA = GetBoxFace(boxA, -normal);  // грань boxA
    //        var faceB = GetBoxFace(boxB, normal);   // грань boxB

    //        // Находим пересечение граней
    //        var contacts = new List<Vector3>();

    //        // Проецируем грани на плоскость, перпендикулярную нормали
    //        var projectedA = ProjectVerticesOnPlane(faceA, normal);
    //        var projectedB = ProjectVerticesOnPlane(faceB, normal);

    //        // Находим пересечение проекций
    //        var intersection = FindIntersectionPoints(projectedA, projectedB);
    //        if (intersection.Length == 0)
    //        {
    //            // Если пересечения нет, берем ближайшие точки
    //            contacts.Add((faceA[0] + faceB[0]) * 0.5f);
    //        }
    //        else
    //        {
    //            contacts.AddRange(intersection);
    //        }

    //        return contacts.ToArray();
    //    }

    //    private Vector3[] GetBoxFace(BoundingBox box, Vector3 normal)
    //    {
    //        var vertices = new Vector3[4];
    //        var center = box.GetCenter();
    //        var extents = box.GetExtents();

    //        // Определяем, какую грань выбрать на основе нормали
    //        if (MathF.Abs(normal.X) > MathF.Abs(normal.Y) && MathF.Abs(normal.X) > MathF.Abs(normal.Z))
    //        {
    //            // X-грань
    //            float x = normal.X > 0 ? box.Max.X : box.Min.X;
    //            vertices[0] = new Vector3(x, box.Min.Y, box.Min.Z);
    //            vertices[1] = new Vector3(x, box.Max.Y, box.Min.Z);
    //            vertices[2] = new Vector3(x, box.Max.Y, box.Max.Z);
    //            vertices[3] = new Vector3(x, box.Min.Y, box.Max.Z);
    //        }
    //        else if (MathF.Abs(normal.Y) > MathF.Abs(normal.Z))
    //        {
    //            // Y-грань
    //            float y = normal.Y > 0 ? box.Max.Y : box.Min.Y;
    //            vertices[0] = new Vector3(box.Min.X, y, box.Min.Z);
    //            vertices[1] = new Vector3(box.Max.X, y, box.Min.Z);
    //            vertices[2] = new Vector3(box.Max.X, y, box.Max.Z);
    //            vertices[3] = new Vector3(box.Min.X, y, box.Max.Z);
    //        }
    //        else
    //        {
    //            // Z-грань
    //            float z = normal.Z > 0 ? box.Max.Z : box.Min.Z;
    //            vertices[0] = new Vector3(box.Min.X, box.Min.Y, z);
    //            vertices[1] = new Vector3(box.Max.X, box.Min.Y, z);
    //            vertices[2] = new Vector3(box.Max.X, box.Max.Y, z);
    //            vertices[3] = new Vector3(box.Min.X, box.Max.Y, z);
    //        }

    //        return vertices;
    //    }

    //    private Vector2[] ProjectVerticesOnPlane(Vector3[] vertices, Vector3 normal)
    //    {
    //        // Создаем базис для проекции
    //        Vector3 tangent1, tangent2;
    //        if (MathF.Abs(normal.Y) < 0.999f)
    //            tangent1 = Vector3.Normalize(Vector3.Cross(normal, Vector3.UnitY));
    //        else
    //            tangent1 = Vector3.Normalize(Vector3.Cross(normal, Vector3.UnitZ));
    //        tangent2 = Vector3.Normalize(Vector3.Cross(normal, tangent1));

    //        // Проецируем вершины
    //        var projected = new Vector2[vertices.Length];
    //        for (int i = 0; i < vertices.Length; i++)
    //        {
    //            projected[i] = new Vector2(
    //                Vector3.Dot(vertices[i], tangent1),
    //                Vector3.Dot(vertices[i], tangent2)
    //            );
    //        }

    //        return projected;
    //    }

    //    private Vector3[] FindIntersectionPoints(Vector2[] polygonA, Vector2[] polygonB)
    //    {
    //        var intersectionPoints = new List<Vector3>();

    //        // Простая реализация - берем центр пересечения
    //        var centerA = new Vector2(0, 0);
    //        var centerB = new Vector2(0, 0);

    //        foreach (var point in polygonA)
    //            centerA += point;
    //        foreach (var point in polygonB)
    //            centerB += point;

    //        centerA /= polygonA.Length;
    //        centerB /= polygonB.Length;

    //        var intersectionCenter = (centerA + centerB) * 0.5f;

    //        // Преобразуем обратно в 3D
    //        intersectionPoints.Add(new Vector3(intersectionCenter.X, intersectionCenter.Y, 0));

    //        return intersectionPoints.ToArray();
    //    }

    //    public void Dispose()
    //    {
    //        _leafNodes.Clear();
    //        _root = null;
    //    }
    //}
}