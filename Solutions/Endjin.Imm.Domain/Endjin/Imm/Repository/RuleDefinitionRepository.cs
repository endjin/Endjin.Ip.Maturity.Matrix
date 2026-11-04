namespace Endjin.Imm.Repository
{
    using System;
    using System.Collections.Frozen;
    using System.Collections.Generic;

    using Endjin.Imm.Contracts;
    using Endjin.Imm.Domain;

    /// <summary>
    /// Looks up rule definitions by id.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The rule set is indexed once, on construction, so each lookup is a hash lookup rather than
    /// a linear scan. (The evaluation engine looks up every rule in an IMM, so the scan made
    /// evaluation quadratic in the number of rules.) The index takes a snapshot of the rule set's
    /// <see cref="IpMaturityModelRuleset.Rules"/>; changes made to that list afterwards are not
    /// seen by lookups.
    /// </para>
    /// </remarks>
    public class RuleDefinitionRepository : IRuleDefinitionRepository
    {
        private readonly IpMaturityModelRuleset ruleSet;
        private readonly FrozenDictionary<Guid, RuleDefinition> definitionsById;

        public RuleDefinitionRepository(IpMaturityModelRuleset ruleSet)
        {
            this.ruleSet = ruleSet;

            // TryAdd keeps the first definition for any id, which is what the linear
            // FirstOrDefault search this replaces returned when a rule set repeats an id.
            // A null entry (such as '- ~' in the YAML) defines no rule, so it is skipped.
            var definitionsById = new Dictionary<Guid, RuleDefinition>();
            foreach (RuleDefinition? definition in ruleSet.Rules)
            {
                if (definition is not null)
                {
                    definitionsById.TryAdd(definition.Id, definition);
                }
            }

            this.definitionsById = definitionsById.ToFrozenDictionary();
        }

        public RuleDefinition GetDefinitionFor(RuleAssertion ruleAssertion)
        {
            return this.definitionsById.TryGetValue(ruleAssertion.Id, out RuleDefinition? result)
                ? result
                : throw new UnknownRuleException(ruleAssertion.Id);
        }

        public IList<RuleDefinition> GetAll()
        {
            return this.ruleSet.Rules;
        }
    }
}
