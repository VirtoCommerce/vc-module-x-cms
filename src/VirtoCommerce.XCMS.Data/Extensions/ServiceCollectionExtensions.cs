using GraphQL.Server;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.XCMS.Core;

namespace VirtoCommerce.XCMS.Data.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddXcms(this IServiceCollection services, IGraphQLBuilder graphQlbuilder)
        {
            graphQlbuilder.AddSchema(typeof(CoreAssemblyMarker), typeof(DataAssemblyMarker));

            return services;
        }
    }
}
