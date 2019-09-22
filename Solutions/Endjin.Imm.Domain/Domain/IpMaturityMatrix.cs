namespace Endjin.Imm.Domain
{
    using Newtonsoft.Json;

    public partial class IpMaturityMatrix
    {
        [JsonProperty("rules")]
        public Rule[] Rules { get; set; }
    }
}