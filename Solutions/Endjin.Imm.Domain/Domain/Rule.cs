namespace Endjin.Imm.Domain
{
    using System;
    using System.Diagnostics;
    using Newtonsoft.Json;

    [DebuggerDisplay("Name = {Name}")]
    public partial class Rule
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("measures")]
        public Measure[] Measures { get; set; }
    }
}