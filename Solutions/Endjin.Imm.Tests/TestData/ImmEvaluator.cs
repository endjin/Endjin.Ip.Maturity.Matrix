using Endjin.Imm.Contracts;
using Endjin.Imm.Domain;
using Endjin.Imm.Processing;
using Endjin.Imm.Repository;
using NodaTime;

namespace Endjin.Imm.Tests.TestData;

/// <summary>
/// Evaluates an IMM document against a rule set, both supplied as YAML, through the same public
/// API the host uses.
/// </summary>
internal static class ImmEvaluator
{
    /// <summary>
    /// The reference date used when a test does not supply one.
    /// </summary>
    public static readonly LocalDate DefaultReferenceDate = new(2019, 12, 3);

    public static ImmEvaluation Evaluate(string ruleSetYaml, string immYaml, LocalDate? referenceDate = null)
    {
        IpMaturityModelRuleset ruleSet = IpMaturityModelRuleset.FromYaml(ruleSetYaml);
        IpMaturityModel imm = IpMaturityModel.FromYaml(immYaml);
        EvaluationEngine engine = new(new RuleDefinitionRepository(ruleSet));

        return engine.Evaluate(imm, new FixedDateEvaluationContext(referenceDate ?? DefaultReferenceDate));
    }

    private sealed class FixedDateEvaluationContext(LocalDate evaluationReferenceDate) : IEvaluationContext
    {
        public LocalDate EvaluationReferenceDate { get; } = evaluationReferenceDate;
    }
}
