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
        public string Description { get; set; } = "";

        /// <summary>
        /// Gets or sets a value indicating whether there are situations in which this measure
        /// might not be applicable, meaning certain projects may need to opt out of it in their
        /// IMM.
        /// </summary>
        /// <remarks>
        /// Opting out of a measure has two effects, one on the badge rendered for the relevant
        /// rule, and one for the badge rendered to show the overall IMM score for the project.
        /// With the per-rule badge, the percentage will be calculated to exclude any measures
        /// that the project has opted out of. For example, if a particular rule definition defines
        /// measures that make a maximum score of 3 possible, but if one of those measures might
        /// not apply to all projects, a project that opts out of that can score 100% with just 2
        /// points on that rule, and 50% with 1 point. (Without the ability to opt out, these would
        /// show as 67% and 33% respectively.) Also, when a project opts out of specific measures,
        /// those do not contribute to the calcluation for the maximum total number of points
        /// achievable for an IMM. This means that projects for which a measure is not applicable
        /// do not get their IMM score distorted. (E.g., a project could conceivably get a perfect
        /// IMM score without scoring any points on a measure that it has legitimately opted out of.)
        /// </remarks>
        [JsonProperty("canOptOut")]
        public bool CanOptOut { get; set; }
    }
}