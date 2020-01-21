using Newtonsoft.Json;

namespace Endjin.Imm.Domain
{
    public class FrameworkMeasureDefinition : MeasureDefinition
    {
        [JsonProperty("framework")]
        public string Framework { get; set; } = "";
    }
}
