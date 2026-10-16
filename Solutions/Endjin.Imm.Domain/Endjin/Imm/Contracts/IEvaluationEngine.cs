namespace Endjin.Imm.Contracts
{
    using Endjin.Imm.Domain;

    public interface IEvaluationEngine
    {
        ImmEvaluation Evaluate(IpMaturityModel imm, IEvaluationContext? context = null);
    }
}