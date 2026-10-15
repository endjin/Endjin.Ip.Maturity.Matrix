namespace Endjin.Imm.Domain
{
    using System.Collections.Generic;

    public partial class IpMaturityModel
    {
        public IpMaturityModel()
            : this(new List<RuleAssertion>())
        {
        }

        private IpMaturityModel(IList<RuleAssertion> rules)
        {
            this.Rules = rules;
        }

        public IList<RuleAssertion> Rules { get; }
    }
}