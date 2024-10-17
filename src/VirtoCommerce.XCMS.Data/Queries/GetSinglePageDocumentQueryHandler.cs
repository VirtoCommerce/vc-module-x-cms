using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Pages.Core.Search;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCMS.Core.Queries;

namespace VirtoCommerce.XCMS.Data.Queries;

public class GetSinglePageDocumentQueryHandler(IPageDocumentSearchService pageDocumentSearchService)
    : IQueryHandler<GetSinglePageDocumentQuery, PageDocument>
{
    public async Task<PageDocument> Handle(GetSinglePageDocumentQuery request, CancellationToken cancellationToken)
    {
        var criteria = new PageDocumentSearchCriteria
        {
            ObjectIds = [request.Id],
            Take = 1,
            Skip = 0,
        };

        var result = await pageDocumentSearchService.SearchAsync(criteria);
        var page = result.Results.FirstOrDefault();

        return page;
    }
}
