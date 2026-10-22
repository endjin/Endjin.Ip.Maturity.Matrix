namespace Endjin.Imm.Serialisation
{
    using Endjin.Imm.Caching;
    using Endjin.Imm.Domain;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Threading.Tasks;

    /// <summary>
    /// Used by <see cref="HttpDeserializingCache{T}"/> to build <see cref="IpMaturityModel"/>
    /// instances from HTTP responses.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The cache calls this inside its <c>imm.content.deserialize</c> span. Only this class sees
    /// the YAML text, so it adds the text's length (in characters) to that span as
    /// <c>imm.content.length</c>.
    /// </para>
    /// </remarks>
    internal class IpMaturityModelHttpDeserializer : IHttpDeserializer<IpMaturityModel>
    {
        public async Task<IpMaturityModel> DeserializeAsync(HttpResponseMessage response)
        {
            string immYaml = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (Activity.Current is { } activity && activity.Source == ImmTelemetry.Source)
            {
                activity.SetTag("imm.content.length", immYaml.Length);
            }

            return IpMaturityModel.FromYaml(immYaml);
        }
    }
}
