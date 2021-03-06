﻿namespace Endjin.Imm.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Endjin.Imm.Contracts;
    using Endjin.Imm.Domain;

    public class RuleDefinitionRepository : IRuleDefinitionRepository
    {
        private readonly IpMaturityMatrixRuleset ruleSet;

        public RuleDefinitionRepository(IpMaturityMatrixRuleset ruleSet)
        {
            this.ruleSet = ruleSet;
        }

        public RuleDefinition GetDefinitionFor(RuleAssertion ruleAssertion)
        {
            RuleDefinition? result = this.ruleSet.Rules.FirstOrDefault(x => x.Id == ruleAssertion.Id);
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
    }
}