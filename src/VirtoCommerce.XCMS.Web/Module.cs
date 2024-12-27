using GraphQL.MicrosoftDI;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.XCMS.Core;
using VirtoCommerce.XCMS.Data;

namespace VirtoCommerce.XCMS.Web;

public class Module : IModule, IHasConfiguration
{
    public ManifestModuleInfo ModuleInfo { get; set; }
    public IConfiguration Configuration { get; set; }

    public void Initialize(IServiceCollection serviceCollection)
    {
        _ = new GraphQLBuilder(serviceCollection, builder =>
        {
            builder.AddSchema(serviceCollection, typeof(CoreAssemblyMarker), typeof(DataAssemblyMarker));
        });
    }

    public void PostInitialize(IApplicationBuilder appBuilder)
    {
        // Nothing to do here
    }

    public void Uninstall()
    {
        // Nothing to do here
    }
}
