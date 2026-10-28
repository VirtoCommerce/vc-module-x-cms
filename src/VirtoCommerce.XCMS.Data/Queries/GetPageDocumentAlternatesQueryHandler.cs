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
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCMS.Core.Models;
using VirtoCommerce.XCMS.Core.Queries;

namespace VirtoCommerce.XCMS.Data.Queries;

public class GetPageDocumentAlternatesQueryHandler(
    IOptionalDependency<IPageDocumentSearchService> pageDocumentSearchService,
    Func<UserManager<ApplicationUser>> userManagerFactory,
    IMemberService memberService,
    IStoreService storeService)
    : IQueryHandler<GetPageDocumentAlternatesQuery, IList<PageDocumentAlternate>>
{
    public async Task<IList<PageDocumentAlternate>> Handle(GetPageDocumentAlternatesQuery request, CancellationToken cancellationToken)
    {
        if (!pageDocumentSearchService.HasValue || string.IsNullOrEmpty(request.TranslationGroupId))
        {
            return [];
        }

        // Without the store or with Pages turned off for it the storefront resolves none of these URLs.
        var store = await storeService.GetNoCloneAsync(request.StoreId);
        if (store == null || !store.Settings.GetValue<bool>(Pages.Core.ModuleConstants.Settings.General.Enable))
        {
            return [];
        }

        // Same visibility rules as the storefront page itself: published, in date range, allowed for the user.
        var criteria = AbstractTypeFactory<PageDocumentSearchCriteria>.TryCreateInstance();
        criteria.StoreId = request.StoreId;
        criteria.TranslationGroupIds = [request.TranslationGroupId];
        criteria.Status = PageDocumentStatus.Published;
        criteria.CertainDate = DateTime.UtcNow;
        criteria.OrganizationId = request.OrganizationId;

        // An anonymous visitor has no user to look up.
        var user = string.IsNullOrEmpty(request.UserId)
            ? null
            : userManagerFactory().Users.FirstOrDefault(x => x.Id == request.UserId);
        await criteria.EnrichAuth(user, memberService);

        var documents = await pageDocumentSearchService.Value.SearchAllNoCloneAsync(criteria);

        // A language removed from the store has no storefront URL, so its versions are not alternates.
        var storeLanguages = new HashSet<string>(store.Languages ?? [], StringComparer.OrdinalIgnoreCase);

        return documents
            .Where(x => !string.IsNullOrEmpty(x.Permalink) && !string.IsNullOrEmpty(x.CultureName) && storeLanguages.Contains(x.CultureName))
            .GroupBy(x => x.CultureName, StringComparer.OrdinalIgnoreCase)
            // One version per language is expected; if there are more, the latest one wins.
            // ModifiedDate is empty for a document that was never changed, so fall back to CreatedDate.
            .Select(x => x.OrderByDescending(document => document.ModifiedDate ?? document.CreatedDate).ThenBy(document => document.Id).First())
            .OrderBy(x => x.CultureName, StringComparer.OrdinalIgnoreCase)
            .Select(ToAlternate)
            .ToList();
    }

    private static PageDocumentAlternate ToAlternate(PageDocument document)
    {
        var alternate = AbstractTypeFactory<PageDocumentAlternate>.TryCreateInstance();
        alternate.Id = document.Id;
        alternate.CultureName = document.CultureName;
        alternate.Permalink = document.Permalink;
        return alternate;
    }
}
