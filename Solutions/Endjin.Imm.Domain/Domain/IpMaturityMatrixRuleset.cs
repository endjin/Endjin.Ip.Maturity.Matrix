namespace Endjin.Imm.Domain
{
    using Newtonsoft.Json;

    public partial class IpMaturityMatrixRuleset
    {
        [JsonProperty("rules")]
        public RuleDefinition[] Rules { get; set; }
    }
}