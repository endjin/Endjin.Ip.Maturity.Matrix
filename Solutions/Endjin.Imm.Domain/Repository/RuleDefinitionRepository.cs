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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "RCS1077:Optimize LINQ method call.", Justification = "Very minor perf impact doesn't justify clumsier code")]
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