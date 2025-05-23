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

public class GetPageDocumentQueryHandler(IOptionalDependency<IPageDocumentSearchService> pageDocumentSearchService,
    Func<UserManager<ApplicationUser>> userManagerFactory,
    IMemberService memberService)
    : IQueryHandler<GetPageDocumentsQuery, GetPageDocumentsResponse>
{

    public async Task<GetPageDocumentsResponse> Handle(GetPageDocumentsQuery request, CancellationToken cancellationToken)
    {
        if (!pageDocumentSearchService.HasValue)
        {
            return new GetPageDocumentsResponse { Pages = Array.Empty<PageDocument>(), TotalCount = 0 };
        }

        var criteria = AbstractTypeFactory<PageDocumentSearchCriteria>.TryCreateInstance();
        criteria.StoreId = request.StoreId;
        criteria.LanguageCode = request.CultureName;
        criteria.Keyword = request.Keyword;
        criteria.Take = request.Take;
        criteria.Skip = request.Skip;
        criteria.CertainDate = DateTime.UtcNow;

        var userManager = userManagerFactory();
        var user = userManager.Users.FirstOrDefault(x => x.Id == request.UserId);
        await criteria.EnrichAuth(user, memberService);

        var result = await pageDocumentSearchService.Value.SearchAsync(criteria);

        return new GetPageDocumentsResponse { Pages = result.Results, TotalCount = result.TotalCount };
    }
}
