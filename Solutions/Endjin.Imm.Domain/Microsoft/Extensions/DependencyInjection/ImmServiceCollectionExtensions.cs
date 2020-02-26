namespace Microsoft.Extensions.DependencyInjection
{
    using System.Linq;
    using Endjin.Imm.Caching;
    using Endjin.Imm.Repository;
    using Endjin.Imm.Contracts;
    using Endjin.Imm.Serialisation;
    using Endjin.Imm.Domain;

    public static class ImmServiceCollectionExtensions
    {
        public static IServiceCollection AddImmSources(
            this IServiceCollection services)
        {
            if (services.Any(s => s.ServiceType == typeof(HttpDeserializingCache<>)))
            {
                return services;
            }

            services.AddSingleton(typeof(HttpDeserializingCache<>));
            services.AddSingleton<IHttpDeserializer<IRuleDefinitionRepository>, RuleDefinitionRepositoryHttpDeserializer>();
            services.AddSingleton<IHttpDeserializer<IpMaturityMatrix>, IpMaturityMatrixHttpDeserializer>();

            services.AddSingleton<IRuleDefinitionRepositorySource, RuleDefinitionRepositorySource>();
            services.AddSingleton<IIpMaturityMatrixSource, IpMaturityMatrixSource>();

            return services;
        }
    }
}
