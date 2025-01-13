using GraphQL.Resolvers;
using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Helpers;
using VirtoCommerce.XCMS.Core.Models;

namespace VirtoCommerce.XCMS.Core.Schemas
{
    public class MenuLinkType : ObjectGraphType<MenuItem>
    {
        public MenuLinkType()
        {
            Field(x => x.Link.Title, nullable: false).Description("Menu item title");
            Field(x => x.Link.Url, nullable: false).Description("Menu item url");
            Field(x => x.Link.Priority, nullable: false).Description("Menu item priority");
            Field(x => x.Link.AssociatedObjectId, nullable: true).Description("Menu item object ID");
            Field(x => x.Link.AssociatedObjectName, nullable: true).Description("Menu item object name");
            Field(x => x.Link.AssociatedObjectType, nullable: true).Description("Menu item type name");
            Field(x => x.Link.OuterId, nullable: true).Description("Menu item outerID");
            AddField(new FieldType
            {
                Name = nameof(MenuItem.ChildItems),
                Type = GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<ListGraphType<NonNullGraphType<MenuLinkType>>>>(),
                Resolver = new FuncFieldResolver<MenuItem, object>(context => context.Source?.ChildItems)
            });
        }
    }
}
