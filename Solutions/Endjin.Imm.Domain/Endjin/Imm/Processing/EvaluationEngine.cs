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

            var ruleEvaluations = new List<RuleEvaluation>();
            long totalScore = 0;
            long maximumPossibleScore = 0;
            foreach (var rule in imm.Rules.Where(r => !r.OptOut))
            {
                var ruleDefinition = this.ruleDefinitionRepository.GetDefinitionFor(rule);
                if (ruleDefinition != null)
                {
                    var calculator = this.ruleCalculatorFactory.Create(ruleDefinition.DataType);

                    var ruleEvaluation = new RuleEvaluation(rule, calculator.Percentage(rule, context), calculator.Score(rule, context));
                    ruleEvaluations.Add(ruleEvaluation);

                    totalScore += ruleEvaluation.Score;
                    maximumPossibleScore += GetMaximumScoreForRule(ruleDefinition, rule);
                }
            }

            return new ImmEvaluation(ruleEvaluations, totalScore, maximumPossibleScore);
        }

        private static long GetMaximumScoreForRule(RuleDefinition definition, RuleAssertion assertion)
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