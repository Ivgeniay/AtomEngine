using AtomEngine.Geometry;
using AtomEngine.Math;

namespace AtomEngine
{
    public sealed class Mesh
    {
        /// <summary>
        /// Координаты записываются в следующем порядке:
        /// {X;Y;Z}{nX;nY;nZ}{R;G;B}{texCoordX;texCoordY}
        /// тоесть 3-х векторов float на точку до 11, 
        /// 3 из которых будут описывать нахождение проекции вершины на экране + 2-е текстурные координаты
        /// </summary> 
        public IEnumerable<Vertice> Vertices => Triangles
                .SelectMany(tri => tri.Vertice)
                .Distinct(new VerticeEqualityComparer())
                .OrderBy(e => e.Index);

        public List<Triangle> Triangles = new List<Triangle>();

        public void SetTriangles(IEnumerable<Triangle> triangles)
        {
            uint counter = 0;
            Triangles = new List<Triangle>(triangles);

            //vertices = Triangles
            //    .SelectMany(tri => tri.Vertice)
            //    .Distinct(new VerticeEqualityComparer())
            //    .OrderBy(e => e.Index); 
        }

        public float[] ToVerticeInfoFloat()
        {
            List<float> result = new List<float>();
            foreach (Vertice vertice in Vertices)
                result.AddRange(vertice.FromoVerticeTo<float>());
                //result.AddRange(vertice.ToVerticeInfoFloat());
            return result.ToArray();
        }

        public void Clear()
        {
            //Vertices = null;
            //Uv = null;
            Triangles = null;
        }
    }
}
