using System;
using System.IO;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Types;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.XCMS.Core.Models;
using VirtoCommerce.XCMS.Core.Queries;

namespace VirtoCommerce.XCMS.Core.Schemas;

public class PageDocumentType : ObjectGraphType<PageDocument>
{
    public PageDocumentType()
    {
        Field(x => x.Id, nullable: false);
        Field(x => x.Title, nullable: true).Description("Page title");
        Field(x => x.Source, nullable: true).Description("Page source");
        Field(x => x.Permalink, nullable: true).Description("Page permalink");
        Field(x => x.Content, nullable: false).ResolveAsync(LoadContent);
        Field(x => x.CultureName, nullable: true).Description("Page language");
        Field<NonNullGraphType<ListGraphType<NonNullGraphType<PageDocumentAlternateType>>>>("alternates")
            .Description("Published versions of the page in the store languages, one per language; empty when the page has no translations")
            .ResolveAsync(LoadAlternates);
    }

    protected virtual async Task<object> LoadAlternates(IResolveFieldContext<PageDocument> context)
    {
        if (string.IsNullOrEmpty(context.Source?.TranslationGroupId))
        {
            return Array.Empty<PageDocumentAlternate>();
        }

        return await context.GetMediator().Send(new GetPageDocumentAlternatesQuery
        {
            StoreId = context.Source.StoreId,
            TranslationGroupId = context.Source.TranslationGroupId,
            UserId = context.GetCurrentUserId(),
            OrganizationId = context.GetCurrentOrganizationId(),
        });
    }

    protected virtual Task<string> LoadContent(IResolveFieldContext<PageDocument> context)
    {
        try
        {
            return Task.FromResult(context.Source?.Content);
        }
        catch (System.Exception)
        {
            throw new FileNotFoundException(context.Source?.Permalink);
        }
    }
}
