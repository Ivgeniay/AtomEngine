using EngineLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtomEngine
{
    [AttributeUsage(AttributeTargets.Field)]
    [Documentation(
    DocumentationSection = "Core",
    Name = "IgnoreChangingSceneAttribute",
    SubSection = "Attribute/Inspector",
    Description = @"
    Hides the field in the inspector.

    namespace AtomEngine
    IgnoreChangingSceneAttribute()

    This attribute is used to exclude the influence of value changes in the scene window.

    Usage examples:
    public struct CameraComponent : IComponent
    {
        [DefaultBool(true)]
        [IgnoreChangingScene]
        public bool IsActive;
    }
    ",
    Author = "AtomEngine Team")]
    public class IgnoreChangingSceneAttribute : Attribute { }
}
