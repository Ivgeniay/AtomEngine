using System.Numerics;
using System;

namespace Editor.NodeSpace
{
    public class NodeConnection
    {
        public NodePort OutputPort { get; set; }
        public NodePort InputPort { get; set; }


        public Vector4 Color { get; set; } = new Vector4(0.7f, 0.7f, 0.7f, 1.0f);
        public float Thickness { get; set; } = 2.0f;
        public bool IsDashed { get; set; } = false;


        public Vector2 StartTangent { get; set; }
        public Vector2 EndTangent { get; set; }


        public Vector2 GetStartPosition()
        {
            return OutputPort?.GetAbsolutePosition() ?? Vector2.Zero;
        }


        public Vector2 GetEndPosition()
        {
            return InputPort?.GetAbsolutePosition() ?? Vector2.Zero;
        }


        public void CalculateBezierPoints()
        {
            Vector2 start = GetStartPosition();
            Vector2 end = GetEndPosition();

            float dx = Math.Abs(end.X - start.X);
            float dy = Math.Abs(end.Y - start.Y);

            float tangentOffset = Math.Max(dx * 0.5f, 50f);

            if (dx < 100)
            {
                tangentOffset = Math.Max(100f, tangentOffset);
            }

            StartTangent = new Vector2(start.X + tangentOffset, start.Y);
            EndTangent = new Vector2(end.X - tangentOffset, end.Y);
        }

        //public void CalculateBezierPoints()
        //{
        //    Vector2 start = GetStartPosition();
        //    Vector2 end = GetEndPosition();

        //    float tangentOffset = Math.Abs(end.X - start.X) * 0.5f;
        //    tangentOffset = Math.Max(tangentOffset, 50f);

        //    StartTangent = new Vector2(start.X + tangentOffset, start.Y);
        //    EndTangent = new Vector2(end.X - tangentOffset, end.Y);
        //}
    }
}
