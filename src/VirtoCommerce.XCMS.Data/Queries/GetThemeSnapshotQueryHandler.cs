using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.ContentModule.Core.Model;
using VirtoCommerce.ContentModule.Core.Services;
using VirtoCommerce.PageBuilderModule.Core.Theme;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Models;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XCMS.Core.Models;
using VirtoCommerce.XCMS.Core.Queries;

namespace VirtoCommerce.XCMS.Data.Queries;

/// <summary>
/// A store's published Page Builder theme with the menus it names. The menus are read on every request so that a menu
/// edited in Content shows without a new publication.
/// </summary>
public class GetThemeSnapshotQueryHandler(
    IStoreDomainResolverService storeDomainResolverService,
    IMenuLinkListSearchService menuLinkListSearchService,
    IThemeSnapshotService themeSnapshotService)
    : IQueryHandler<GetThemeSnapshotQuery, ThemeSnapshotResult>
{
    public virtual async Task<ThemeSnapshotResult> Handle(GetThemeSnapshotQuery request, CancellationToken cancellationToken)
    {
        var storeRequest = AbstractTypeFactory<StoreDomainRequest>.TryCreateInstance();
        storeRequest.StoreId = request.StoreId;
        storeRequest.Domain = request.Domain;
        var store = await storeDomainResolverService.GetStoreAsync(storeRequest);
        if (store == null)
        {
            return null;
        }

        var snapshot = await themeSnapshotService.GetPublishedAsync(store.Id, request.CultureName, cancellationToken);
        if (snapshot == null)
        {
            return null;
        }

        var result = AbstractTypeFactory<ThemeSnapshotResult>.TryCreateInstance();
        result.StoreId = snapshot.StoreId;
        result.CultureName = snapshot.CultureName;
        result.Publication = snapshot.Publication;
        result.Content = snapshot.Content;
        result.Menus = await GetMenusAsync(snapshot);

        return result;
    }

    protected virtual async Task<IList<Menu>> GetMenusAsync(ThemeSnapshot snapshot)
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
