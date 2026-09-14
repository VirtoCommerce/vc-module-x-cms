using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.PageBuilderModule.Core.Services;
using VirtoCommerce.Pages.Core.ContentProviders;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.XCMS.Core.Models;

namespace VirtoCommerce.XCMS.Core.Schemas;

public class BuilderPageItemType : ObjectGraphType<BuilderPageItem>
{
    private readonly IOptionalDependency<IGroupedPageService> _groupedPageService;
    private readonly IPageContentProvider _pageBuilderContentProvider;

    public BuilderPageItemType(IOptionalDependency<IGroupedPageService> groupedPageService)
        : this(groupedPageService, [])
    {
    }

    [ActivatorUtilitiesConstructor]
    public BuilderPageItemType(
        IOptionalDependency<IGroupedPageService> groupedPageService,
        IEnumerable<IPageContentProvider> pageContentProviders)
    {
        _groupedPageService = groupedPageService;
        _pageBuilderContentProvider = pageContentProviders?.FirstOrDefault(x => x.ProviderName == "PageBuilder");

        Field(x => x.Permalink, nullable: true).Description("Page permalink");
        Field(x => x.Content, nullable: true).ResolveAsync(LoadContent);
    }

    protected virtual async Task<string> LoadContent(IResolveFieldContext<BuilderPageItem> context)
    {
        if (_groupedPageService.HasValue && context.Source.Content.IsNullOrEmpty())
        {
            var pageId = context.Source.PageId;

            if (pageId != null)
            {
                if (_pageBuilderContentProvider != null)
                {
                    // Use the same resolved document as publishing. Authoring content intentionally keeps
                    // componentRef markers, which the standalone storefront preview cannot render.
                    var pages = await _pageBuilderContentProvider.GetByIdsAsync([pageId]);
                    return pages.FirstOrDefault(x => x.Id == pageId)?.Content;
                }

                return await _groupedPageService.Value.LoadContent(pageId);
            }
        }

        return context.Source.Content;
    }
}

