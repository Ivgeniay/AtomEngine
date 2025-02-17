using System.Numerics;

namespace AtomEngine
{
    public struct BoundingSphere : IBoundingVolume
    {
        private const int SEGMENTS = 16;
        private const int LONGITUDE_SEGMENTS = SEGMENTS * 2;

        private Vector3[] _vertices;
        private uint[] _indices;

        public Vector3 Position;
        public float Radius;
        public Vector3 Min => Position - new Vector3(Radius);
        public Vector3 Max => Position + new Vector3(Radius);

        public BoundingSphere(Vector3 position, float radius)
        {
            Position = position;
            Radius = radius;
        }
        

        public static BoundingSphere ComputeBoundingSphere(Vertex[] vertices)
        {
            if (vertices.Length == 0)
                return new BoundingSphere(Vector3.Zero, 0);
            Vector3 center = Vector3.Zero;
            foreach (var vertex in vertices)
            {
                center += vertex.Position;
            }
            center /= vertices.Length;

            float maxRadiusSq = 0;
            foreach (var vertex in vertices)
            {
                float distSq = Vector3.DistanceSquared(center, vertex.Position);
                if (distSq > maxRadiusSq)
                    maxRadiusSq = distSq;
            }

            return new BoundingSphere(center, MathF.Sqrt(maxRadiusSq));
        }

        public static BoundingSphere ComputeBoundingSphereRitter(Vertex[] vertices)
        {
            if (vertices.Length == 0)
                return new BoundingSphere(Vector3.Zero, 0);

            // 1. Находим две самые дальние точки
            Vector3 p1 = vertices[0].Position;
            Vector3 p2 = p1;
            float maxDistSq = 0;

            // Находим самую дальнюю точку от p1
            foreach (var vertex in vertices)
            {
                float distSq = Vector3.DistanceSquared(p1, vertex.Position);
                if (distSq > maxDistSq)
                {
                    maxDistSq = distSq;
                    p2 = vertex.Position;
                }
            }

            // Находим самую дальнюю точку от p2
            maxDistSq = 0;
            foreach (var vertex in vertices)
            {
                float distSq = Vector3.DistanceSquared(p2, vertex.Position);
                if (distSq > maxDistSq)
                {
                    maxDistSq = distSq;
                    p1 = vertex.Position;
                }
            }

            // 2. Используем эти точки как начальную сферу
            Vector3 center = (p1 + p2) * 0.5f;
            float radius = Vector3.Distance(p1, p2) * 0.5f;

            // 3. Расширяем сферу чтобы включить все точки
            foreach (var vertex in vertices)
            {
                Vector3 d = vertex.Position - center;
                float dist = d.Length();
                if (dist > radius)
                {
                    float newRadius = (radius + dist) * 0.5f;
                    float k = (newRadius - radius) / dist;
                    radius = newRadius;
                    center += d * k;
                }
            }

            return new BoundingSphere(center, radius);
        }

        public bool Intersects(IBoundingVolume other) => other switch
        {
            BoundingBox box => this.Intersects(in box),
            BoundingSphere sphere => this.Intersects(in sphere),
            _ => throw new ArgumentError(nameof(Intersects) + " with " + $"{other}"),
        };

        public Vector3[] GetVertices()
        {
            if (_vertices != null) return _vertices;

            // Вычисляем количество вершин заранее для избежания ресайзов списка
            int vertexCount = (SEGMENTS + 1) * (LONGITUDE_SEGMENTS + 1);
            _vertices = new Vector3[vertexCount];
            int currentVertex = 0;

            // Генерируем вершины от верхнего полюса к нижнему
            for (int lat = 0; lat <= SEGMENTS; lat++)
            {
                // Преобразуем индекс в угол в радианах (-π/2 до π/2)
                float latitude = MathF.PI * (-0.5f + (float)lat / SEGMENTS);
                float sinLat = MathF.Sin(latitude);
                float cosLat = MathF.Cos(latitude);

                // Для каждой широты генерируем точки вокруг сферы
                for (int lon = 0; lon <= LONGITUDE_SEGMENTS; lon++)
                {
                    // Преобразуем индекс в угол в радианах (0 до 2π)
                    float longitude = 2 * MathF.PI * (float)lon / LONGITUDE_SEGMENTS;
                    float sinLon = MathF.Sin(longitude);
                    float cosLon = MathF.Cos(longitude);

                    // Вычисляем позицию точки на единичной сфере
                    Vector3 point = new Vector3(
                        cosLon * cosLat,  // x = r * cos(θ) * cos(φ)
                        sinLat,           // y = r * sin(φ)
                        sinLon * cosLat   // z = r * sin(θ) * cos(φ)
                    );

                    // Трансформируем точку с учетом позиции и радиуса сферы
                    _vertices[currentVertex++] = Position + point * Radius;
                }
            }

            return _vertices;
        }

        public uint[] GetIndices()
        {
            if (_indices != null) return _indices;

            // Вычисляем количество индексов заранее
            int lineCount = SEGMENTS * (LONGITUDE_SEGMENTS + 1) + LONGITUDE_SEGMENTS * (SEGMENTS + 1);
            _indices = new uint[lineCount * 2];
            int currentIndex = 0;

            // Создаем индексы для параллелей (горизонтальные линии)
            for (int lat = 0; lat <= SEGMENTS; lat++)
            {
                int rowStart = lat * (LONGITUDE_SEGMENTS + 1);
                for (int lon = 0; lon < LONGITUDE_SEGMENTS; lon++)
                {
                    _indices[currentIndex++] = (uint)(rowStart + lon);
                    _indices[currentIndex++] = (uint)(rowStart + lon + 1);
                }
            }

            // Создаем индексы для меридианов (вертикальные линии)
            for (int lon = 0; lon <= LONGITUDE_SEGMENTS; lon++)
            {
                for (int lat = 0; lat < SEGMENTS; lat++)
                {
                    _indices[currentIndex++] = (uint)(lat * (LONGITUDE_SEGMENTS + 1) + lon);
                    _indices[currentIndex++] = (uint)((lat + 1) * (LONGITUDE_SEGMENTS + 1) + lon);
                }
            }

            return _indices;
        }

        public IBoundingVolume Transform(Matrix4x4 modelTransformMatrix)
        {
            float scaleX = new Vector3(modelTransformMatrix.M11, modelTransformMatrix.M12, modelTransformMatrix.M13).Length();
            float scaleY = new Vector3(modelTransformMatrix.M21, modelTransformMatrix.M22, modelTransformMatrix.M23).Length();
            float scaleZ = new Vector3(modelTransformMatrix.M31, modelTransformMatrix.M32, modelTransformMatrix.M33).Length();

            float maxScale = MathF.Max(scaleX, MathF.Max(scaleY, scaleZ));
            Vector3 transformedCenter = Vector3.Transform(Position, modelTransformMatrix);

            return new BoundingSphere(transformedCenter, Radius * maxScale);
        }

        private bool Intersects(in BoundingSphere other)
        {
            return Vector3.Distance(Position, other.Position) < Radius + other.Radius;
        }

        private bool Intersects(in BoundingBox box)
        {
            float x = Math.Max(box.Min.X, Math.Min(Position.X, box.Max.X));
            float y = Math.Max(box.Min.Y, Math.Min(Position.Y, box.Max.Y));
            float z = Math.Max(box.Min.Z, Math.Min(Position.Z, box.Max.Z));
            float cubeDistance = (x - Position.X) * (x - Position.X) + (y - Position.Y) * (y - Position.Y) + (z - Position.Z) * (z - Position.Z);
            return cubeDistance < Radius * Radius;
        }
    }
}
