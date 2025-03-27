namespace OpenglLib
{
    public class RSFileInfo
    {
        public string SourcePath { get; set; } = string.Empty;
        public string SourceFolder { get; set; } = string.Empty;
        public string InterfaceName { get; set; } = string.Empty;
        public string ProcessedCode { get; set; } = string.Empty;
        public List<UniformBlockStructure> UniformBlocks { get; set; } = new List<UniformBlockStructure>();
        public List<UniformField> Uniforms { get; set; } = new List<UniformField>();
        public List<GlslStructure> Structures { get; set; } = new List<GlslStructure>();
        public List<string> Methods { get; set; } = new List<string>();
        public List<string> RequiredComponent { get; set; } = new List<string>();
    }
}
