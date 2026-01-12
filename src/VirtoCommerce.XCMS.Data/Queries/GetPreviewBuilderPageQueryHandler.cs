using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;
using VirtoCommerce.ContentModule.Core.Search;
using VirtoCommerce.ContentModule.Core.Services;
using VirtoCommerce.PageBuilderModule.Core.Models;
using VirtoCommerce.PageBuilderModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCMS.Core.Models;
using VirtoCommerce.XCMS.Core.Queries;

using static VirtoCommerce.PageBuilderModule.Core.ModuleConstants.PageStatuses;

namespace VirtoCommerce.XCMS.Data.Queries;

/// <summary>
/// Handles queries for retrieving a preview builder page item based on store and page identifiers.
/// </summary>
/// <remarks>This handler supports scenarios where either a page or a group may be identified by the provided
/// identifiers. If the page builder page search service is not available, the handler returns null. The handler is
/// used in the context of previewing builder pages within a store.</remarks>
/// <param name="pageBuilderPageSearchService">An optional dependency that provides access to the page builder page search service used to retrieve page
/// information.</param>
/// <param name="groupedPageSearchService">An optional dependency that provides access to the grouped page search service used to retrieve group information
/// associated with pages.</param>
public class GetPreviewBuilderPageQueryHandler(
    IOptionalDependency<IPageBuilderPageSearchService> pageBuilderPageSearchService,
    IOptionalDependency<IGroupedPageSearchService> groupedPageSearchService,
    IOptionalDependency<IContentService> contentService,
    IStoreService storeService
) : IQueryHandler<GetBuilderPageQuery, BuilderPageItem>
{
    public async Task<BuilderPageItem> Handle(GetBuilderPageQuery request, CancellationToken cancellationToken)
    {
        var store = await storeService.GetByIdAsync(request.StoreId);

        var pagesEnabled = store.Settings.GetValue<bool>(Pages.Core.ModuleConstants.Settings.General.Enable);

        if (!pagesEnabled)
        {
            return await GetStaticContentPage(request, cancellationToken);
        }

        if (!pageBuilderPageSearchService.HasValue)
        {
            return null;
        }

        var (page, group) =  await GetPage(request);

        if (page != null && group == null)
        {
            group = await GetGroupById(request.StoreId, page.GroupId);
        }

        var pageItem = AbstractTypeFactory<BuilderPageItem>.TryCreateInstance();

        pageItem.PageId = page?.Id;
        pageItem.Permalink = group?.Permalink;
        return pageItem;
    }

    private async Task<BuilderPageItem> GetStaticContentPage(GetBuilderPageQuery request)
    {
        if (!contentService.HasValue)
        {
            return null;
        }

        var file = await contentService.Value.GetFileContentAsync(request.PageId);

        var result = AbstractTypeFactory<BuilderPageItem>.TryCreateInstance();
        result.PageId = request.PageId;
        result.Permalink = file?.Permalink;
        result.Content = file?.Content;

        return result;
    }

    private async Task<(PageBuilderPage, GroupedPageBuilderPage)> GetPage(GetBuilderPageQuery request)
    {
        var page = await GetPageById(request.StoreId, request.PageId);
        GroupedPageBuilderPage group = null;
        if (page == null)
        {
            group = await GetGroupById(request.StoreId, request.PageId);
            if (group != null)
            {
                var pageModel = group.Pages.FirstOrDefault(x => x.Status == Draft)
                    ?? group.Pages.FirstOrDefault(x => x.Status == Published)
                    ?? group.Pages.OrderByDescending(x => x.ModifiedDate).FirstOrDefault(x => x.Status == Archived);
                if (pageModel != null)
                {
                    request.PageId = pageModel.Id;
                    page = await GetPageById(request.StoreId, pageModel.Id);
                }
            }
        }

        return (page, group);
    }

    private async Task<PageBuilderPage> GetPageById(string storeId, string pageId)
    {
        var pageSearchCriteria = AbstractTypeFactory<PageBuilderPageSearchCriteria>.TryCreateInstance();
        pageSearchCriteria.StoreId = storeId;
        pageSearchCriteria.ObjectIds = [pageId];

        var pageSearchResult = await pageBuilderPageSearchService.Value.SearchAsync(pageSearchCriteria);
        var page = pageSearchResult.Results.FirstOrDefault();

        return page;
    }

    private async Task<GroupedPageBuilderPage> GetGroupById(string storeId, string groupId)
    {
        var groupSearchCriteria = AbstractTypeFactory<PageBuilderPageSearchCriteria>.TryCreateInstance();
        groupSearchCriteria.StoreId = storeId;
        groupSearchCriteria.ObjectIds = [groupId];

        var groupSearchResult = await groupedPageSearchService.Value.SearchAsync(groupSearchCriteria);
        var group = groupSearchResult.Results.FirstOrDefault();

        return group;
    }
}
