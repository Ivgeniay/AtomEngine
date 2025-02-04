using Newtonsoft.Json;

namespace OpenglLib
{
    internal class GlslTypeModel
    {
        [JsonProperty("gl_code")]
        public int GlCode { get; set; }

        [JsonProperty("gl_define")]
        public string? GlDefine { get; set; }

        [JsonProperty("glsl_mark")]
        public string? GlslMark { get; set; }

        [JsonProperty("np_type")]
        public string? NpType { get; set; }

        [JsonProperty("std140")]
        public int Std140 { get; set; }

        [JsonProperty("std430")]
        public int Std430 { get; set; }

        [JsonProperty("default")]
        public object? Default { get; set; }

        [JsonProperty("version")]
        public double Version { get; set; }

        public override string ToString() => JsonConvert.SerializeObject(this);
    }
}
