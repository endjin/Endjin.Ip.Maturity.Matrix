using Newtonsoft.Json;
using System.Diagnostics;

namespace Endjin.Imm.Domain
{
    [DebuggerDisplay("Date = {Date}")]
    public class AgeMeasureAssertion : MeasureAssertion
    {
        [JsonProperty("date")]
        public string Date { get; set; } = "";
    }
}
