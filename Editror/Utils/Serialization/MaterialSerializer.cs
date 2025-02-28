using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Silk.NET.Maths;
using OpenglLib;
using System.Numerics;

namespace Editor
{
    public static class MaterialSerializer
    {
        private static readonly HashSet<Type> SpecialTypes = new HashSet<Type>
        {
            typeof(Vector2),
            typeof(Vector3),
            typeof(Vector4),
            typeof(Vector2D<float>),
            typeof(Vector3D<float>),
            typeof(Vector4D<float>),
            typeof(Matrix2X2<float>),
            typeof(Matrix3X3<float>),
            typeof(Matrix4X4<float>)
        };

        public static string SerializeMaterial(MaterialAsset material)
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.Auto,
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            var serializableData = new
            {
                material.ShaderRepresentationGuid,
                material.ShaderRepresentationTypeName,
                material.Name,
                UniformValues = ConvertUniformValuesToSerializable(material.UniformValues),
                material.TextureReferences
            };

            return JsonConvert.SerializeObject(serializableData, settings);
        }

        public static MaterialAsset DeserializeMaterial(string json)
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            };

            var deserializedData = JsonConvert.DeserializeObject<MaterialAsset>(json, settings);

            if (deserializedData.UniformValues != null)
            {
                deserializedData.UniformValues = ConvertUniformValuesToTyped(deserializedData.UniformValues);
            }

            return deserializedData;
        }

        private static Dictionary<string, object> ConvertUniformValuesToSerializable(Dictionary<string, object> values)
        {
            var result = new Dictionary<string, object>();

            if (values == null)
                return result;

            foreach (var pair in values)
            {
                result[pair.Key] = ConvertToSerializable(pair.Value);
            }

            return result;
        }

        private static Dictionary<string, object> ConvertUniformValuesToTyped(Dictionary<string, object> values)
        {
            var result = new Dictionary<string, object>();

            if (values == null)
                return result;

            foreach (var pair in values)
            {
                result[pair.Key] = ConvertToTyped(pair.Value);
            }

            return result;
        }

        private static object ConvertToSerializable(object value)
        {
            if (value == null)
                return null;

            Type type = value.GetType();

            if (!SpecialTypes.Contains(type))
                return value;

            if (type == typeof(Vector3) || type == typeof(Vector3D<float>))
            {
                if (value is Vector3 v3)
                    return v3;
                else
                    return ((Vector3D<float>)value).ToNumetrix();
            }
            else if (type == typeof(Vector2) || type == typeof(Vector2D<float>))
            {
                if (value is Vector2 v2)
                    return v2;
                else
                    return ((Vector2D<float>)value).ToNumetrix();
            }
            else if (type == typeof(Vector4) || type == typeof(Vector4D<float>))
            {
                if (value is Vector4 v4)
                    return v4;
                else
                    return ((Vector4D<float>)value).ToNumetrix();
            }
            else if (type == typeof(Matrix4X4<float>))
            {
                return ((Matrix4X4<float>)value).ToNumetrix();
            }

            return value;
        }

        private static object ConvertToTyped(object value)
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
                            return new Vector4(x4, y4, z4, w4);
                        }
                        else
                        {
                            float x3 = jObj["X"].ToObject<float>();
                            float y3 = jObj["Y"].ToObject<float>();
                            float z3 = jObj["Z"].ToObject<float>();
                            return new Vector3(x3, y3, z3);
                        }
                    }
                    else
                    {
                        float x2 = jObj["X"].ToObject<float>();
                        float y2 = jObj["Y"].ToObject<float>();
                        return new Vector2(x2, y2);
                    }
                }
                else if (jObj["Values"] != null)
                {
                    var values = jObj["Values"].ToObject<float[]>();
                    if (values.Length == 16)
                    {
                        return new Matrix4X4<float>(
                            values[0], values[1], values[2], values[3],
                            values[4], values[5], values[6], values[7],
                            values[8], values[9], values[10], values[11],
                            values[12], values[13], values[14], values[15]
                        );
                    }
                }

                return jObj;
            }

            return value;
        }

    }
}
