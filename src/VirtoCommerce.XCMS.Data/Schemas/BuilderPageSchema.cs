using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphQL.Resolvers;
using GraphQL.Types;
using MediatR;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Helpers;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCMS.Core.Queries;
using VirtoCommerce.XCMS.Core.Schemas;

namespace VirtoCommerce.XCMS.Data.Schemas;

public class BuilderPageSchema : ISchemaBuilder
{
    public BuilderPageSchema()
    {
    }

    [Obsolete("Use the constructor without IMediator. The mediator is resolved from context.RequestServices per request.", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    public BuilderPageSchema(IMediator mediator)
        : this()
    {
    }

    public void Build(ISchema schema)
    {
        _ = schema.Query.AddField(new FieldType
        {
            Name = "builderPage",
            Arguments = new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "storeId" },
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "pageId" }
            ),
            Type = GraphTypeExtensionHelper.GetActualType<BuilderPageItemType>(),
            Resolver = new FuncFieldResolver<object>(async context =>
            {
                context.CopyArgumentsToUserContext();

                var result = await context.GetMediator().Send(new GetBuilderPageQuery
                {
                    StoreId = context.GetArgument<string>("storeId"),
                    PageId = context.GetArgument<string>("pageId"),
                });

                return result;
            })
        });
    }
}
