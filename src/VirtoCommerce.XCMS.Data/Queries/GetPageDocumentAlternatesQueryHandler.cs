using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Pages.Core.Extensions;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Pages.Core.Search;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCMS.Core.Models;
using VirtoCommerce.XCMS.Core.Queries;

namespace VirtoCommerce.XCMS.Data.Queries;

public class GetPageDocumentAlternatesQueryHandler(
    IOptionalDependency<IPageDocumentSearchService> pageDocumentSearchService,
    Func<UserManager<ApplicationUser>> userManagerFactory,
    IMemberService memberService)
    : IQueryHandler<GetPageDocumentAlternatesQuery, IList<PageDocumentAlternate>>
{
    public async Task<IList<PageDocumentAlternate>> Handle(GetPageDocumentAlternatesQuery request, CancellationToken cancellationToken)
    {
        if (!pageDocumentSearchService.HasValue || string.IsNullOrEmpty(request.TranslationGroupId))
        {
            return [];
        }

        // Same visibility rules as the storefront page itself: published, in date range, allowed for the user.
        var criteria = AbstractTypeFactory<PageDocumentSearchCriteria>.TryCreateInstance();
        criteria.StoreId = request.StoreId;
        criteria.TranslationGroupId = request.TranslationGroupId;
        criteria.Status = PageDocumentStatus.Published;
        criteria.CertainDate = DateTime.UtcNow;
        criteria.OrganizationId = request.OrganizationId;

        var userManager = userManagerFactory();
        var user = userManager.Users.FirstOrDefault(x => x.Id == request.UserId);
        await criteria.EnrichAuth(user, memberService);

        var documents = await pageDocumentSearchService.Value.SearchAllNoCloneAsync(criteria);

        return documents
            .Where(x => !string.IsNullOrEmpty(x.CultureName))
            .GroupBy(x => x.CultureName, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .OrderBy(x => x.CultureName, StringComparer.OrdinalIgnoreCase)
            .Select(x => new PageDocumentAlternate { Id = x.Id, CultureName = x.CultureName, Permalink = x.Permalink })
            .ToList();
    }
}
