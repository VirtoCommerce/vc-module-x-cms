using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Pages.Core.Search;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCMS.Core.Queries;

namespace VirtoCommerce.XCMS.Data.Queries;

public class GetSinglePageDocumentQueryHandler(
    IPageDocumentSearchService pageDocumentSearchService,
    Func<UserManager<ApplicationUser>> userManagerFactory,
    IMemberService memberService
    )
    : IQueryHandler<GetSinglePageDocumentQuery, PageDocument>
{
    public async Task<PageDocument> Handle(GetSinglePageDocumentQuery request, CancellationToken cancellationToken)
    {
        var criteria = AbstractTypeFactory<PageDocumentSearchCriteria>.TryCreateInstance();
        criteria.UserGroups = [];
        criteria.ObjectIds = [request.Id];
        criteria.Take = 1;
        criteria.Skip = 0;

        var member = await FindMember(request.UserId);
        if (member != null)
        {
            criteria.UserGroups = member.Groups.ToArray();
        }

        var result = await pageDocumentSearchService.SearchAsync(criteria);
        var page = result.Results.FirstOrDefault(x => x.Status == PageDocumentStatus.Published && x.Visibility == PageDocumentVisibility.Public);

        return page;
    }

    private async Task<Member> FindMember(string userId)
    {
        var userManager = userManagerFactory();
        var user = userManager.Users.FirstOrDefault(x => x.Id == userId);

        if (user != null)
        {
            return await memberService.GetByIdAsync(user.MemberId);
        }

        return null;
    }
}
