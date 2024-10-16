using GraphQL.Resolvers;
using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Helpers;
using VirtoCommerce.XCMS.Core.Models;

namespace VirtoCommerce.XCMS.Core.Schemas
{
    public class MenuLinkListType : ObjectGraphType<Menu>
    {
        public MenuLinkListType()
        {
            Field(x => x.Name, nullable: false).Description("Menu name");
            Field(x => x.OuterId, nullable: true).Description("Menu outer ID");
            AddField(new FieldType
            {
                Name = "items",
                Type = GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<ListGraphType<NonNullGraphType<MenuLinkType>>>>(),
                Resolver = new FuncFieldResolver<Menu, object>(context => context.Source?.Items)
            });
        }
    }
}
