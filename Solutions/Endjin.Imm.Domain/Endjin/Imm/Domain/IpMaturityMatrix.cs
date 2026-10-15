namespace Endjin.Imm.Domain
{
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

        public IList<RuleAssertion> Rules { get; }
    }
}