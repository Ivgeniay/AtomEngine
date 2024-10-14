namespace AtomEngine.Math
{
    public struct Matrix4x4
    {
        private double[,] values;

        public Matrix4x4() { values = new double[4, 4]; }
        public Matrix4x4(double[,] values)
        {
            if (values.GetLength(0) != 4 || values.GetLength(1) != 4) throw new ArgumentException("Matrix must be 4x4");
            this.values = values;
        }
        public Matrix4x4(float[,] values)
        {
            this.values = new double[4, 4];
            if (values.GetLength(0) != 4 || values.GetLength(1) != 4) throw new ArgumentException("Matrix must be 4x4"); 
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    this.values[i, j] = values[i, j];
        }

        public static Matrix4x4 operator *(Matrix4x4 a, Matrix4x4 b)
        {
            Matrix4x4 result = new Matrix4x4();
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    for (int k = 0; k < 4; k++)
                        result.values[i, j] += a.values[i, k] * b.values[k, j];
            return result;
        }
        public static Matrix4x4 Zero() => Constant(0); 
        public static Matrix4x4 Identity()
        {
            double[,] values = new double[4, 4];

            values[0, 0] = 1;
            values[1, 1] = 1;
            values[2, 2] = 1;
            values[3, 3] = 1;

            return new Matrix4x4(values);
        }
        public static Matrix4x4 Constant(double value)
        {
            double[,] values = new double[4, 4];

            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    values[i, j] = value;

            return new Matrix4x4(values);
        }
        public static Matrix4x4 Scale(Vector3D factor)
        {
            double[,] values = new double[4, 4];

            values[0, 0] = factor.X;
            values[1, 1] = factor.Y;
            values[2, 2] = factor.Z;
            values[3, 3] = 1;

            return new Matrix4x4(values);
        }
        public static Matrix4x4 Translation(Vector3D v)
        {
            double[,] values = Identity().values;

            values[0, 3] = v.X;
            values[1, 3] = v.Y;
            values[2, 3] = v.Z;

            return new Matrix4x4(values);
        }
        public static Matrix4x4 Rotation(Vector3D r) => RotationZ(r.X) * RotationY(r.Y) * RotationX(r.Z);
        public static Matrix4x4 RotationX(double rx)
        {
            double cos = System.Math.Cos(rx);
            double sin = System.Math.Sin(rx);

            double[,] values = Identity().values;

            values[0, 0] = 1;

            values[1, 1] = cos;
            values[1, 2] = -sin;
            values[2, 1] = sin;
            values[2, 2] = cos;

            values[3, 3] = 1;

            return new Matrix4x4(values);
        }
        public static Matrix4x4 RotationY(double ry)
        {
            double cos = System.Math.Cos(ry);
            double sin = System.Math.Sin(ry);

            double[,] values = Identity().values;

            values[1, 1] = 1;

            values[0, 0] = cos;
            values[0, 2] = sin;
            values[2, 0] = -sin;
            values[2, 2] = cos;

            values[3, 3] = 1;

            return new Matrix4x4(values);
        }
        public static Matrix4x4 RotationZ(double rz)
        {
            double cos = System.Math.Cos(rz);
            double sin = System.Math.Sin(rz);

            double[,] values = Identity().values;

            values[2, 2] = 1;

            values[0, 0] = cos;
            values[0, 1] = -sin;
            values[1, 0] = sin;
            values[1, 1] = cos;

            values[3, 3] = 1;

            return new Matrix4x4(values);
        } 
        public static Matrix4x4 Rotation(Vector3D v, double rv)
        {
            double cos = System.Math.Cos(rv);
            double sin = System.Math.Sin(rv);
            double oneMinusCos = 1 - cos;
            Vector3D nv = v.Normalized();

            double[,] values = new double[4, 4];

            values[0, 0] = cos + nv.X * nv.X * oneMinusCos;
            values[0, 1] = nv.X * nv.Y * oneMinusCos - nv.Z * sin;
            values[0, 2] = nv.X * nv.Z * oneMinusCos + nv.Y * sin;

            values[1, 0] = nv.Y * nv.X * oneMinusCos + nv.Z * sin;
            values[1, 1] = nv.Y * nv.Y * oneMinusCos + cos;
            values[1, 2] = nv.Y * nv.Z * oneMinusCos - nv.X * sin;

            values[2, 0] = nv.Z * nv.X * oneMinusCos - nv.Y * sin;
            values[2, 1] = nv.Z * nv.Y * oneMinusCos + nv.X * sin;
            values[2, 2] = nv.Z * nv.Z * oneMinusCos + cos;

            values[3, 3] = 1;

            return new Matrix4x4(values);
        }
        public static Matrix4x4 View(Vector3D left, Vector3D up, Vector3D lookAt, Vector3D eye)
        {
            Vector3D zAxis = (lookAt - eye).Normalized();
            //Vector3D xAxis = up.Cross(zAxis).Normalized();
            //Vector3D yAxis = zAxis.Cross(xAxis);
            Vector3D xAxis = Vector3D.Cross(up, zAxis).Normalized();
            Vector3D yAxis = Vector3D.Cross(zAxis, xAxis);

            float[,] values = new float[4, 4];
            values[0, 0] = (float)xAxis.X; values[0, 1] = (float)yAxis.X; values[0, 2] = (float)zAxis.X; values[0, 3] = 0;
            values[1, 0] = (float)xAxis.Y; values[1, 1] = (float)yAxis.Y; values[1, 2] = (float)zAxis.Y; values[1, 3] = 0;
            values[2, 0] = (float)xAxis.Z; values[2, 1] = (float)yAxis.Z; values[2, 2] = (float)zAxis.Z; values[2, 3] = 0;
            values[3, 0] = (float)-Vector3D.Dot(xAxis, eye);
            values[3, 1] = (float)-Vector3D.Dot(yAxis, eye);
            values[3, 2] = (float)-Vector3D.Dot(zAxis, eye);
            values[3, 3] = 1;

            return new Matrix4x4(values);
        } 
        public static Matrix4x4 Projection(double fov = 90.0, double aspect = 1.0, double zNear = 1.0, double zFar = 10.0)
        {
            double yScale = 1.0 /System.Math.Tan(fov * Constants.PI / 360.0);
            double xScale = yScale / aspect;
            double zRange = zFar - zNear;

            double[,] values = new double[4, 4];

            values[0, 0] = xScale;
            values[1, 1] = yScale;
            values[2, 2] = zFar/(zFar - zNear);
            values[2, 3] = (-zFar * zNear)/(zFar - zNear);

            values[3, 2] = 1;

            return new Matrix4x4(values);
        } 
        public static Matrix4x4 ScreenSpace(int width, int height)
        {
            double[,] values = new double[4, 4];

            values[0, 0] = -width / 2.0f;
            values[1, 1] = -height / 2.0f;
            values[2, 2] = 1;
            values[3, 3] = 1;
            values[0, 3] = width / 2.0f;
            values[1, 3] = height / 2.0f;

            return new Matrix4x4(values);
        }


        public Vector3D X() => new Vector3D(values[0, 0], values[1, 0], values[2, 0]);
        public Vector3D Y() => new Vector3D(values[0, 1], values[1, 1], values[2, 1]);
        public Vector3D Z() => new Vector3D(values[0, 2], values[1, 2], values[2, 2]);
        public Vector3D W() => new Vector3D(values[0, 3], values[1, 3], values[2, 3]);

        public static bool IsNear(double a, double b) => System.Math.Abs(a - b) < Constants.EPS; 
    }
}
