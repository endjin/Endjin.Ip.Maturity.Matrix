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
            switch (dataType)
            {
                case DataType.Continuous:
                    return new ContinuousRuleCalculator(this.rdr);
                case DataType.Discrete:
                    return new DiscreteRuleCalculator(this.rdr);
                case DataType.Age:
                    return new AgeRuleCalculator(this.rdr);
                default:
                    return new NullRuleCalculator();
            }
        }
    }
}