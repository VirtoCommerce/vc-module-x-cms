using System.IO;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Types;
using VirtoCommerce.Pages.Core.Models;

namespace VirtoCommerce.XCMS.Core.Schemas;

public class PageDocumentType : ObjectGraphType<PageDocument>
{
    public PageDocumentType()
    {
        Field(x => x.Id, nullable: false);
        Field(x => x.Source, nullable: true).Description("Page source");
        // Field(x => x.RelativeUrl, nullable: true).Description("Page file relative url");
        Field(x => x.Permalink, nullable: true).Description("Page permalink");
        Field(x => x.Content, nullable: false).ResolveAsync(LoadContent);
    }

    protected virtual Task<string> LoadContent(IResolveFieldContext<PageDocument> context)
    {
        try
        {
            return Task.FromResult(context.Source?.Content);
        }
        catch (System.Exception)
        {
            // throw new FileNotFoundException(context.Source?.RelativeUrl);
            throw new FileNotFoundException(context.Source?.Permalink);
        }
    }
}
