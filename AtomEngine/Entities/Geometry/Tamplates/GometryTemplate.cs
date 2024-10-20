using AtomEngine.Geometry;
using AtomEngine.Math;

namespace AtomEngine.Entities.Geometry
{
    internal static class GometryTemplate
    {
        public static Vertice[] Quad() =>
            new Vertice[] { 
                new Vertice(new Vector3D(-0.5f, -0.5f, 0.0f), 0),
                new Vertice(new Vector3D(0.5f, -0.5f, 0.0f), 1),
                new Vertice(new Vector3D(0.5f,  0.5f, 0.0f), 2),
                new Vertice(new Vector3D(-0.5f,  0.5f, 0.0f), 3),
            };

        public static Vertice[] Cube() =>
            new Vertice[] {
                new Vertice(new Vector3D(0.5f, 0.5f, 0.5f ), 0),
                new Vertice(new Vector3D(0.5f, 0.5f, -0.5f ), 1),
                new Vertice(new Vector3D(0.5f, -0.5f, 0.5f ), 2),
                new Vertice(new Vector3D(0.5f, -0.5f, -0.5f ), 3),
                new Vertice(new Vector3D(-0.5f, -0.5f, 0.5f ), 4),
                new Vertice(new Vector3D(-0.5f, -0.5f, -0.5f), 5),
                new Vertice(new Vector3D(-0.5f, 0.5f, 0.5f  ), 6),
                new Vertice(new Vector3D(-0.5f, 0.5f, -0.5f ), 7), 
            }; 
    }
}
