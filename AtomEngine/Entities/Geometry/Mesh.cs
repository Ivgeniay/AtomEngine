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
        public List<Vertice>? Vertices;
        //public List<Vector2D>? TexCoord;
        public List<Triangle>? Triangles;
        //public List<Color4>? Colors;
        //public List<Vector2D<double>>? Uv;
        public int VertexCount => Vertices?.Count ?? 0;

        public void SetTriangles(IEnumerable<Triangle> triangles)
        {
            uint counter = 0;
            Triangles = new List<Triangle>(triangles);

            foreach (Triangle triangle in Triangles) 
                for(int i = 0; i < triangle.Vertice.Count(); i++) 
                    triangle.Vertice[i].Index = counter++;  
        }

        public void Clear()
        {
            Vertices = null;
            //Uv = null;
            Triangles = null;
        }
    }
}
