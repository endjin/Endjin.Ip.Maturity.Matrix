namespace Endjin.Imm.Processing
{
    using Endjin.Imm.Contracts;
    using Endjin.Imm.Domain;
    using NodaTime;
    using System;
    using System.Collections.Generic;

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

                    // The calculator's maximum honours MeasureDefinition.CanOptOut, so opting out of a
                    // measure that cannot be opted out of lowers neither the rule's maximum nor the total.
                    maximumPossibleScore += ruleEvaluation.MaximumScore;
                }
            }

            return new ImmEvaluation(ruleEvaluations, totalScore, maximumPossibleScore);
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
