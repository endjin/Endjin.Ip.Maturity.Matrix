namespace Endjin.Imm.Domain
{
    using Newtonsoft.Json;
    using System.Diagnostics;

    [DebuggerDisplay("Score = {Score}, {Description}")]
    public partial class Measure
    {
        [JsonProperty("score")]
        public long Score { get; set; }

        [JsonProperty("description")]
        public string? Description { get; set; }

        // TODO: this gets icky for a few reasons
        // First, we've ended up merging all possible measure structures into a single type, when it
        // would be better to have differentiation - multiple derived types since this is C#. (In some
        // other language we could use sum types to reflect the variations.)
        // Second, the existing model uses this Measure type both in the RuleDefinition and the Rule, which
        // made sense when it was written, but with the introduction of age types, the definition and the
        // IMM entry now look different. But we've got both the Age (which is what appears in the rule
        // definition) and the Date (which is what appears in the IMM instances) in the same type.
        [JsonProperty("age")]
        public string? Age { get; set; }

        [JsonProperty("date")]
        public string? Date { get; set; }

        [JsonProperty("framework")]
        public string? Framework { get; set; }
    }
}