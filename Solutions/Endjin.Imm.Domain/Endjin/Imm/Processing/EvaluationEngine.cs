namespace Endjin.Imm.Processing
{
    using Endjin.Imm.Contracts;
    using Endjin.Imm.Domain;
    using NodaTime;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class EvaluationEngine : IEvaluationEngine
    {
        private readonly RuleCalculatorFactory ruleCalculatorFactory;
        private readonly IRuleDefinitionRepository ruleDefinitionRepository;

        public EvaluationEngine(IRuleDefinitionRepository ruleDefinitionRepository)
        {
            this.ruleDefinitionRepository = ruleDefinitionRepository;
            this.ruleCalculatorFactory = new RuleCalculatorFactory(this.ruleDefinitionRepository);
        }

        public ImmEvaluation Evaluate(IpMaturityMatrix imm, IEvaluationContext? context = null)
        {
            context ??= new Context();

            IList<RuleAssertion> rules = imm.Rules;
            var ruleEvaluations = new List<RuleEvaluation>(rules.Count);
            long totalScore = 0;
            long maximumPossibleScore = 0;
            for (int i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];
                if (rule.OptOut)
                {
                    continue;
                }

                var ruleDefinition = this.ruleDefinitionRepository.GetDefinitionFor(rule);
                if (ruleDefinition != null)
                {
                    var calculator = this.ruleCalculatorFactory.Create(ruleDefinition.DataType);

                    var ruleEvaluation = calculator.Evaluate(rule, context);
                    ruleEvaluations.Add(ruleEvaluation);

                    totalScore += ruleEvaluation.Score;
                    maximumPossibleScore += GetMaximumScoreForRule(ruleDefinition, rule);
                }
            }

            return new ImmEvaluation(ruleEvaluations, totalScore, maximumPossibleScore);
        }

        private static long GetMaximumScoreForRule(RuleDefinition definition, RuleAssertion assertion)
        {
            // Fast path: when the assertion opts out of nothing, every measure is applicable, so
            // there is no need to build the filtered copies below.
            // Note: unlike RuleCalculatorBase, this does not consider MeasureDefinition.CanOptOut.
            IList<MeasureAssertion> measureAssertions = assertion.Measures;
            IList<MeasureDefinition> measureDefinitions = definition.Measures;
            if (measureAssertions is not null &&
                measureDefinitions is not null &&
                !RuleCalculatorBase.AnyOptedOut(measureAssertions))
            {
                if (definition.DataType == DataType.Continuous)
                {
                    long sum = 0;
                    for (int i = 0; i < measureDefinitions.Count; i++)
                    {
                        // Checked, like Enumerable.Sum.
                        sum = checked(sum + measureDefinitions[i].Score);
                    }

                    return sum;
                }

                // Enumerable.Max throws InvalidOperationException if there are no measures.
                return measureDefinitions.Max(m => m.Score);
            }

            return GetMaximumScoreForRuleWithOptOuts(definition, assertion);
        }

        // Separate from GetMaximumScoreForRule because the lambdas here capture a local, and the
        // compiler allocates the closure on entry to the scope declaring it, even on the fast path.
        private static long GetMaximumScoreForRuleWithOptOuts(RuleDefinition definition, RuleAssertion assertion)
        {
            var optedOut = assertion.Measures.Where(m => m.OptOut).Select(m => m.Description).ToHashSet();

            var applicableMeasures = definition.Measures.Where(m => !optedOut.Contains(m.Description)).ToList();
            return definition.DataType switch
            {
                DataType.Continuous => applicableMeasures.Sum(x => x.Score),
                _ => applicableMeasures.Max(m => m.Score),
            };
        }

        private class Context : IEvaluationContext
        {
            public Context()
            {
                this.EvaluationReferenceDate = LocalDate.FromDateTime(DateTime.Now);
            }

            public LocalDate EvaluationReferenceDate { get; }
        }
    }
}
