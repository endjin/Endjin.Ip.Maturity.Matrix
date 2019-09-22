namespace Endjin.Imm.Contracts
{
    using Endjin.Imm.Domain;

    public interface IRuleCalculator
    {
        decimal Percentage(Rule rule);

        long Score(Rule rule);
    }
}