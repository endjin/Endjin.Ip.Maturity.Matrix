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
        public string Description { get; set; }
    }
}