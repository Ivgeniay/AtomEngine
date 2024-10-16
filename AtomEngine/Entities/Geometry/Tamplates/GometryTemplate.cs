using AtomEngine.Geometry;
using AtomEngine.Math;

namespace AtomEngine.Entities.Geometry
{
    internal static class GometryTemplate
    {
        public static Vertice[] Quad() =>
            new Vertice[] { 
                new Vertice(new Vector4D(-0.5f, -0.5f, 0.0f, 1f), 0),
                new Vertice(new Vector4D(0.5f, -0.5f, 0.0f, 1f), 0),
                new Vertice(new Vector4D(0.5f,  0.5f, 0.0f, 1f), 0),
                new Vertice(new Vector4D(-0.5f,  0.5f, 0.0f, 1f), 0),
            };

        public static Vertice[] Cube() =>
            new Vertice[] {
                new Vertice(new Vector4D(0.5f, 0.5f, 0.5f, 1f), 0),
                new Vertice(new Vector4D(0.5f, 0.5f, -0.5f, 1f), 1),
                new Vertice(new Vector4D(0.5f, -0.5f, 0.5f, 1f), 2),
                new Vertice(new Vector4D(0.5f, -0.5f, -0.5f, 1f), 3),
                new Vertice(new Vector4D(-0.5f, -0.5f, 0.5f, 1f), 4),
                new Vertice(new Vector4D(-0.5f, -0.5f, -0.5f, 1f), 5),
                new Vertice(new Vector4D(-0.5f, 0.5f, 0.5f, 1f), 6),
                new Vertice(new Vector4D(-0.5f, 0.5f, -0.5f, 1f), 7), 
            }; 
    }
}
