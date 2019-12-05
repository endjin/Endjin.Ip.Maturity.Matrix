namespace Endjin.Imm.Contracts
{
    using Endjin.Imm.Domain;

    public interface IRuleCalculator
    {
        decimal Percentage(Rule rule, IEvaluationContext context);

        long Score(Rule rule, IEvaluationContext context);
    }
}