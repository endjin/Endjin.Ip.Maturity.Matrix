namespace Endjin.Imm.Serialisation
{
    using Endjin.Imm.Caching;
    using Endjin.Imm.Domain;
    using System.Net.Http;
    using System.Threading.Tasks;

    /// <summary>
    /// Used by <see cref="HttpDeserializingCache{T}"/> to build <see cref="IpMaturityMatrix"/>
    /// instances from HTTP responses.
    /// </summary>
    internal class IpMaturityMatrixHttpDeserializer : IHttpDeserializer<IpMaturityMatrix>
    {
        public async Task<IpMaturityMatrix> DeserializeAsync(HttpResponseMessage response)
        {
            string immYaml = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return IpMaturityMatrix.FromYaml(immYaml);
        }
    }
}
