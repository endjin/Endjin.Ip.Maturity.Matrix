namespace Endjin.Imm.Processing
{
    using Endjin.Imm.Contracts;
    using Endjin.Imm.Domain;
    using Endjin.Imm.Repository;
    using NodaTime;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class EvaluationEngine : IEvaluationEngine
    {
        private readonly IpMaturityMatrixRuleset ruleSet;
        private readonly RuleCalculatorFactory ruleCalculatorFactory;
        private readonly IRuleDefinitionRepository ruleDefinitionRepository;

        public EvaluationEngine(IpMaturityMatrixRuleset ruleSet)
        {
            this.ruleSet = ruleSet;
            this.ruleDefinitionRepository = new RuleDefinitionRepository(this.ruleSet);
            this.ruleCalculatorFactory = new RuleCalculatorFactory(this.ruleDefinitionRepository);
        }

        public IEnumerable<RuleEvaluation> Evaluate(IpMaturityMatrix imm, IEvaluationContext context = null)
        {
            context ??= new Context();
            foreach (var rule in imm.Rules)
            {
                var ruleDefinition = this.ruleDefinitionRepository.Get(rule);
                var calculator = this.ruleCalculatorFactory.Create(ruleDefinition.DataType);

                yield return new RuleEvaluation { Rule = rule, Percentage = calculator.Percentage(rule, context), Score = calculator.Score(rule, context) };
            }
        }

        public long MaximumScore()
        {
            var definitions = this.ruleDefinitionRepository.GetAll();
            long runningTotal = 0;

            foreach (var definition in definitions)
            {
                switch (definition.DataType)
                {
                    case DataType.Continuous:
                        runningTotal += definition.Measures.Sum(x => x.Score);
                        break;
                    case DataType.Discrete:
                        runningTotal += definition.Measures.Last().Score;
                        break;
                    default:
                        break;
                }
            }

            return runningTotal;
        }

        private class Context : IEvaluationContext
        {
            public Context()
            {
                LocalDate.FromDateTime(DateTime.Now);
            }

            public LocalDate EvaluationReferenceDate { get; }
        }
    }
}