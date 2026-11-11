using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.ContentModule.Core.Model;
using VirtoCommerce.ContentModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCMS.Core.Models;
using VirtoCommerce.XCMS.Core.Queries;

namespace VirtoCommerce.XCMS.Data.Queries
{
    public class GetMenuQueryHandler : IQueryHandler<GetMenuQuery, GetMenuResponse>
    {
        private readonly IMenuLinkListSearchService _menuLinkListSearchService;

        public GetMenuQueryHandler(IMenuLinkListSearchService menuLinkListSearchService)
        {
            _menuLinkListSearchService = menuLinkListSearchService;
        }

        public async Task<GetMenuResponse> Handle(GetMenuQuery request, CancellationToken cancellationToken)
        {
            var result = new GetMenuResponse();

            var criteria = AbstractTypeFactory<MenuLinkListSearchCriteria>.TryCreateInstance();
            criteria.StoreId = request.StoreId;

            var menuLinkLists = await _menuLinkListSearchService.SearchAllAsync(criteria);
            result.MenuList = ToMenu(menuLinkLists, request.CultureName, request.Name);

            return result;
        }

        internal static Menu ToMenu(IList<MenuLinkList> menuLinkLists, string cultureName, string name)
        {
            var menuLinkList = GetMenuLinkList(menuLinkLists, cultureName, name);
            if (menuLinkList == null)
            {
                return null;
            }

            var expMenuLinkList = new Menu
            {
                Name = menuLinkList.Name,
                OuterId = menuLinkList.OuterId,
            };

            foreach (var menuLink in menuLinkList.MenuLinks)
            {
                var childMenuLinkList = GetMenuLinkList(menuLinkLists, cultureName, menuLink.Title);
                expMenuLinkList.Items.Add(GetMenuLink(menuLink, childMenuLinkList));
            }

            return expMenuLinkList;
        }

        private static MenuLinkList GetMenuLinkList(IEnumerable<MenuLinkList> lists, string cultureName, string name)
        {
            return lists.FirstOrDefault(x =>
                x.Language?.EqualsIgnoreCase(cultureName) == true &&
                x.Name.EqualsIgnoreCase(name));
        }

        private static MenuItem GetMenuLink(MenuLink link, MenuLinkList list)
        {
            return new MenuItem
            {
                Link = link,
                ChildItems = list?.MenuLinks?.Select(x => GetMenuLink(x, null)).ToList() ?? []
            };
        }
    }
}
