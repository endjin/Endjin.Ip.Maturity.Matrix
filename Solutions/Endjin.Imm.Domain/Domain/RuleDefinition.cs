namespace Endjin.Imm.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Defines a rule type that may be used in an IP Maturity Matrix.
    /// </summary>
    /// <remarks>
    /// <para>
    /// We use this when loading the rule set definition from <see cref="IpMaturityMatrixRuleset.RuleSetDefinitionsUrl"/>.
    /// Each entry in that file deserializes to one of these.
    /// </para>
    /// <para>
    /// Each individual project with an IP Maturity Matrix will have an <c>imm.yaml</c> file, and that does
    /// <em>not</em> correspond to this type. Each entry in a project's matrix gets loaded into a <see cref="RuleAssertion"/>.
    /// Every <see cref="RuleAssertion"/> should be associated with a <see cref="RuleDefinition"/>, based on the
    /// <see cref="Id"/> property.
    /// </para>
    /// </remarks>
    [DebuggerDisplay("RuleDefinition = {Name}, {Measures.Count} Measures")]
    public partial class RuleDefinition
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("dataType", ItemConverterType = typeof(StringEnumConverter))]
        public DataType DataType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether there are situations in which this rule might
        /// not be applicable, meaning certain projects may need to opt out of it in their IMM.
        /// </summary>
        /// <remarks>
        /// Opting out of a rule reduces the maximum total number of points achievable for an IMM.
        /// This has an impact on the IMM summary score badge rendering, which shows the number of
        /// points achieved next to the maximum possible, and also colour codes the badge based on
        /// what proportion of the maximum score has been achieved. The ability to opt out means
        /// that projects for which a rule is not applicable do not get their IMM score
        /// distorted. (E.g., a project could conceivably get a perfect IMM score without scoring
        /// any points on a rule that it has legitimately opted out of.)
        /// </remarks>
        [JsonProperty("canOptOut")]
        public bool CanOptOut { get; set; }

        [JsonProperty("measures")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "YamlDotNet.Serialization doesn't know how to deserialize a collection into a pre-initialized read-only property")]
        public IList<MeasureDefinition> Measures { get; set; } = new List<MeasureDefinition>();
    }
}