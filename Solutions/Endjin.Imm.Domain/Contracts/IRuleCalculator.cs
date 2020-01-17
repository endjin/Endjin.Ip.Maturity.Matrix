namespace Endjin.Imm.Contracts
{
    using Endjin.Imm.Domain;

    public interface IRuleCalculator
    {
        decimal Percentage(RuleAssertion rule, IEvaluationContext context);

        long Score(RuleAssertion rule, IEvaluationContext context);
    }
}