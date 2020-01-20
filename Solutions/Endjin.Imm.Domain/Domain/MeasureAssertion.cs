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
        public string Description { get; set; } = "";

        /// <summary>
        /// Gets or sets a value indicating whether the project has opted out of this particular
        /// measure.
        /// </summary>
        /// <remarks>
        /// This may be true only if the corresponding <see cref="MeasureDefinition.CanOptOut"/>
        /// property is true.
        /// </remarks>
        [JsonProperty("optOut")]
        public bool OptOut { get; set; }
    }
}