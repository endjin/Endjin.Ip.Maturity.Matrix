namespace Endjin.Imm.Domain
{
    using Newtonsoft.Json;

    /// <summary>
    /// Defines a measure type that may be used in an IP Maturity Matrix inside a particular rule type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// We use this when loading the rule set definition from <see cref="IpMaturityMatrixRuleset.RuleSetDefinitionsUrl"/>.
    /// Each entry in that file deserializes to a <see cref="RuleDefinition"/>, with a <see cref="RuleDefinition.Measures"/>
    /// property containing a collection of <see cref="MeasureDefinition"/>s.
    /// </para>
    /// </remarks>
    public class MeasureDefinition
    {
        [JsonProperty("score")]
        public long Score { get; set; }

        [JsonProperty("description")]
        public string? Description { get; set; }
    }
}