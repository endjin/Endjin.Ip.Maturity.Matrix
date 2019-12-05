namespace Endjin.Imm.Processing
{
    using Endjin.Imm.Contracts;
    using Endjin.Imm.Domain;

    public class NullRuleCalculator : IRuleCalculator
    {
        public decimal Percentage(Rule rule, IEvaluationContext context)
        {
            return 0m;
        }

        public long Score(Rule rule, IEvaluationContext context)
        {
            return 0;
        }
    }
}