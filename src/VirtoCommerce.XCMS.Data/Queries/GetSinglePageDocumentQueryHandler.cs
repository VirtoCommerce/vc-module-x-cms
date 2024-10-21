using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Pages.Core.Search;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCMS.Core.Queries;

namespace VirtoCommerce.XCMS.Data.Queries;

public class GetSinglePageDocumentQueryHandler(IPageDocumentSearchService pageDocumentSearchService)
    : IQueryHandler<GetSinglePageDocumentQuery, PageDocument>
{
    public async Task<PageDocument> Handle(GetSinglePageDocumentQuery request, CancellationToken cancellationToken)
    {
        var criteria = AbstractTypeFactory<PageDocumentSearchCriteria>.TryCreateInstance();
        criteria.ObjectIds = [request.Id];
        criteria.Take = 1;
        criteria.Skip = 0;

        var result = await pageDocumentSearchService.SearchAsync(criteria);
        var page = result.Results.FirstOrDefault(x => x.Status == PageDocumentStatus.Published && x.Visibility == PageDocumentVisibility.Public);

        return page;
    }
}
