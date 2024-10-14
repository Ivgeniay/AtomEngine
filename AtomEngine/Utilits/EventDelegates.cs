using AtomEngine.Math; 

namespace AtomEngine.Utilits
{
    internal delegate void VoidDelegate();
    internal delegate void IntDelegate(int value);
    internal delegate void DoubleDelegate(double value);
    internal delegate void FloatDelegate(float value);
    internal delegate void StringDelegate(string value);
    internal delegate void BoolDelegate(bool value);
    internal delegate void ObjectDelegate(object value);
    internal delegate void Vector2DDelegate(Vector2D<int> value);
}
