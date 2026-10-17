using Endjin.Imm.Domain;

namespace Endjin.Imm.Tests.TestData;

internal static class ImmEvaluationExtensions
{
    /// <summary>
    /// Gets the evaluation of the rule with the specified id.
    /// </summary>
    /// <param name="evaluation">The IMM evaluation.</param>
    /// <param name="ruleId">The rule id.</param>
    /// <returns>The single <see cref="RuleEvaluation"/> for the rule.</returns>
    public static RuleEvaluation ForRule(this ImmEvaluation evaluation, string ruleId)
    {
        Guid id = Guid.Parse(ruleId);
        return evaluation.RuleEvaluations.Single(r => r.RuleAssertion.Id == id);
    }
}
