using System.Collections.Generic;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCMS.Core.Models;

namespace VirtoCommerce.XCMS.Core.Queries;

/// <summary>
/// Published translations of a page that the current user can see, one per language.
/// </summary>
public class GetPageDocumentAlternatesQuery : IQuery<IList<PageDocumentAlternate>>
{
    public string StoreId { get; set; }
    public string TranslationGroupId { get; set; }
    public string UserId { get; set; }
    public string OrganizationId { get; set; }
}
