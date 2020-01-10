namespace Endjin.Imm.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    [DebuggerDisplay("RuleDefinition = {Name}, {Measures.Count} Measures")]
    public partial class RuleDefinition
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("dataType", ItemConverterType = typeof(StringEnumConverter))]
        public DataType DataType { get; set; }

        [JsonProperty("measures")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "YamlDotNet.Serialization doesn't know how to deserialize a collection into a pre-initialized read-only property")]
        public IList<Measure> Measures { get; set; } = new List<Measure>();
    }
}