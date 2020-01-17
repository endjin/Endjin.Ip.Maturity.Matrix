namespace Endjin.Imm.Domain
{
    using Newtonsoft.Json;
    using System.Collections.Generic;

    public partial class IpMaturityMatrix
    {
        public IpMaturityMatrix()
            : this(new List<RuleAssertion>())
        {
        }

        private IpMaturityMatrix(IList<RuleAssertion> rules)
        {
            this.Rules = rules;
        }

        [JsonProperty("rules")]
        public IList<RuleAssertion> Rules { get; }
    }
}