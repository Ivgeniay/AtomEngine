using System.Collections.Generic;
using OpenglLib;

namespace Editor
{
    public class RSFileInfo
    {
        public string SourcePath { get; set; } = string.Empty;
        public string SourceFolder { get; set; } = string.Empty;
        public string InterfaceName { get; set; } = string.Empty;
        public string ProcessedCode { get; set; } = string.Empty;
        public List<UniformBlockStructure> UniformBlocks { get; set; } = new List<UniformBlockStructure>();
        public List<(string type, string name, int? arraySize)> Uniforms { get; set; } = new List<(string type, string name, int? arraySize)>();
        public List<GlslStructure> Structures { get; set; } = new List<GlslStructure>();
        public List<string> Methods { get; set; } = new List<string>();
        public List<string> RequiredComponent { get; set; } = new List<string>();
    }
}
