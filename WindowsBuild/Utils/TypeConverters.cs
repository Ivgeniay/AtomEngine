namespace WindowsBuild
{
    public static class TypeConverters
    {
        public static object ConvertToTyped(object value)
        {
            if (value == null)
                return null;

            if (value is Newtonsoft.Json.Linq.JObject jObj)
            {
                if (jObj["X"] != null && jObj["Y"] != null)
                {
                    if (jObj["Z"] != null)
                    {
                        if (jObj["W"] != null)
                        {
                            float x4 = jObj["X"].ToObject<float>();
                            float y4 = jObj["Y"].ToObject<float>();
                            float z4 = jObj["Z"].ToObject<float>();
                            float w4 = jObj["W"].ToObject<float>();
                            return new Silk.NET.Maths.Vector4D<float>(x4, y4, z4, w4);
                        }
                        else
                        {
                            float x3 = jObj["X"].ToObject<float>();
                            float y3 = jObj["Y"].ToObject<float>();
                            float z3 = jObj["Z"].ToObject<float>();
                            return new Silk.NET.Maths.Vector3D<float>(x3, y3, z3);
                        }
                    }
                    else
                    {
                        float x2 = jObj["X"].ToObject<float>();
                        float y2 = jObj["Y"].ToObject<float>();
                        return new Silk.NET.Maths.Vector2D<float>(x2, y2);
                    }
                }
                else if (jObj["Values"] != null)
                {
                    var values = jObj["Values"].ToObject<float[]>();
                    if (values.Length == 16)
                    {
                        return new Silk.NET.Maths.Matrix4X4<float>(
                            values[0], values[1], values[2], values[3],
                            values[4], values[5], values[6], values[7],
                            values[8], values[9], values[10], values[11],
                            values[12], values[13], values[14], values[15]
                        );
                    }
                    else if (values.Length == 9)
                    {
                        return new Silk.NET.Maths.Matrix3X3<float>(
                            values[0], values[1], values[2],
                            values[3], values[4], values[5],
                            values[6], values[7], values[8]
                        );
                    }
                    else if (values.Length == 4)
                    {
                        return new Silk.NET.Maths.Matrix2X2<float>(
                            values[0], values[1],
                            values[2], values[3]
                        );
                    }
                }

                if (jObj["$type"] != null)
                {
                    string typeName = jObj["$type"].ToString();
                    if (typeName.Contains("Vector3"))
                    {
                        float x = jObj["X"]?.ToObject<float>() ?? 0f;
                        float y = jObj["Y"]?.ToObject<float>() ?? 0f;
                        float z = jObj["Z"]?.ToObject<float>() ?? 0f;
                        return new Silk.NET.Maths.Vector3D<float>(x, y, z);
                    }
                    else if (typeName.Contains("Vector2"))
                    {
                        float x = jObj["X"]?.ToObject<float>() ?? 0f;
                        float y = jObj["Y"]?.ToObject<float>() ?? 0f;
                        return new Silk.NET.Maths.Vector2D<float>(x, y);
                    }
                    else if (typeName.Contains("Vector4"))
                    {
                        float x = jObj["X"]?.ToObject<float>() ?? 0f;
                        float y = jObj["Y"]?.ToObject<float>() ?? 0f;
                        float z = jObj["Z"]?.ToObject<float>() ?? 0f;
                        float w = jObj["W"]?.ToObject<float>() ?? 0f;
                        return new Silk.NET.Maths.Vector4D<float>(x, y, z, w);
                    }
                }

                return jObj;
            }

            return value;
        }

        public static object ConvertValueToTargetType(object value, Type targetType)
        {
            if (value == null) return null;
            if (targetType.IsInstanceOfType(value)) return value;

            if (value is Newtonsoft.Json.Linq.JObject jObject)
            {
                if (targetType == typeof(System.Numerics.Vector2) || targetType == typeof(Silk.NET.Maths.Vector2D<float>))
                {
                    float x = jObject["X"]?.ToObject<float>() ?? 0f;
                    float y = jObject["Y"]?.ToObject<float>() ?? 0f;

                    if (targetType == typeof(System.Numerics.Vector2))
                        return new System.Numerics.Vector2(x, y);
                    else
                        return new Silk.NET.Maths.Vector2D<float>(x, y);
                }
                else if (targetType == typeof(System.Numerics.Vector3) || targetType == typeof(Silk.NET.Maths.Vector3D<float>))
                {
                    float x = jObject["X"]?.ToObject<float>() ?? 0f;
                    float y = jObject["Y"]?.ToObject<float>() ?? 0f;
                    float z = jObject["Z"]?.ToObject<float>() ?? 0f;

                    if (targetType == typeof(System.Numerics.Vector3))
                        return new System.Numerics.Vector3(x, y, z);
                    else
                        return new Silk.NET.Maths.Vector3D<float>(x, y, z);
                }
                else if (targetType == typeof(System.Numerics.Vector4) || targetType == typeof(Silk.NET.Maths.Vector4D<float>))
                {
                    float x = jObject["X"]?.ToObject<float>() ?? 0f;
                    float y = jObject["Y"]?.ToObject<float>() ?? 0f;
                    float z = jObject["Z"]?.ToObject<float>() ?? 0f;
                    float w = jObject["W"]?.ToObject<float>() ?? 0f;

                    if (targetType == typeof(System.Numerics.Vector4))
                        return new System.Numerics.Vector4(x, y, z, w);
                    else
                        return new Silk.NET.Maths.Vector4D<float>(x, y, z, w);
                }
            }
            if (targetType.IsPrimitive || targetType == typeof(string))
            {
                return Convert.ChangeType(value, targetType);
            }

            return value;
        }

    }


}
