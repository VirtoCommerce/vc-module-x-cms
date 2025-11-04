using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.XCMS.Core.Queries;

/// <summary>
/// Get all page documents from the search index for current date.
/// </summary>
public class GetPageDocumentsQuery : IQuery<GetPageDocumentsResponse>
{
    public string UserId { get; set; }
    public string StoreId { get; set; }
    public string CultureName { get; set; }
    public string Keyword { get; set; }
    public string OrganizationId { get; set; }

    public int Skip { get; set; }
    public int Take { get; set; }
}
