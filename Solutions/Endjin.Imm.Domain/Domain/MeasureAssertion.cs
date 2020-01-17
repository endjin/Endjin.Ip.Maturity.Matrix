namespace Endjin.Imm.Domain
{
    using Newtonsoft.Json;
    using System.Diagnostics;

    /// <summary>
    /// An entry inside a rule assertion from an <c>imm.yaml</c> file, in which a particular project is asserting
    /// one particular measure against a particular rule.
    /// </summary>
    [DebuggerDisplay("Score = {Score}, {Description}")]
    public partial class MeasureAssertion
    {
        [JsonProperty("score")]
        public long Score { get; set; }

        [JsonProperty("description")]
        public string? Description { get; set; }
    }
}