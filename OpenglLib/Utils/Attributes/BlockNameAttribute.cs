using EngineLib;

namespace OpenglLib
{
    [Documentation(
        Author = "AtomEngine Team",
        Description = "Kek",
        Name = "BlockNameAttribute",
        DocumentationSection = "Engine",
        SubSection = "Attributes")]
    public class BlockNameAttribute : Attribute
    {
        public string BlockName { get; }
        public BlockNameAttribute(string blockName)
        {
            BlockName = blockName;
        }
    }
}
