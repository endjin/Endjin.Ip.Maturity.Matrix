namespace Endjin.Imm.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Newtonsoft.Json;

    /// <summary>
    /// An entry from an <c>imm.yaml</c> file, in which a particular project is asserting its
    /// measures against a particular rule.
    /// </summary>
    [DebuggerDisplay("Name = {Name}")]
    public partial class RuleAssertion
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("measures")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "YamlDotNet.Serialization doesn't know how to deserialize a collection into a pre-initialized read-only property")]
        public IList<MeasureAssertion> Measures { get; set; } = new List<MeasureAssertion>();
    }
}