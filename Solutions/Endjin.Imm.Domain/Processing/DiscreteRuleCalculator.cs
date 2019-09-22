namespace Endjin.Imm.Processing
{
    using Endjin.Imm.Contracts;
    using Endjin.Imm.Domain;
    using System;
    using System.Linq;

    public class DiscreteRuleCalculator : IRuleCalculator
    {
        private readonly IRuleDefinitionRepository rdr;

        public DiscreteRuleCalculator(IRuleDefinitionRepository rdr)
        {
            this.rdr = rdr;
        }

        public decimal Percentage(Rule rule)
        {
            var definition = this.rdr.Get(rule);
            var higestScore = definition.Measures.LastOrDefault().Score;

            return Math.Round((Convert.ToDecimal(this.Score(rule)) / Convert.ToDecimal(higestScore)) * 100);
        }

        public long Score(Rule rule)
        {
            return rule.Measures.FirstOrDefault().Score;
        }
    }
}