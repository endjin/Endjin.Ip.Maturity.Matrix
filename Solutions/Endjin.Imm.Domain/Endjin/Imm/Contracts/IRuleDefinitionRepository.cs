namespace Endjin.Imm.Contracts
{
    using Endjin.Imm.Domain;
    using System.Collections.Generic;

    public interface IRuleDefinitionRepository
    {
        RuleDefinition GetDefinitionFor(RuleAssertion ruleAssertion);

        IList<RuleDefinition> GetAll();
    }
}