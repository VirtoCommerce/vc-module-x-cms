using System;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Builders;
using GraphQL.Resolvers;
using GraphQL.Types;
using MediatR;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Helpers;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCMS.Core.Models;
using VirtoCommerce.XCMS.Core.Queries;
using VirtoCommerce.XCMS.Core.Schemas;
using static VirtoCommerce.Xapi.Core.ModuleConstants;

namespace VirtoCommerce.XCMS.Data.Schemas
{
    public class ContentSchema : ISchemaBuilder
    {
        private readonly IMediator _mediator;

        public ContentSchema(IMediator mediator)
        {
            _mediator = mediator;
        }

        public void Build(ISchema schema)
        {
            _ = schema.Query.AddField(new FieldType
            {
                Name = "menu",
                Arguments = new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "storeId" },
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "cultureName" },
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "name" }
                ),
                Type = GraphTypeExtensionHelper.GetActualType<MenuLinkListType>(),
                Resolver = new FuncFieldResolver<object>(async context =>
                {
                    var result = await _mediator.Send(new GetMenuQuery
                    {
                        StoreId = context.GetArgument<string>("storeId"),
                        CultureName = context.GetArgument<string>("cultureName"),
                        Name = context.GetArgument<string>("name"),
                    });

                    return result.MenuList;
                })
            });

            _ = schema.Query.AddField(new FieldType
            {
                Name = "menus",
                Arguments = new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "storeId" },
                    new QueryArgument<StringGraphType> { Name = "cultureName" },
                    new QueryArgument<StringGraphType> { Name = "keyword" }
                ),
                Type = GraphTypeExtensionHelper.GetActualType<NonNullGraphType<ListGraphType<NonNullGraphType<MenuLinkListType>>>>(),
                Resolver = new FuncFieldResolver<object>(async context =>
                {
                    var result = await _mediator.Send(new GetMenusQuery
                    {
                        StoreId = context.GetArgument<string>("storeId"),
                        CultureName = context.GetArgument<string>("cultureName"),
                        Keyword = context.GetArgument<string>("keyword"),
                    });

                    return result.Menus;
                })
            });

            _ = schema.Query.AddField(new FieldType
            {
                Name = "page",
                Arguments = new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "storeId" },
                    new QueryArgument<StringGraphType> { Name = "cultureName" },
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" }
                ),
                Type = GraphTypeExtensionHelper.GetActualType<PageType>(),
                Resolver = new FuncFieldResolver<object>(async context =>
                {
                    context.CopyArgumentsToUserContext();

                    var result = await _mediator.Send(new GetSinglePageQuery
                    {
                        StoreId = context.GetArgument<string>("storeId"),
                        CultureName = context.GetArgument<string>("cultureName"),
                        OrganizationId = context.GetCurrentOrganizationId(),
                        Id = context.GetArgument<string>("id"),
                    });

                    return result;
                })
            });

            var pagesConnectionBuilder = GraphTypeExtensionHelper.CreateConnection<PageType, object>("pages")
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

            var query = new GetPageQuery
            {
                Skip = skip,
                Take = first ?? context.PageSize ?? Connections.DefaultPageSize,
                StoreId = context.GetArgument<string>("storeId"),
                CultureName = context.GetArgument<string>("cultureName"),
                OrganizationId = context.GetCurrentOrganizationId(),
                Keyword = context.GetArgument<string>("keyword"),
            };

            var response = await _mediator.Send(query);

            return new PagedConnection<PageItem>(response.Pages, query.Skip, query.Take, response.TotalCount);
        }
    }
}
