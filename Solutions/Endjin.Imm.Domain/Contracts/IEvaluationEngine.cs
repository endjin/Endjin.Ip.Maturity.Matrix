namespace Endjin.Imm.Contracts
{
    using Endjin.Imm.Domain;
    using System.Collections.Generic;

    public interface IEvaluationEngine
    {
        IEnumerable<RuleEvaluation> Evaluate(IpMaturityMatrix imm, IEvaluationContext? context = null);
    }
}