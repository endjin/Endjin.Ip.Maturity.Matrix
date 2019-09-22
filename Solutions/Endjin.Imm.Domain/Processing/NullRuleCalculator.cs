namespace Endjin.Imm.Processing
{
    using Endjin.Imm.Contracts;
    using Endjin.Imm.Domain;

    public class NullRuleCalculator : IRuleCalculator
    {
        public decimal Percentage(Rule rule)
        {
            return 0m;
        }

        public long Score(Rule rule)
        {
            return 0;
        }
    }
}