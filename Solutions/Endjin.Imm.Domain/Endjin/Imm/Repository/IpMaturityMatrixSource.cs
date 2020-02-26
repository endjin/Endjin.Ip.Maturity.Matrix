namespace Endjin.Imm.Repository
{
    using Endjin.Imm.Caching;
    using Endjin.Imm.Contracts;
    using Endjin.Imm.Domain;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides cached access to <see cref="IpMaturityMatrix"/> objects.
    /// </summary>
    internal class IpMaturityMatrixSource : IIpMaturityMatrixSource
    {
        private readonly HttpDeserializingCache<IpMaturityMatrix> cache;

        public IpMaturityMatrixSource(HttpDeserializingCache<IpMaturityMatrix> cache)
        {
            this.cache = cache;
        }

        /// <inheritdoc />
        public async Task<IpMaturityMatrix> GetIpMaturityMatrixAsync(
            string gitHubOwner,
            string gitHubProject,
            string objectName)
        {
            string url = $"https://raw.githubusercontent.com/{gitHubOwner}/{gitHubProject}/{objectName}/imm.yaml";
            IpMaturityMatrix imm = await cache.GetAsync(url).ConfigureAwait(false);
            return imm;
        }
    }
}
