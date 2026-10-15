namespace Endjin.Imm.Processing
{
    using Endjin.Imm.Contracts;
    using Endjin.Imm.Domain;

    /// <summary>
    /// Provides the <see cref="IRuleCalculator"/> for each <see cref="DataType"/>.
    /// </summary>
    /// <remarks>
    /// The calculators hold no state other than the rule definition repository, so each factory
    /// creates them once and returns the same instances every time.
    /// </remarks>
    public class RuleCalculatorFactory
    {
        private readonly IRuleCalculator continuous;
        private readonly IRuleCalculator discrete;
        private readonly IRuleCalculator age;
        private readonly IRuleCalculator framework;
        private readonly IRuleCalculator unknown;

        public RuleCalculatorFactory(IRuleDefinitionRepository rdr)
        {
            this.continuous = new ContinuousRuleCalculator(rdr);
            this.discrete = new DiscreteRuleCalculator(rdr);
            this.age = new AgeRuleCalculator(rdr);
            this.framework = new FrameworkRuleCalculator(rdr);
            this.unknown = new NullRuleCalculator();
        }

        public IRuleCalculator Create(DataType dataType)
        {
            return dataType switch
            {
                DataType.Continuous => this.continuous,
                DataType.Discrete   => this.discrete,
                DataType.Age        => this.age,
                DataType.Framework  => this.framework,
                _ => this.unknown,
            };
        }
    }
}
