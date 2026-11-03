using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.ContentModule.Core.Model;
using VirtoCommerce.ContentModule.Core.Services;
using VirtoCommerce.PageBuilderModule.Core.Theme;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Models;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XCMS.Core.Models;
using VirtoCommerce.XCMS.Core.Queries;

namespace VirtoCommerce.XCMS.Data.Queries;

/// <summary>
/// The store found as pageContext finds it, its published Page Builder theme, and the menus the theme names, read on
/// every request so that a menu edited in Content shows without a new publication.
/// </summary>
public class GetThemeSnapshotQueryHandler(
    IStoreDomainResolverService storeDomainResolverService,
    IMenuLinkListSearchService menuLinkListSearchService,
    IOptionalDependency<IThemeSnapshotService> themeSnapshotService)
    : IQueryHandler<GetThemeSnapshotQuery, ThemeSnapshotResult>
{
    public async Task<ThemeSnapshotResult> Handle(GetThemeSnapshotQuery request, CancellationToken cancellationToken)
    {
        if (!themeSnapshotService.HasValue)
        {
            return null;
        }

        var storeRequest = AbstractTypeFactory<StoreDomainRequest>.TryCreateInstance();
        storeRequest.StoreId = request.StoreId;
        storeRequest.Domain = request.Domain;
        var store = await storeDomainResolverService.GetStoreAsync(storeRequest);
        if (store == null)
        {
            return null;
        }

        var snapshot = await themeSnapshotService.Value.GetPublishedAsync(store.Id, request.CultureName, cancellationToken);
        if (snapshot == null)
        {
            return null;
        }

        return new ThemeSnapshotResult
        {
            StoreId = snapshot.StoreId,
            CultureName = snapshot.CultureName,
            Publication = snapshot.Publication,
            Content = snapshot.Content,
            Menus = await GetMenusAsync(snapshot),
        };
    }

    private async Task<IList<Menu>> GetMenusAsync(ThemeSnapshot snapshot)
    {
        if (snapshot.Menus.Count == 0)
        {
            return [];
        }

        var criteria = AbstractTypeFactory<MenuLinkListSearchCriteria>.TryCreateInstance();
        criteria.StoreId = snapshot.StoreId;
        var menuLinkLists = await menuLinkListSearchService.SearchAllAsync(criteria);

        return snapshot.Menus
            .Select(name => (Name: name, Menu: GetMenuQueryHandler.ToMenu(menuLinkLists, snapshot.CultureName, name)))
            .Where(x => x.Menu != null)
            .Select(x =>
            {
                // under the name the theme holds, which the storefront looks it up by
                x.Menu.Name = x.Name;
                return x.Menu;
            })
            .ToList();
    }
}
