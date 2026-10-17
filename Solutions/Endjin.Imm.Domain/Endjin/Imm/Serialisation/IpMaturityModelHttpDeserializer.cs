namespace Endjin.Imm.Serialisation
{
    using Endjin.Imm.Caching;
    using Endjin.Imm.Domain;
    using System.Net.Http;
    using System.Threading.Tasks;

    /// <summary>
    /// Used by <see cref="HttpDeserializingCache{T}"/> to build <see cref="IpMaturityModel"/>
    /// instances from HTTP responses.
    /// </summary>
    internal class IpMaturityModelHttpDeserializer : IHttpDeserializer<IpMaturityModel>
    {
        public async Task<IpMaturityModel> DeserializeAsync(HttpResponseMessage response)
        {
            string immYaml = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return IpMaturityModel.FromYaml(immYaml);
        }
    }
}
