namespace Endjin.Imm.Processing
{
    using Endjin.Imm.Contracts;
    using Endjin.Imm.Domain;
    using System;
    using System.Linq;

    public class ContinuousRuleCalculator : IRuleCalculator
    {
        private readonly IRuleDefinitionRepository rdr;

        public ContinuousRuleCalculator(IRuleDefinitionRepository rdr)
        {
            this.rdr = rdr;
        }

        public decimal Percentage(RuleAssertion rule, IEvaluationContext context)
        {
            var definition = this.rdr.GetDefinitionFor(rule);
            var higestScore = definition.Measures.Sum(x => x.Score);

            return Math.Round(Convert.ToDecimal(this.Score(rule, context) / Convert.ToDecimal(higestScore)) * 100);
        }

        public long Score(RuleAssertion rule, IEvaluationContext context)
        {
            return rule.Measures.Sum(x => x.Score);
        }
    }
}