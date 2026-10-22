namespace Endjin.Imm.Processing
{
    using Endjin.Imm.Contracts;
    using Endjin.Imm.Domain;
    using NodaTime;
    using NodaTime.Text;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// Scores a project's IP Maturity Model against the rule definitions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When something listens to <see cref="ImmTelemetry.ActivitySourceName"/>, each evaluation
    /// is traced as an <c>imm.evaluate</c> span recording the number of rules evaluated
    /// (<c>imm.rule_count</c>: those the IMM asserts, has not opted out of, and the rule set
    /// defines), the score (<c>imm.score</c>) and the maximum possible score
    /// (<c>imm.max_score</c>). If the evaluation fails, the span's status is
    /// <see cref="ActivityStatusCode.Error"/> and its <c>error.type</c> tag names the exception's
    /// type. With no listener, no span is created.
    /// </para>
    /// </remarks>
    public class EvaluationEngine : IEvaluationEngine
    {
        private readonly RuleCalculatorFactory ruleCalculatorFactory;
        private readonly IRuleDefinitionRepository ruleDefinitionRepository;

        public EvaluationEngine(IRuleDefinitionRepository ruleDefinitionRepository)
        {
            this.ruleDefinitionRepository = ruleDefinitionRepository;
            this.ruleCalculatorFactory = new RuleCalculatorFactory(this.ruleDefinitionRepository);
        }

        public ImmEvaluation Evaluate(IpMaturityModel imm, IEvaluationContext? context = null)
        {
            using Activity? activity = ImmTelemetry.Source.StartActivity("imm.evaluate", ActivityKind.Internal);
            try
            {
                ImmEvaluation evaluation = this.EvaluateRules(imm, context ?? new Context());
                if (activity is not null)
                {
                    activity.SetTag("imm.rule_count", evaluation.RuleEvaluations.Count);
                    activity.SetTag("imm.score", evaluation.TotalScore);
                    activity.SetTag("imm.max_score", evaluation.MaximumPossibleTotalScore);
                }

                return evaluation;
            }
            catch (Exception ex)
            {
                activity.RecordFailure(ex);
                throw;
            }
        }

        private ImmEvaluation EvaluateRules(IpMaturityModel imm, IEvaluationContext context)
        {
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
