namespace OpenglLib
{
    public class RSFileInfo
    {
        public string SourcePath { get; set; } = string.Empty;
        public string SourceFolder { get; set; } = string.Empty;
        public string InterfaceName { get; set; } = string.Empty;
        public string ComponentName { get; set; } = string.Empty;
        public string SystemName { get; set; } = string.Empty;
        public string ProcessedCode { get; set; } = string.Empty;
        public List<GlslConstantModel> Constants { get; set; } = new List<GlslConstantModel>();
        public List<UniformBlockModel> UniformBlocks { get; set; } = new List<UniformBlockModel>();
        public List<UniformModel> Uniforms { get; set; } = new List<UniformModel>();
        public List<GlslStructureModel> Structures { get; set; } = new List<GlslStructureModel>();
        public List<GlslMethodInfo> Methods { get; set; } = new List<GlslMethodInfo>();
        public List<string> RequiredComponent { get; set; } = new List<string>();
    }
}
