using AtomEngine.Math;

namespace AtomEngine.Geometry
{
    public class Triangle
    {
        public readonly Vertice[] Vertice;
        public Vector3D Normal;
        public Color4 Color;

        public Triangle(params Vertice[] vertice)
        {
            if (vertice.Length != Constants.VerticesPerTriangle) throw new ArgumentException("Triangle must have 3 vertices");
            this.Vertice = vertice;
        }

        private void CalculateNormal()
        {
            Vector3D v1 = Vertice[1] - Vertice[0];
            Vector3D v2 = Vertice[2] - Vertice[0];
            Vector3D crossProduct = v1.Cross(v2);

            if (crossProduct.SqrAbs() > Constants.EPS) Normal = crossProduct.Normalized();
            else Normal = new Vector3D(0, 0, 0); 
        }
    }
}

/*
 * class Triangle final {
private:
    sf::Color _color;
    Vec4D _points[3];
    Vec3D _normal;

    void calculateNormal();
public:
    Triangle() = default;

    Triangle(const Triangle &triangle);

    Triangle(const Vec4D &p1, const Vec4D &p2, const Vec4D &p3, sf::Color color = {0, 0, 0});

    Triangle &operator=(const Triangle &) = default;

    [[nodiscard]] const Vec4D& operator[](int i) const;

    [[nodiscard]] Vec3D position() const { return Vec3D(_points[0] + _points[1] + _points[2])/3; }

    [[nodiscard]] Vec3D norm() const;

    // Operations with Matrix4x4
    [[nodiscard]] Triangle operator*(const Matrix4x4 &matrix4X4) const;

    [[nodiscard]] bool isPointInside(const Vec3D &point) const;

    [[nodiscard]] sf::Color color() const { return _color; }

    void setColor(sf::Color newColor) { _color = newColor; }

    [[nodiscard]] double distance(const Vec3D &vec) const { return norm().dot(Vec3D(_points[0]) - vec); }
};
 */
