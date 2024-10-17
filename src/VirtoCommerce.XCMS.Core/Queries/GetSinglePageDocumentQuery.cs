using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.XCMS.Core.Queries;

public class GetSinglePageDocumentQuery : IQuery<PageDocument>
{
    //public string StoreId { get; set; }
    //public string CultureName { get; set; }
    public string Id { get; set; }
}
