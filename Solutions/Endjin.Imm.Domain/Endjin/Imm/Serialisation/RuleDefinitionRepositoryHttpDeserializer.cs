namespace Endjin.Imm.Serialisation
{
    using Endjin.Imm.Caching;
    using Endjin.Imm.Contracts;
    using Endjin.Imm.Domain;
    using Endjin.Imm.Repository;
    using System.Net.Http;
    using System.Threading.Tasks;

    /// <summary>
    /// Used by <see cref="HttpDeserializingCache{T}"/> to build
    /// <see cref="IRuleDefinitionRepository"/> instances from HTTP responses.
    /// </summary>
    internal class RuleDefinitionRepositoryHttpDeserializer : IHttpDeserializer<IRuleDefinitionRepository>
    {
        public async Task<IRuleDefinitionRepository> DeserializeAsync(HttpResponseMessage response)
        {
            string yaml = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var ruleset = IpMaturityMatrixRuleset.FromYaml(yaml);
            return new RuleDefinitionRepository(ruleset);
        }
    }
}
