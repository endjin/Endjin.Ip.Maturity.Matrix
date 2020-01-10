namespace Endjin.Imm.Processing
{
    using Endjin.Imm.Contracts;
    using Endjin.Imm.Domain;

    public class RuleCalculatorFactory
    {
        private readonly IRuleDefinitionRepository rdr;

        public RuleCalculatorFactory(IRuleDefinitionRepository rdr)
        {
            this.rdr = rdr;
        }

        public IRuleCalculator Create(DataType dataType)
        {
            return dataType switch
            {
                DataType.Continuous => new ContinuousRuleCalculator(this.rdr),
                DataType.Discrete   => new DiscreteRuleCalculator(this.rdr),
                DataType.Age        => new AgeRuleCalculator(this.rdr),
                DataType.Framework  => new FrameworkRuleCalculator(this.rdr),
                _ => new NullRuleCalculator(),
            };
        }
    }
}