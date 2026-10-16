namespace Endjin.Imm.Repository
{
    using Endjin.Imm.Caching;
    using Endjin.Imm.Contracts;
    using Endjin.Imm.Domain;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides cached access to <see cref="IpMaturityModel"/> objects.
    /// </summary>
    internal class IpMaturityModelSource : IIpMaturityModelSource
    {
        private readonly HttpDeserializingCache<IpMaturityModel> cache;

        public IpMaturityModelSource(HttpDeserializingCache<IpMaturityModel> cache)
        {
            this.cache = cache;
        }

        /// <inheritdoc />
        public async Task<IpMaturityModel> GetIpMaturityModelAsync(
            string gitHubOwner,
            string gitHubProject,
            string objectName)
        {
            string url = $"https://raw.githubusercontent.com/{gitHubOwner}/{gitHubProject}/{objectName}/imm.yaml";
            IpMaturityModel imm = await cache.GetAsync(url).ConfigureAwait(false);
            return imm;
        }
    }
}
