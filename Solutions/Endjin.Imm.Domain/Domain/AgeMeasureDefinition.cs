using Newtonsoft.Json;

namespace Endjin.Imm.Domain
{
    public class AgeMeasureDefinition : MeasureDefinition
    {
        [JsonProperty("age")]
        public string Age { get; set; } = "";
    }
}
