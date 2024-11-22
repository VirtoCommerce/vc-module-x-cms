using System;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Builders;
using GraphQL.Resolvers;
using GraphQL.Types;
using MediatR;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Helpers;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCMS.Core.Queries;
using VirtoCommerce.XCMS.Core.Schemas;
using static VirtoCommerce.Xapi.Core.ModuleConstants;

namespace VirtoCommerce.XCMS.Data.Schemas
{
    public class PageDocumentSchema(IMediator mediator) : ISchemaBuilder
    {
        public void Build(ISchema schema)
        {
            _ = schema.Query.AddField(new FieldType
            {
                Name = "pageDocument",
                Arguments = new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" }
                ),
                Type = GraphTypeExtenstionHelper.GetActualType<PageDocumentType>(),
                Resolver = new AsyncFieldResolver<object>(async context =>
                {
                    context.CopyArgumentsToUserContext();

                    var result = await mediator.Send(new GetSinglePageDocumentQuery
                    {
                        UserId = context.GetCurrentUserId(),
                        Id = context.GetArgument<string>("id"),
                    });

                    return result;
                })
            });

            var pagesConnectionBuilder = GraphTypeExtenstionHelper.CreateConnection<PageDocumentType, object>()
                .Name("pageDocuments")
                .Argument<NonNullGraphType<StringGraphType>>("storeId", "The store id where pages are searched")
                .Argument<NonNullGraphType<StringGraphType>>("keyword", "The keyword parameter performs the full-text search")
                .Argument<StringGraphType>("cultureName", "The language for which all localized category data will be returned")
                .PageSize(Connections.DefaultPageSize);

            pagesConnectionBuilder.ResolveAsync(ResolvePagesConnection);

            schema.Query.AddField(pagesConnectionBuilder.FieldType);
        }

        private async Task<object> ResolvePagesConnection(IResolveConnectionContext<object> context)
        {
            context.CopyArgumentsToUserContext();

            var first = context.First;
            var skip = Convert.ToInt32(context.After ?? 0.ToString());

            var query = new GetPageDocumentsQuery
            {
                Skip = skip,
                Take = first ?? context.PageSize ?? Connections.DefaultPageSize,
                StoreId = context.GetArgument<string>("storeId"),
                CultureName = context.GetArgument<string>("cultureName"),
                Keyword = context.GetArgument<string>("keyword"),
                UserId = context.GetCurrentUserId(),
            };

            var response = await mediator.Send(query);

            return new PagedConnection<PageDocument>(response.Pages, query.Skip, query.Take, response.TotalCount);
        }
    }
}
