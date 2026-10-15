namespace Endjin.Imm.Repository
{
    using System;
    using System.Collections.Frozen;
    using System.Collections.Generic;
    using System.Linq;

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
    /// <see cref="IpMaturityMatrixRuleset.Rules"/>; changes made to that list afterwards are not
    /// seen by lookups.
    /// </para>
    /// </remarks>
    public class RuleDefinitionRepository : IRuleDefinitionRepository
    {
        private readonly IpMaturityMatrixRuleset ruleSet;
        private readonly FrozenDictionary<Guid, RuleDefinition> definitionsById;
        private readonly bool ruleSetContainsNullEntries;

        public RuleDefinitionRepository(IpMaturityMatrixRuleset ruleSet)
        {
            this.ruleSet = ruleSet;

            // TryAdd keeps the first definition for any id, which is what the linear
            // FirstOrDefault search this replaces returned when a rule set repeats an id.
            var definitionsById = new Dictionary<Guid, RuleDefinition>();
            foreach (RuleDefinition definition in ruleSet.Rules)
            {
                if (definition is null)
                {
                    // A linear search fails with a NullReferenceException on reaching a null
                    // entry. Only the entries before the first null can be found without reaching
                    // it, so we index just those and fall back to the linear search otherwise.
                    this.ruleSetContainsNullEntries = true;
                    break;
                }

                definitionsById.TryAdd(definition.Id, definition);
            }

            this.definitionsById = definitionsById.ToFrozenDictionary();
        }

        public RuleDefinition GetDefinitionFor(RuleAssertion ruleAssertion)
        {
            if (this.definitionsById.TryGetValue(ruleAssertion.Id, out RuleDefinition? result))
            {
                return result;
            }

            if (this.ruleSetContainsNullEntries)
            {
                result = this.FindByLinearSearch(ruleAssertion.Id);
            }

            if (result == null)
            {
                throw new ArgumentException("No definition found for rule id " + ruleAssertion.Id);
            }

            return result;
        }

        public IList<RuleDefinition> GetAll()
        {
            return this.ruleSet.Rules;
        }

        // In its own method because a lambda capturing a parameter allocates its closure on entry
        // to the method that declares it, which would make every lookup allocate.
        private RuleDefinition? FindByLinearSearch(Guid id)
        {
            return this.ruleSet.Rules.FirstOrDefault(x => x.Id == id);
        }
    }
}
