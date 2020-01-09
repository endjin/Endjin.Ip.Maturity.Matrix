namespace Endjin.Imm.Domain
{
    public class RuleEvaluation
    {
        public RuleEvaluation(Rule rule, decimal percentage, long score)
        {
            Rule = rule;
            Percentage = percentage;
            Score = score;
        }

        public Rule Rule { get; }

        public decimal Percentage { get; }

        public long Score { get; }
    }
}