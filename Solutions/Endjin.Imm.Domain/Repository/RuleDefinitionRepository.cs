namespace Endjin.Imm.Repository
{
    using Endjin.Imm.Contracts;
    using Endjin.Imm.Domain;
    using System.Collections.Generic;
    using System.Linq;

    public class RuleDefinitionRepository : IRuleDefinitionRepository
    {
        private readonly IpMaturityMatrixRuleset ruleSet;

        public RuleDefinitionRepository(IpMaturityMatrixRuleset ruleSet)
        {
            this.ruleSet = ruleSet;
        }

        public RuleDefinition GetDefinitionFor(Rule rule)
        {
            return this.ruleSet.Rules.FirstOrDefault(x => x.Id == rule.Id);
        }

        public IList<RuleDefinition> GetAll()
        {
            return this.ruleSet.Rules;
        }
    }
}