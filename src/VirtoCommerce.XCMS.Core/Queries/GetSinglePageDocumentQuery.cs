using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.XCMS.Core.Queries;

/// <summary>
/// Get a single page document from the search index for the current date.
/// </summary>
public class GetSinglePageDocumentQuery : IQuery<PageDocument>
{
    public string Id { get; set; }
    public string UserId { get; set; }
}
