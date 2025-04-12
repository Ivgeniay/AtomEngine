using Silk.NET.Maths;

namespace OpenglLib
{
    public interface IViewRender
    {
        Matrix4X4<float> model { set; }
        Matrix4X4<float> view { set; }
        Matrix4X4<float> projection { set; }
    }
}
