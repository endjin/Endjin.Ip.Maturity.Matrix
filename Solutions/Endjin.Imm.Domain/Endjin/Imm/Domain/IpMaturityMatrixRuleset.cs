namespace Endjin.Imm.Domain
{
    using System.Collections.Generic;

    public partial class IpMaturityMatrixRuleset
    {
        public IpMaturityMatrixRuleset()
            : this(new List<RuleDefinition>())
        {
        }

        private IpMaturityMatrixRuleset(IList<RuleDefinition> rules)
        {
            this.Rules = rules;
        }

        public IList<RuleDefinition> Rules { get; }
    }
}