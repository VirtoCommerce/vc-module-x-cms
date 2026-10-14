using GraphQL.Types;
using VirtoCommerce.XCMS.Core.Models;

namespace VirtoCommerce.XCMS.Core.Schemas;

public class PageDocumentAlternateType : ObjectGraphType<PageDocumentAlternate>
{
    public PageDocumentAlternateType()
    {
        Field(x => x.Id, nullable: false).Description("Page document id of the translation");
        Field(x => x.CultureName, nullable: false).Description("Language of the translation");
        Field(x => x.Permalink, nullable: false).Description("Permalink of the translation");
    }
}
