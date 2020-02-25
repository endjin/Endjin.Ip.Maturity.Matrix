namespace Endjin.Imm.Contracts
{
    using Endjin.Imm.Domain;

    public interface IEvaluationEngine
    {
        ImmEvaluation Evaluate(IpMaturityMatrix imm, IEvaluationContext? context = null);
    }
}