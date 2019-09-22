namespace Endjin.Imm.Domain
{
    using System;
    using System.Diagnostics;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    [DebuggerDisplay("RuleDefinition = {Name}, {Measures.Length} Measures")]
    public partial class RuleDefinition
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("dataType", ItemConverterType = typeof(StringEnumConverter))]
        public DataType DataType { get; set; }

        [JsonProperty("measures")]
        public Measure[] Measures { get; set; }
    }
}