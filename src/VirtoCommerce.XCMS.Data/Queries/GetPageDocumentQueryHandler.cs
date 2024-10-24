using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Pages.Core.Search;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCMS.Core.Queries;

namespace VirtoCommerce.XCMS.Data.Queries;

public class GetPageDocumentQueryHandler(IPageDocumentSearchService pageDocumentSearchService)
    : IQueryHandler<GetPageDocumentsQuery, GetPageDocumentsResponse>
{

    public async Task<GetPageDocumentsResponse> Handle(GetPageDocumentsQuery request, CancellationToken cancellationToken)
    {
        var criteria = AbstractTypeFactory<PageDocumentSearchCriteria>.TryCreateInstance();
        criteria.StoreId = request.StoreId;
        criteria.LanguageCode = request.CultureName;
        criteria.Keyword = request.Keyword;
        criteria.Take = request.Take;
        criteria.Skip = request.Skip;

        var result = await pageDocumentSearchService.SearchAsync(criteria);

        return new GetPageDocumentsResponse { Pages = result.Results, TotalCount = result.TotalCount };
    }
}
