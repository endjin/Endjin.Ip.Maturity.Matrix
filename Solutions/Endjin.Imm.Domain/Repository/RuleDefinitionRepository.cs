namespace Endjin.Imm.Repository
{
    using Endjin.Imm.Contracts;
    using Endjin.Imm.Domain;
    using System.Linq;

    public class RuleDefinitionRepository : IRuleDefinitionRepository
    {
        private readonly IpMaturityMatrixRuleset ruleSet;

        public RuleDefinitionRepository(IpMaturityMatrixRuleset ruleSet)
        {
            this.ruleSet = ruleSet;
        }

        public RuleDefinition Get(Rule rule)
        {
            return this.ruleSet.Rules.FirstOrDefault(x => x.Id == rule.Id);
        }

        public RuleDefinition[] GetAll()
        {
            return this.ruleSet.Rules;
        }
    }
}