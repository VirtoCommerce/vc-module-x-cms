using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCMS.Core.Models;

namespace VirtoCommerce.XCMS.Core.Queries;

public class GetBuilderPageQuery : IQuery<BuilderPageItem>
{
    public string StoreId { get; set; }
    public string PageId { get; set; }
}
