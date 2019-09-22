namespace Endjin.Imm.Contracts
{
    using Endjin.Imm.Domain;

    public interface IRuleDefinitionRepository
    {
        RuleDefinition Get(Rule rule);

        RuleDefinition[] GetAll();
    }
}