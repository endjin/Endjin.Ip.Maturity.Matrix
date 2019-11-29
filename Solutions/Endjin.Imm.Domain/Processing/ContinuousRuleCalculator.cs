﻿namespace Endjin.Imm.Processing
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

        public decimal Percentage(Rule rule, IEvaluationContext context)
        {
            var definition = this.rdr.Get(rule);
            var higestScore = definition.Measures.Sum(x => x.Score);

            return Math.Round(Convert.ToDecimal(this.Score(rule, context) / Convert.ToDecimal(higestScore)) * 100);
        }

        public long Score(Rule rule, IEvaluationContext context)
        {
            return rule.Measures.Sum(x => x.Score);
        }
    }
}