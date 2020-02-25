namespace Endjin.Imm.Repository
{
    using Endjin.Imm.Caching;
    using Endjin.Imm.Contracts;
    using Endjin.Imm.Domain;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides cached access to IMM rule definitions.
    /// </summary>
    internal class RuleDefinitionRepositorySource : IRuleDefinitionRepositorySource
    {
        private readonly HttpDeserializingCache<IRuleDefinitionRepository> cache;

        public RuleDefinitionRepositorySource(HttpDeserializingCache<IRuleDefinitionRepository> cache)
        {
            this.cache = cache;
        }

        /// <inheritdoc />
        public async Task<IRuleDefinitionRepository> GetRuleDefinitionRepositoryAsync(string objectName)
        {
            string ruleUrl = IpMaturityMatrixRuleset.DefinitionsUrlForName(objectName);
            IRuleDefinitionRepository repository = await cache.GetAsync(ruleUrl).ConfigureAwait(false);
            return repository;
        }
    }
}