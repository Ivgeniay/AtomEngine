using System.Text;
using System.Text.Json.Nodes;

namespace AtomEngine.Serialize
{

    public interface ISerializable
    {
        public JsonObject OnSerialize();
        public void OnDeserialize(JsonObject json);
    }
    internal interface ISerializable<T> : ISerializable
    { 
    }

    //public static void Serialize(ISerializable obj, Stream stream)
    //{
    //    using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, true))
    //    {
    //        obj.OnSerialize(writer);
    //    }
    //}

    //public static T Deserialize<T>(Stream stream) where T : ISerializable, new()
    //{
    //    T obj = new T();
    //    using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, true))
    //    {
    //        obj.OnDeserialize(reader);
    //    }
    //    return obj;
    //}
}
