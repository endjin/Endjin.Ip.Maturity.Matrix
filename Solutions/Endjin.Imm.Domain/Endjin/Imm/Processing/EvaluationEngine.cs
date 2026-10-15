namespace Endjin.Imm.Processing
{
    using Endjin.Imm.Contracts;
    using Endjin.Imm.Domain;
    using NodaTime;
    using NodaTime.Text;
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

                    var ruleEvaluation = Evaluate(calculator, rule, context);
                    ruleEvaluations.Add(ruleEvaluation);

                    totalScore += ruleEvaluation.Score;

                    // The calculator's maximum honours MeasureDefinition.CanOptOut, so opting out of a
                    // measure that cannot be opted out of lowers neither the rule's maximum nor the total.
                    maximumPossibleScore += ruleEvaluation.MaximumScore;
                }
            }

            return new ImmEvaluation(ruleEvaluations, totalScore, maximumPossibleScore);
        }

        /// <summary>
        /// Evaluates one rule, reporting the failures that mean the IMM's assertion for the rule does
        /// not fit the rule's definition as an <see cref="ImmEvaluationException"/> naming the rule.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Only the exceptions the calculators throw for such data are wrapped: more or fewer
        /// measures asserted than the rule allows (<see cref="InvalidOperationException"/> from
        /// <c>Single</c>), a date or period that cannot be parsed (<see cref="UnparsableValueException"/>),
        /// and an Age or Framework measure where the calculator needs the other kind
        /// (<see cref="ArgumentException"/>). An <see cref="UnknownRuleException"/> is not wrapped,
        /// and anything else is a bug, so it propagates as it is.
        /// </para>
        /// </remarks>
        private static RuleEvaluation Evaluate(IRuleCalculator calculator, RuleAssertion rule, IEvaluationContext context)
        {
            try
            {
                return calculator.Evaluate(rule, context);
            }
            catch (Exception ex) when ((ex is InvalidOperationException or UnparsableValueException or ArgumentException) && ex is not UnknownRuleException)
            {
                throw new ImmEvaluationException(
                    rule.Id,
                    $"Rule {rule.Id}{(rule.Name is null ? string.Empty : $" ('{rule.Name}')")} could not be evaluated: {ex.Message}",
                    ex);
            }
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
