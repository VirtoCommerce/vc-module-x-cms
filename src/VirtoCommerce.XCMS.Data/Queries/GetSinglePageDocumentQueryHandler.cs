using System;
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
using VirtoCommerce.XCMS.Core.Queries;

namespace VirtoCommerce.XCMS.Data.Queries;

public class GetSinglePageDocumentQueryHandler(
    IOptionalDependency<IPageDocumentSearchService> pageDocumentSearchService,
    Func<UserManager<ApplicationUser>> userManagerFactory,
    IMemberService memberService
    )
    : IQueryHandler<GetSinglePageDocumentQuery, PageDocument>
{
    public async Task<PageDocument> Handle(GetSinglePageDocumentQuery request, CancellationToken cancellationToken)
    {
        if (!pageDocumentSearchService.HasValue)
        {
            return null;
        }

        var criteria = AbstractTypeFactory<PageDocumentSearchCriteria>.TryCreateInstance();
        criteria.ObjectIds = [request.Id];
        criteria.Take = 1;
        criteria.Skip = 0;
        criteria.CertainDate = DateTime.UtcNow;

        var userManager = userManagerFactory();
        var user = userManager.Users.FirstOrDefault(x => x.Id == request.UserId);
        await criteria.EnrichAuth(user, memberService);

        var result = await pageDocumentSearchService.Value.SearchAsync(criteria);
        var page = result.Results.FirstOrDefault();

        return page;
    }
}
