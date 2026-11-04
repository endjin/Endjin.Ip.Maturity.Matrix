namespace Endjin.Imm.Domain
{
    using System.Collections.Generic;

    public partial class IpMaturityModelRuleset
    {
        public IpMaturityModelRuleset()
            : this(new List<RuleDefinition>())
        {
        }

        private IpMaturityModelRuleset(IList<RuleDefinition> rules)
        {
            this.Rules = rules;
        }

        public IList<RuleDefinition> Rules { get; }
    }
}