using System.Numerics;
using static AtomEngine.GJKAlgorithm;

namespace AtomEngine
{
    public static class EPAAlgorithm
    {
        private const float Epsilon = 1e-4f;  // Точность для определения завершения алгоритма
        private const int MaxIterations = 32;  // Максимальное количество итераций

        // Структура для представления грани политопа
        private struct Face
        {
            public int A, B, C;          // Индексы вершин, образующих треугольник
            public Vector3 Normal;       // Нормаль к грани (направлена наружу)
            public float Distance;       // Расстояние от грани до начала координат

            // Создаем грань из трех вершин
            public Face(int a, int b, int c, List<Vector3> vertices)
            {
                A = a;
                B = b;
                C = c;

                // Вычисляем нормаль как векторное произведение
                Vector3 ab = vertices[b] - vertices[a];
                Vector3 ac = vertices[c] - vertices[a];
                Normal = Vector3.Cross(ab, ac);

                // Нормализуем вектор нормали
                if (Normal.LengthSquared() > Epsilon)
                {
                    Normal = Vector3.Normalize(Normal);

                    // Проверяем, смотрит ли нормаль от начала координат
                    if (Vector3.Dot(Normal, vertices[a]) < 0)
                    {
                        // Если нет, меняем направление нормали и порядок вершин
                        Normal = -Normal;
                        (B, C) = (C, B);
                    }
                }

                // Вычисляем расстояние от грани до начала координат
                Distance = Vector3.Dot(Normal, vertices[a]);
            }
        }

        // Основной метод EPA, который возвращает информацию о контакте
        public static bool GetContactInfo(
            GJKAlgorithm.Simplex gjkSimplex,  // Симплекс от GJK
            SupportFunction supportA,          // Support функция для первого объекта
            SupportFunction supportB,          // Support функция для второго объекта
            out Vector3 normal,               // Нормаль контакта
            out float penetrationDepth,       // Глубина проникновения
            out Vector3 contactPoint)         // Точка контакта
        {
            // Инициализируем выходные параметры
            normal = Vector3.Zero;
            penetrationDepth = 0;
            contactPoint = Vector3.Zero;

            // Создаем список вершин из симплекса GJK
            List<Vector3> vertices = new List<Vector3>();
            for (int i = 0; i < gjkSimplex.Count; i++)
            {
                vertices.Add(gjkSimplex[i]);
            }

            // Создаем начальные грани из тетраэдра
            List<Face> faces = new List<Face>();
            InitializeFaces(vertices, faces);

            // Основной цикл EPA
            for (int iteration = 0; iteration < MaxIterations; iteration++)
            {
                // Находим ближайшую к началу координат грань
                int closestFaceIndex = FindClosestFace(faces, vertices);
                Face closestFace = faces[closestFaceIndex];

                // Ищем новую точку в направлении нормали ближайшей грани
                Vector3 searchDirection = closestFace.Normal;
                Vector3 support = supportA(searchDirection) - supportB(-searchDirection);

                float newDistance = Vector3.Dot(support, closestFace.Normal);

                // Проверяем, нашли ли мы минимальное расстояние
                if (newDistance - closestFace.Distance < Epsilon)
                {
                    // Нашли результат
                    normal = closestFace.Normal;
                    penetrationDepth = closestFace.Distance;
                    contactPoint = CalculateContactPoint(vertices[closestFace.A],
                                                      vertices[closestFace.B],
                                                      vertices[closestFace.C]);
                    return true;
                }

                // Расширяем политоп, добавляя новую точку
                vertices.Add(support);
                ExpandPolytopeWithNewVertex(vertices, faces, vertices.Count - 1);
            }

            return false;
        }

        private static void InitializeFaces(List<Vector3> vertices, List<Face> faces)
        {
            // Тетраэдр состоит из четырех треугольных граней
            // Нам нужно создать эти грани так, чтобы их нормали были направлены наружу

            // Предполагаем, что вершины тетраэдра идут в таком порядке:
            // vertices[0] - последняя добавленная точка
            // vertices[1] - предпоследняя точка
            // vertices[2] - вторая точка
            // vertices[3] - первая точка

            // Создаем четыре грани тетраэдра
            faces.Add(new Face(0, 1, 2, vertices));  // Грань из первых трех точек
            faces.Add(new Face(0, 2, 3, vertices));  // Грань, включающая дальнюю точку
            faces.Add(new Face(0, 3, 1, vertices));  // Грань слева
            faces.Add(new Face(1, 3, 2, vertices));  // Основание тетраэдра

            // Проверяем ориентацию граней
            ValidateFaceOrientation(vertices, faces);
        }

        private static void ValidateFaceOrientation(List<Vector3> vertices, List<Face> faces)
        {
            // Для каждой грани проверяем, что четвертая точка тетраэдра
            // находится по отрицательную сторону от грани
            // Это гарантирует, что все нормали направлены наружу

            for (int i = 0; i < faces.Count; i++)
            {
                Face face = faces[i];

                // Находим точку, не принадлежащую текущей грани
                Vector3 unusedPoint = Vector3.Zero;
                for (int j = 0; j < 4; j++)  // У нас всегда 4 точки в тетраэдре
                {
                    if (j != face.A && j != face.B && j != face.C)
                    {
                        unusedPoint = vertices[j];
                        break;
                    }
                }

                // Проверяем знак проекции вектора от грани к точке на нормаль грани
                Vector3 pointToFace = unusedPoint - vertices[face.A];
                if (Vector3.Dot(pointToFace, face.Normal) > 0)
                {
                    // Если проекция положительная, нормаль направлена внутрь
                    // Меняем порядок вершин на противоположный
                    faces[i] = new Face(face.A, face.C, face.B, vertices);
                }
            }
        }

        private static int FindClosestFace(List<Face> faces, List<Vector3> vertices)
        {
            float minDistance = float.MaxValue;
            int closestFaceIndex = 0;

            // Проходим по всем граням и ищем ту, что ближе всего к началу координат
            for (int i = 0; i < faces.Count; i++)
            {
                Face face = faces[i];

                // Расстояние до грани - это проекция любой её точки на нормаль
                // Мы уже сохранили это значение в Face.Distance
                float distance = face.Distance;

                // Убеждаемся, что расстояние положительное
                // Если оно отрицательное, что-то пошло не так с нашими нормалями
                if (distance > 0 && distance < minDistance)
                {
                    minDistance = distance;
                    closestFaceIndex = i;
                }
            }

            return closestFaceIndex;
        }

        private static Vector3 CalculateContactPoint(Vector3 a, Vector3 b, Vector3 c)
        {
            // Вычисляем барицентрические координаты проекции начала координат на грань

            // Векторы граней
            Vector3 ab = b - a;
            Vector3 ac = c - a;

            // Нормаль к грани
            Vector3 normal = Vector3.Normalize(Vector3.Cross(ab, ac));

            // Проекция начала координат на плоскость грани
            Vector3 point = a - Vector3.Dot(a, normal) * normal;

            // Вычисляем барицентрические координаты
            Vector3 crossA = Vector3.Cross(b - point, c - point);
            Vector3 crossB = Vector3.Cross(c - point, a - point);
            Vector3 crossC = Vector3.Cross(a - point, b - point);

            float totalArea = Vector3.Dot(normal, Vector3.Cross(b - a, c - a));
            float u = Vector3.Dot(normal, crossA) / totalArea;
            float v = Vector3.Dot(normal, crossB) / totalArea;
            float w = Vector3.Dot(normal, crossC) / totalArea;

            // Если точка проекции лежит внутри треугольника, используем её
            if (u >= 0 && v >= 0 && w >= 0)
            {
                return u * a + v * b + w * c;
            }

            // Иначе находим ближайшее ребро или вершину
            float abDist = PointLineDistance(Vector3.Zero, a, b);
            float bcDist = PointLineDistance(Vector3.Zero, b, c);
            float caDist = PointLineDistance(Vector3.Zero, c, a);

            float minDist = Math.Min(abDist, Math.Min(bcDist, caDist));

            if (minDist == abDist)
                return ClosestPointOnLine(Vector3.Zero, a, b);
            else if (minDist == bcDist)
                return ClosestPointOnLine(Vector3.Zero, b, c);
            else
                return ClosestPointOnLine(Vector3.Zero, c, a);
        }

        private static float PointLineDistance(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
        {
            Vector3 line = lineEnd - lineStart;
            float length = line.Length();
            if (length < Epsilon)
                return Vector3.Distance(point, lineStart);

            Vector3 normalized = line / length;
            float projection = Vector3.Dot(point - lineStart, normalized);

            if (projection <= 0)
                return Vector3.Distance(point, lineStart);
            if (projection >= length)
                return Vector3.Distance(point, lineEnd);

            return Vector3.Distance(point, lineStart + normalized * projection);
        }

        private static Vector3 ClosestPointOnLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
        {
            Vector3 line = lineEnd - lineStart;
            float length = line.Length();
            if (length < Epsilon)
                return lineStart;

            Vector3 normalized = line / length;
            float projection = Vector3.Dot(point - lineStart, normalized);

            if (projection <= 0)
                return lineStart;
            if (projection >= length)
                return lineEnd;

            return lineStart + normalized * projection;
        }

        private static void ExpandPolytopeWithNewVertex(List<Vector3> vertices, List<Face> faces, int newVertexIndex)
        {
            // Хранит ребра, которые формируют "силуэт" видимой части политопа
            List<(int, int)> silhouetteEdges = new List<(int, int)>();
            // Хранит индексы граней, которые нужно удалить
            List<int> facesToRemove = new List<int>();

            // Определяем, какие грани видны из новой точки
            // и собираем силуэтные ребра
            for (int i = 0; i < faces.Count; i++)
            {
                Face face = faces[i];
                Vector3 newPoint = vertices[newVertexIndex];

                // Проверяем, видна ли грань из новой точки
                // Грань видна, если новая точка находится перед ней (по направлению нормали)
                Vector3 pointToFace = newPoint - vertices[face.A];
                if (Vector3.Dot(pointToFace, face.Normal) > Epsilon)  // Грань видна
                {
                    // Добавляем ребра этой грани как потенциальные силуэтные
                    AddEdgeIfSilhouette((face.A, face.B), silhouetteEdges);
                    AddEdgeIfSilhouette((face.B, face.C), silhouetteEdges);
                    AddEdgeIfSilhouette((face.C, face.A), silhouetteEdges);

                    // Помечаем грань для удаления
                    facesToRemove.Add(i);
                }
            }

            // Удаляем видимые грани (в обратном порядке, чтобы не нарушить индексы)
            facesToRemove.Sort();
            for (int i = facesToRemove.Count - 1; i >= 0; i--)
            {
                faces.RemoveAt(facesToRemove[i]);
            }

            // Создаем новые грани, соединяя силуэтные ребра с новой точкой
            foreach (var edge in silhouetteEdges)
            {
                faces.Add(new Face(edge.Item1, edge.Item2, newVertexIndex, vertices));
            }
        }

        private static void AddEdgeIfSilhouette((int, int) edge, List<(int, int)> silhouetteEdges)
        {
            // Ребро является силуэтным, если оно встречается только один раз
            // (т.е. разделяет видимую и невидимую грани)

            // Ищем обратное ребро
            var reverseEdge = (edge.Item2, edge.Item1);

            // Проверяем, есть ли уже это ребро в списке
            int index = silhouetteEdges.FindIndex(e =>
                (e.Item1 == edge.Item1 && e.Item2 == edge.Item2) ||
                (e.Item1 == edge.Item2 && e.Item2 == edge.Item1));

            if (index != -1)
            {
                // Если ребро уже есть, удаляем его (оно не силуэтное)
                silhouetteEdges.RemoveAt(index);
            }
            else
            {
                // Если ребра нет, добавляем его
                silhouetteEdges.Add(edge);
            }
        }
    }
}
