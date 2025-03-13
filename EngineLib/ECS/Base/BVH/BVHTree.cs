using AtomEngine.RenderEntity;
using System.Numerics;
using System.Reflection;

namespace AtomEngine
{
    public class BVHTreeNode
    {
        public BoundingBox Bounds { get; set; }
        public bool IsLeaf { get; set; }
        public List<uint> EntityIds { get; set; } = new List<uint>();
        public BVHTreeNode Left { get; set; }
        public BVHTreeNode Right { get; set; }
    }

    public struct BvhRaycastHit
    {
        public uint EntityId { get; set; }
        public float Distance { get; set; }
        public Vector3 Point { get; set; }
    }

    public class BVHTree : IDisposable
    {
        private static BVHTree _instance;
        public static BVHTree Instance => _instance ??= new BVHTree();

        private BVHTreeNode _root;
        private Dictionary<uint, BoundingBox> _entityBounds = new Dictionary<uint, BoundingBox>();
        private Dictionary<uint, MeshBase> _entityMesh = new Dictionary<uint, MeshBase>();
        private IEntityComponentInfoProvider _componentProvider;
        private int _maxEntitiesPerNode = 4;
        private float _minimumNodeSize = 0.01f;

        private BVHTree() { }
        public void Initialize(IEntityComponentInfoProvider componentProvider)
        {
            _componentProvider = componentProvider;
            _root = null;
            _entityBounds.Clear();
        }

        public void AddEntity(uint entityId)
        {
            if (_componentProvider == null)
                return;

            if (!_componentProvider.HasComponent<TransformComponent>(entityId) ||
                !_componentProvider.HasComponent<MeshComponent>(entityId))
                return;

            var transformComponent = _componentProvider.GetComponent<TransformComponent>(entityId);
            var meshComponent = _componentProvider.GetComponent<MeshComponent>(entityId);

            MeshBase mesh = GetMeshFromComponent(meshComponent);
            _entityMesh[entityId] = mesh;

            if (mesh?.BoundingVolume == null)
                return;

            CalculateBound(entityId, transformComponent, mesh);
            RebuildTree();
        }
        private void CalculateBound(uint entityId, TransformComponent transformComponent, MeshBase mesh)
        {
            var modelMatrix = CreateModelMatrix(transformComponent);
            if (mesh.BoundingVolume.Transform(modelMatrix) is BoundingBox worldBounds)
            {
                _entityBounds[entityId] = worldBounds;
            }
        }
        public void RemoveEntity(uint entityId)
        {
            if (_entityBounds.Remove(entityId))
            {
                RebuildTree();
            }
        }
        public void UpdateEntity(uint entityId)
        {
            if (_componentProvider == null ||
                !_componentProvider.HasComponent<TransformComponent>(entityId) ||
                !_componentProvider.HasComponent<MeshComponent>(entityId))
                return;

            RemoveEntity(entityId);
            AddEntity(entityId);
        }
        private void RebuildTree()
        {
            if (_entityBounds.Count == 0)
            {
                _root = null;
                return;
            }

            var allEntityIds = _entityBounds.Keys.ToList();
            foreach ( var entityId in allEntityIds)
            {
                var transformComponent = _componentProvider.GetComponent<TransformComponent>(entityId);
                var mesh = _entityMesh[entityId];
                CalculateBound(entityId, transformComponent, mesh);
            }
            var allBounds = ComputeBoundsForEntities(allEntityIds);
            _root = BuildNode(allEntityIds, allBounds);
        }
        private BoundingBox ComputeBoundsForEntities(List<uint> entityIds)
        {
            if (entityIds.Count == 0)
                return new BoundingBox(Vector3.Zero, Vector3.Zero);

            var first = _entityBounds[entityIds[0]];
            var min = first.Min;
            var max = first.Max;

            for (int i = 1; i < entityIds.Count; i++)
            {
                var bounds = _entityBounds[entityIds[i]];
                min = Vector3.Min(min, bounds.Min);
                max = Vector3.Max(max, bounds.Max);
            }

            return new BoundingBox(min, max);
        }
        private BVHTreeNode BuildNode(List<uint> entityIds, BoundingBox bounds)
        {
            var node = new BVHTreeNode
            {
                Bounds = bounds,
                EntityIds = new List<uint>()
            };

            // Если количество сущностей меньше порога или размер узла слишком мал, делаем лист
            if (entityIds.Count <= _maxEntitiesPerNode ||
                IsTooSmall(bounds))
            {
                node.IsLeaf = true;
                node.EntityIds.AddRange(entityIds);
                return node;
            }

            // Иначе разделяем на два узла
            var (leftIds, rightIds) = SplitEntities(entityIds, bounds);

            // Если разделение не удалось, создаем лист
            if (leftIds.Count == 0 || rightIds.Count == 0)
            {
                node.IsLeaf = true;
                node.EntityIds.AddRange(entityIds);
                return node;
            }

            // Создаем дочерние узлы
            var leftBounds = ComputeBoundsForEntities(leftIds);
            var rightBounds = ComputeBoundsForEntities(rightIds);

            node.Left = BuildNode(leftIds, leftBounds);
            node.Right = BuildNode(rightIds, rightBounds);
            node.IsLeaf = false;

            return node;
        }
        private bool IsTooSmall(BoundingBox bounds)
        {
            var size = bounds.Max - bounds.Min;
            return size.X < _minimumNodeSize || size.Y < _minimumNodeSize || size.Z < _minimumNodeSize;
        }
        private (List<uint>, List<uint>) SplitEntities(List<uint> entityIds, BoundingBox bounds)
        {
            var leftIds = new List<uint>();
            var rightIds = new List<uint>();

            // Находим самую длинную ось
            var size = bounds.Max - bounds.Min;
            int axis = 0;
            if (size.Y > size.X) axis = 1;
            if (size.Z > size[axis]) axis = 2;

            // Вычисляем центр разделения
            float splitPosition = bounds.Min[axis] + size[axis] * 0.5f;

            // Распределяем сущности по узлам
            foreach (var entityId in entityIds)
            {
                var entityBounds = _entityBounds[entityId];
                var center = (entityBounds.Min + entityBounds.Max) * 0.5f;

                if (center[axis] < splitPosition)
                    leftIds.Add(entityId);
                else
                    rightIds.Add(entityId);
            }

            // Если все сущности оказались в одном узле, делим список пополам
            if (leftIds.Count == 0 || rightIds.Count == 0)
            {
                entityIds.Sort((a, b) =>
                {
                    var centerA = (_entityBounds[a].Min + _entityBounds[a].Max) * 0.5f;
                    var centerB = (_entityBounds[b].Min + _entityBounds[b].Max) * 0.5f;
                    return centerA[axis].CompareTo(centerB[axis]);
                });

                int mid = entityIds.Count / 2;
                leftIds = entityIds.Take(mid).ToList();
                rightIds = entityIds.Skip(mid).ToList();
            }

            return (leftIds, rightIds);
        }
        private MeshBase GetMeshFromComponent(MeshComponent meshComponent)
        {
            var type = meshComponent.GetType();
            var fields = type
                .GetFields(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(e => typeof(MeshBase).IsAssignableFrom(e.FieldType));
            if (fields != null)
            {
                var value = fields.GetValue(meshComponent);
                if (value != null) return fields.GetValue(meshComponent) as MeshBase;
                return null;
            }
            return null;
        }
        private Matrix4x4 CreateModelMatrix(TransformComponent transform)
        {
            Matrix4x4 result = Matrix4x4.Identity;

            Matrix4x4 translate = Matrix4x4.CreateTranslation(transform.Position);
            Matrix4x4 rotation = Matrix4x4.CreateFromQuaternion(transform.Rotation.ToQuaternion());
            Matrix4x4 scale = Matrix4x4.CreateScale(transform.Scale);

            result *= rotation;
            result *= translate;
            result *= scale;

            return result;
        }

        public void FreeCache()
        {
            _entityBounds.Clear();
            _entityMesh.Clear();
            _root = null;
        }

        public void Dispose()
        {
            _componentProvider = null;
            _entityBounds.Clear();
            _entityMesh.Clear();
        }
        public class BvhRay
        {
            private Vector3 _origin;
            private Vector3 _direction;
            private float _maxDistance;

            public BvhRay(Vector3 origin, Vector3 direction, float maxDistance = float.MaxValue)
            {
                _origin = origin;
                _direction = Vector3.Normalize(direction);
                _maxDistance = maxDistance;
            }

            public bool Raycast(out BvhRaycastHit hit)
            {
                hit = new BvhRaycastHit();

                if (BVHTree.Instance._root == null)
                    return false;

                float closestDistance = float.MaxValue;
                uint closestEntityId = 0;
                var closestIntersectionPoint = Vector3.Zero;
                bool hasHit = false;

                RaycastNode(BVHTree.Instance._root, ref hasHit, ref closestDistance, ref closestEntityId, ref closestIntersectionPoint);

                if (hasHit)
                {
                    hit.EntityId = closestEntityId;
                    hit.Distance = closestDistance;
                    hit.Point = closestIntersectionPoint;
                    return true;
                }

                return false;
            }

            private void RaycastNode(BVHTreeNode node, ref bool hasHit, ref float closestDistance, ref uint closestEntityId, ref Vector3 closestPoint)
            {
                // Проверяем пересечение с ограничивающим объемом узла
                if (!IntersectsAABB(node.Bounds))
                    return;

                // Если это лист, проверяем все сущности в нем
                if (node.IsLeaf)
                {
                    foreach (var entityId in node.EntityIds)
                    {
                        if (RaycastEntity(entityId, out var distance, out var point) && distance < closestDistance)
                        {
                            hasHit = true;
                            closestDistance = distance;
                            closestEntityId = entityId;
                            closestPoint = point;
                        }
                    }
                }
                else
                {
                    // Иначе рекурсивно проверяем дочерние узлы
                    if (node.Left != null)
                        RaycastNode(node.Left, ref hasHit, ref closestDistance, ref closestEntityId, ref closestPoint);

                    // Если нашли пересечение ближе, чем с текущим узлом, можем не проверять правый узел
                    if (node.Right != null)
                        RaycastNode(node.Right, ref hasHit, ref closestDistance, ref closestEntityId, ref closestPoint);
                }
            }

            private bool RaycastEntity(uint entityId, out float distance, out Vector3 point)
            {
                distance = float.MaxValue;
                point = Vector3.Zero;

                if (!BVHTree.Instance._entityBounds.TryGetValue(entityId, out var bounds) ||
                    !BVHTree.Instance._entityMesh.TryGetValue(entityId, out var mesh))
                    return false;

                if (!IntersectsAABB(bounds, out var aabbDistance, out var aabbPoint) || aabbDistance > _maxDistance)
                    return false;

                RayShape rayShape = new RayShape(_origin, _direction, aabbDistance + 0.1f);

                var meshVertices = GetTransformedMeshVertices(entityId, mesh);

                GJKAlgorithm.SupportFunction raySupport = direction => rayShape.GetSupport(direction);
                GJKAlgorithm.SupportFunction meshSupport = direction => GetSupportFromVertices(meshVertices, direction);

                GJKAlgorithm.Simplex simplex;
                if (GJKAlgorithm.Intersect(raySupport, meshSupport, out simplex))
                {
                    point = GetIntersectionPoint(simplex);
                    distance = Vector3.Distance(_origin, point);

                    Vector3 toPoint = point - _origin;
                    float projectionLength = Vector3.Dot(toPoint, _direction);

                    if (projectionLength >= 0 && projectionLength <= _maxDistance)
                    {
                        return true;
                    }
                }

                // Если GJK не нашел пересечения или точка не на луче, возвращаем результат AABB проверки
                distance = aabbDistance;
                point = aabbPoint;
                return true;
                //return false;
            }

            private Vector3[] GetTransformedMeshVertices(uint entityId, MeshBase mesh)
            {
                // Получаем компонент трансформации
                var transformComponent = BVHTree.Instance._componentProvider.GetComponent<TransformComponent>(entityId);

                // Вычисляем матрицу трансформации
                Matrix4x4 modelMatrix = BVHTree.Instance.CreateModelMatrix(transformComponent);

                // Получаем вершины меша
                Vector3[] vertices;

                if (mesh.Vertices_ != null)
                {
                    // Предполагается, что Vertices_ содержит вершины меша
                    vertices = new Vector3[mesh.Vertices_.Length];
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        vertices[i] = Vector3.Transform(mesh.Vertices_[i].Position, modelMatrix);
                    }
                }
                else if (mesh.BoundingVolume is BoundingBox boundingBox)
                {
                    // Если у нас нет доступа к вершинам меша, используем вершины ограничивающего бокса
                    vertices = boundingBox.GetVertices();
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        vertices[i] = Vector3.Transform(vertices[i], modelMatrix);
                    }
                }
                else
                {
                    // Если нет ни вершин, ни бокса, возвращаем пустой массив
                    vertices = new Vector3[0];
                }

                return vertices;
            }
            private Vector3 GetSupportFromVertices(Vector3[] vertices, Vector3 direction)
            {
                if (vertices.Length == 0)
                    return Vector3.Zero;

                float maxDot = float.MinValue;
                Vector3 support = Vector3.Zero;

                foreach (var vertex in vertices)
                {
                    float dot = Vector3.Dot(vertex, direction);
                    if (dot > maxDot)
                    {
                        maxDot = dot;
                        support = vertex;
                    }
                }

                return support;
            }
            private Vector3 GetIntersectionPoint(GJKAlgorithm.Simplex simplex)
            {
                if (simplex.Count == 0)
                    return Vector3.Zero;

                // Простое усреднение точек симплекса как приближение точки пересечения
                Vector3 center = Vector3.Zero;
                for (int i = 0; i < simplex.Count; i++)
                {
                    center += simplex[i];
                }

                return center / simplex.Count;

                // Примечание: для более точного определения точки пересечения можно использовать 
                // дополнительные алгоритмы, такие как EPA (Expanding Polytope Algorithm)
            }

            //private bool RaycastEntity(uint entityId, out float distance, out Vector3 point)
            //{
            //    distance = float.MaxValue;
            //    point = Vector3.Zero;

            //    if (!BVHTree.Instance._entityBounds.TryGetValue(entityId, out var bounds))
            //        return false;

            //    if (IntersectsAABB(bounds, out distance, out point) && distance <= _maxDistance)
            //        return true;

            //    return false;
            //}

            private bool IntersectsAABB(BoundingBox box)
            {
                float tMin = float.MinValue;
                float tMax = float.MaxValue;

                // Проверка для каждой оси
                for (int i = 0; i < 3; i++)
                {
                    if (Math.Abs(_direction[i]) < float.Epsilon)
                    {
                        // Луч параллелен оси, проверяем, находится ли начало луча между границами
                        if (_origin[i] < box.Min[i] || _origin[i] > box.Max[i])
                            return false;
                    }
                    else
                    {
                        // Вычисляем параметры пересечения с плоскостями
                        float invD = 1.0f / _direction[i];
                        float t1 = (box.Min[i] - _origin[i]) * invD;
                        float t2 = (box.Max[i] - _origin[i]) * invD;

                        // Меняем местами, если t1 > t2
                        if (t1 > t2)
                        {
                            float temp = t1;
                            t1 = t2;
                            t2 = temp;
                        }

                        // Обновляем границы
                        tMin = Math.Max(tMin, t1);
                        tMax = Math.Min(tMax, t2);

                        if (tMin > tMax)
                            return false;
                    }
                }

                return tMax >= 0 && tMin <= _maxDistance;
            }

            private bool IntersectsAABB(BoundingBox box, out float distance, out Vector3 point)
            {
                distance = float.MaxValue;
                point = Vector3.Zero;

                float tMin = float.MinValue;
                float tMax = float.MaxValue;

                // Проверка для каждой оси
                for (int i = 0; i < 3; i++)
                {
                    if (Math.Abs(_direction[i]) < float.Epsilon)
                    {
                        // Луч параллелен оси
                        if (_origin[i] < box.Min[i] || _origin[i] > box.Max[i])
                            return false;
                    }
                    else
                    {
                        float invD = 1.0f / _direction[i];
                        float t1 = (box.Min[i] - _origin[i]) * invD;
                        float t2 = (box.Max[i] - _origin[i]) * invD;

                        if (t1 > t2)
                        {
                            float temp = t1;
                            t1 = t2;
                            t2 = temp;
                        }

                        tMin = Math.Max(tMin, t1);
                        tMax = Math.Min(tMax, t2);

                        if (tMin > tMax)
                            return false;
                    }
                }

                // Проверяем, что пересечение находится перед началом луча
                if (tMax < 0)
                    return false;

                // Используем ближайшую точку пересечения
                distance = Math.Max(0, tMin);
                if (distance > _maxDistance)
                    return false;

                point = _origin + _direction * distance;
                return true;
            }
        }
    }

    public class RayShape
    {
        private Vector3 _origin;
        private Vector3 _direction;
        private float _length;

        public RayShape(Vector3 origin, Vector3 direction, float length)
        {
            _origin = origin;
            _direction = Vector3.Normalize(direction);
            _length = length;
        }

        public Vector3 GetSupport(Vector3 direction)
        {
            float directionDot = Vector3.Dot(_direction, direction);

            if (directionDot > 0)
                return _origin + _direction * _length;

            return _origin;
        }
    }

    public interface IEntityComponentInfoProvider
    {
        public ref T GetComponent<T>(uint entity) where T : struct, IComponent;
        public bool HasComponent<T>(uint entity) where T : struct, IComponent;
    }
}
