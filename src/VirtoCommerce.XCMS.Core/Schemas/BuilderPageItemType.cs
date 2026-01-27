using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Types;
using Microsoft.AspNetCore.Components.Web;
using VirtoCommerce.ContentModule.Core.Services;
using VirtoCommerce.PageBuilderModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.XCMS.Core.Models;

namespace VirtoCommerce.XCMS.Core.Schemas;

public class BuilderPageItemType : ObjectGraphType<BuilderPageItem>
{
    private readonly IOptionalDependency<IGroupedPageService> _groupedPageService;

    public BuilderPageItemType(IOptionalDependency<IGroupedPageService> groupedPageService)
    {
        _groupedPageService = groupedPageService;

        Field(x => x.Permalink, nullable: true).Description("Page permalink");
        Field(x => x.Content, nullable: true).ResolveAsync(LoadContent);
    }

    protected virtual async Task<string> LoadContent(IResolveFieldContext<BuilderPageItem> context)
    {
        if (_groupedPageService.HasValue && context.Source.Content.IsNullOrEmpty())
        {
            var pageId = context.Source.PageId;

            if (pageId != null)
            {
                return await _groupedPageService.Value.LoadContent(pageId);
            }
        }

        return context.Source.Content;
    }
}

