using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.XCMS.Core.Queries;

public class GetSinglePageDocumentQuery : IQuery<PageDocument>
{
    public string Id { get; set; }
}
