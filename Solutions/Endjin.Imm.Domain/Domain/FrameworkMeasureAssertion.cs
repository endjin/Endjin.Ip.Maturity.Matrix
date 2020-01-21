using Newtonsoft.Json;
using System.Diagnostics;

namespace Endjin.Imm.Domain
{
    /// <summary>
    /// An entry from an <c>imm.yaml</c> file, in which a particular project is asserting its
    /// measures against a <see cref="FrameworkMeasureDefinition"/>-style measure definition.
    /// </summary>
    [DebuggerDisplay("Framework = {Framework}, {Description}")]
    public class FrameworkMeasureAssertion : MeasureAssertion
    {
        [JsonProperty("framework")]
        public string Framework { get; set; } = "";
    }
}
