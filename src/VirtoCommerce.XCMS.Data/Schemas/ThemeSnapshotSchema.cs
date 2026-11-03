using GraphQL.Resolvers;
using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Helpers;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCMS.Core.Queries;
using VirtoCommerce.XCMS.Core.Schemas;

namespace VirtoCommerce.XCMS.Data.Schemas;

/// <summary>
/// The published Page Builder theme a storefront requests from index.html before its JavaScript loads: anonymous,
/// the same for every visitor; null when the store has the New Page Builder setting off.
/// </summary>
public class ThemeSnapshotSchema : ISchemaBuilder
{
    public void Build(ISchema schema)
    {
        _ = schema.Query.AddField(new FieldType
        {
            Name = "themeSnapshot",
            Arguments = new QueryArguments(
                new QueryArgument<StringGraphType> { Name = "domain" },
                new QueryArgument<StringGraphType> { Name = "storeId" },
                new QueryArgument<StringGraphType> { Name = "cultureName" }
            ),
            Type = GraphTypeExtensionHelper.GetActualType<ThemeSnapshotType>(),
            Resolver = new FuncFieldResolver<object>(async context =>
                await context.GetMediator().Send(new GetThemeSnapshotQuery
                {
                    Domain = context.GetArgument<string>("domain"),
                    StoreId = context.GetArgument<string>("storeId"),
                    CultureName = context.GetArgument<string>("cultureName"),
                })),
        });
    }
}
