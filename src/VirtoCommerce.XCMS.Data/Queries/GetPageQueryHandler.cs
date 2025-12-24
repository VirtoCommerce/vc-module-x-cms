using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.ContentModule.Core.Model;
using VirtoCommerce.ContentModule.Core.Search;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCMS.Core.Models;
using VirtoCommerce.XCMS.Core.Queries;
using static VirtoCommerce.ContentModule.Core.ContentConstants;

namespace VirtoCommerce.XCMS.Data.Queries;

public class GetPageQueryHandler(
    IFullTextContentSearchService searchContentService,
    Func<UserManager<ApplicationUser>> userManagerFactory,
    IMemberService memberService)
    : IQueryHandler<GetPageQuery, GetPageResponse>
{

    public async Task<GetPageResponse> Handle(GetPageQuery request, CancellationToken cancellationToken)
    {
        var criteria = new ContentSearchCriteria
        {
            StoreId = request.StoreId,
            OrganizationId = request.OrganizationId,
            ActiveOn = DateTime.UtcNow,
            ContentType = ContentTypes.Pages,
            LanguageCode = request.CultureName,
            Keyword = request.Keyword,
            Take = request.Take,
            Skip = request.Skip,
        };

        var userManager = userManagerFactory();
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            // Filter will have the "any" value
            criteria.UserGroups = [];
        }
        else
        {
            if (user.IsAdministrator)
            {
                // Filter will not be applied when UserGroups is null
                criteria.UserGroups = null;
            }
            else
            {
                var member = await memberService.GetByIdAsync(user.MemberId);
                if (member != null && member.Groups != null)
                {
                    criteria.UserGroups = member.Groups.ToArray();
                }
                else
                {
                    criteria.UserGroups = [];
                }
            }
        }

        var result = await searchContentService.SearchAsync(criteria);
        var pages = result.Results.Select(x => new PageItem
        {
            Id = x.Id,
            Name = string.IsNullOrEmpty(x.DisplayName) ? x.Name : x.DisplayName,
            RelativeUrl = x.RelativeUrl,
            Permalink = x.Permalink
        });
        return new GetPageResponse { Pages = pages, TotalCount = result.TotalCount };
    }
}
