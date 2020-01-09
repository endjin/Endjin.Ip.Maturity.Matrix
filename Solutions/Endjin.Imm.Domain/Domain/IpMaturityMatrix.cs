namespace Endjin.Imm.Domain
{
    using Newtonsoft.Json;
    using System.Collections.Generic;

    public partial class IpMaturityMatrix
    {
        public IpMaturityMatrix()
            : this(new List<Rule>())
        {
        }

        private IpMaturityMatrix(IList<Rule> rules)
        {
            this.Rules = rules;
        }

        [JsonProperty("rules")]
        public IList<Rule> Rules { get; }
    }
}