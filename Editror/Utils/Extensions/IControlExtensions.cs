using Avalonia.VisualTree;
using Avalonia.Controls;
using System;


namespace Editor
{
    internal static class IControlExtensions
    {
        public static T FindDescendantOfType<T>(this Control control) where T : Control
        {
            return control.FindDescendantOfType<T>(x => true);
        }

        public static T FindDescendantOfType<T>(this Control control, Func<T, bool> predicate) where T : Control
        {
            if (control is T controlAsT && predicate(controlAsT))
            {
                return controlAsT;
            }

            foreach (var child in control.GetVisualChildren())
            {
                if (child is T childAsT && predicate(childAsT))
                {
                    return childAsT;
                }

                if (child is Control childControl)
                {
                    var result = childControl.FindDescendantOfType<T>(predicate);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            return default;
        }
    }
}
