namespace Endjin.Imm.Serialisation
{
    using Endjin.Imm.Caching;
    using Endjin.Imm.Contracts;
    using Endjin.Imm.Domain;
    using Endjin.Imm.Repository;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Threading.Tasks;

    /// <summary>
    /// Used by <see cref="HttpDeserializingCache{T}"/> to build
    /// <see cref="IRuleDefinitionRepository"/> instances from HTTP responses.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The cache calls this inside its <c>imm.content.deserialize</c> span. Only this class sees
    /// the YAML text, so it adds the text's length (in characters) to that span as
    /// <c>imm.content.length</c>.
    /// </para>
    /// </remarks>
    internal class RuleDefinitionRepositoryHttpDeserializer : IHttpDeserializer<IRuleDefinitionRepository>
    {
        public async Task<IRuleDefinitionRepository> DeserializeAsync(HttpResponseMessage response)
        {
            string yaml = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (Activity.Current is { } activity && activity.Source == ImmTelemetry.Source)
            {
                activity.SetTag("imm.content.length", yaml.Length);
            }

            var ruleset = IpMaturityModelRuleset.FromYaml(yaml);
            return new RuleDefinitionRepository(ruleset);
        }
    }
}
