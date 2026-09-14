using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VirtoCommerce.PageBuilderModule.Core.Services;
using VirtoCommerce.Pages.Core.ContentProviders;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.XCMS.Core.Models;

namespace VirtoCommerce.XCMS.Core.Schemas;

public class BuilderPageItemType : ObjectGraphType<BuilderPageItem>
{
    /// <summary>
    /// <see cref="IPageContentProvider.ProviderName"/> of the Page Builder content provider.
    /// Declared locally until a PageBuilderModule.Core package that publishes
    /// ModuleConstants.ContentProviders.PageBuilder is released; switch to that constant afterwards.
    /// </summary>
    protected const string PageBuilderProviderName = "PageBuilder";

    private readonly IOptionalDependency<IGroupedPageService> _groupedPageService;

    public BuilderPageItemType(IOptionalDependency<IGroupedPageService> groupedPageService)
    {
        _groupedPageService = groupedPageService;

        Field(x => x.Permalink, nullable: true).Description("Page permalink");
        Field(x => x.Content, nullable: true).ResolveAsync(LoadContent);
    }

    protected virtual async Task<string> LoadContent(IResolveFieldContext<BuilderPageItem> context)
    {
        var pageId = context.Source.PageId;

        if (!_groupedPageService.HasValue || !context.Source.Content.IsNullOrEmpty() || pageId == null)
        {
            return context.Source.Content;
        }

        var contentProvider = GetPageBuilderContentProvider(context);

        if (contentProvider == null)
        {
            return await _groupedPageService.Value.LoadContent(pageId, context.CancellationToken);
        }

        // Use the same resolved document as publishing and indexing. Authoring content intentionally keeps
        // componentRef markers, which the standalone storefront preview cannot render.
        var pages = await contentProvider.GetByIdsAsync([pageId]);
        var content = pages?.FirstOrDefault(x => x.Id.EqualsIgnoreCase(pageId))?.Content;

        if (content == null)
        {
            // The provider skips a page it cannot resolve (unknown page, missing group, malformed
            // componentRef marker). Preview stays empty by design, so leave a trace for diagnostics.
            GetLogger(context)?.LogWarning(
                "Page Builder content provider returned no resolved content for page '{PageId}'. The preview will be empty.",
                pageId);
        }

        return content;
    }

    /// <summary>
    /// Resolves the Page Builder content provider from the per-request DI scope.
    /// Graph types are built once and act as singletons, so request-scoped services must never be
    /// constructor-injected here. Returns null when Page Builder (or the request scope) is not available,
    /// in which case the raw authoring content is served as before.
    /// </summary>
    protected virtual IPageContentProvider GetPageBuilderContentProvider(IResolveFieldContext context)
    {
        return context.RequestServices
            ?.GetServices<IPageContentProvider>()
            .FirstOrDefault(x => x.ProviderName.EqualsIgnoreCase(PageBuilderProviderName));
    }

    protected virtual ILogger GetLogger(IResolveFieldContext context)
    {
        return context.RequestServices?.GetService<ILogger<BuilderPageItemType>>();
    }
}
