using GraphQL.Resolvers;
using GraphQL.Types;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Helpers;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCMS.Core.Queries;
using VirtoCommerce.XCMS.Core.Schemas;

namespace VirtoCommerce.XCMS.Data.Schemas;

/// <summary>
/// A store's published Page Builder theme. Anonymous, because the theme is the same for every visitor.
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
            {
                var query = AbstractTypeFactory<GetThemeSnapshotQuery>.TryCreateInstance();
                query.Domain = context.GetArgument<string>("domain");
                query.StoreId = context.GetArgument<string>("storeId");
                query.CultureName = context.GetArgument<string>("cultureName");

                return await context.GetMediator().Send(query);
            }),
        });
    }
}
