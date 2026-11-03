using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XCMS.Core.Models;

namespace VirtoCommerce.XCMS.Core.Schemas;

public class ThemeSnapshotType : ExtendableGraphType<ThemeSnapshotResult>
{
    public ThemeSnapshotType()
    {
        Name = "ThemeSnapshot";

        Field(x => x.StoreId, nullable: false).Description("The store the snapshot belongs to");
        Field(x => x.CultureName, nullable: false).Description("The language the texts are in");
        Field(x => x.Publication, nullable: false).Description("The number of the theme publication");
        Field(x => x.Content, nullable: false).Description("The snapshot JSON: rules, templates, header and footer, brand and palette");
        Field<NonNullGraphType<ListGraphType<NonNullGraphType<MenuLinkListType>>>>("menus")
            .Description("The link lists the header and footer name, under the names they hold")
            .Resolve(context => context.Source.Menus);
    }
}
