using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VirtoCommerce.ContentModule.Core.Model;
using VirtoCommerce.ContentModule.Core.Services;
using VirtoCommerce.PageBuilderModule.Core.Theme;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Models;
using VirtoCommerce.Xapi.Core.Queries;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XCMS.Core.Models;
using VirtoCommerce.XCMS.Core.Queries;
using static VirtoCommerce.ContentModule.Core.ContentConstants;

namespace VirtoCommerce.XCMS.Data.Queries;

/// <summary>
/// A store's published Page Builder theme with the menus it names and what is at the permalink asked for. The menus and
/// the permalink are read on every request, outside the cached snapshot, so a menu edited in Content shows without a new
/// publication. The permalink is looked up in the language the snapshot is served in.
/// </summary>
public class GetThemeSnapshotQueryHandler(
    IStoreDomainResolverService storeDomainResolverService,
    IMenuLinkListSearchService menuLinkListSearchService,
    IThemeSnapshotService themeSnapshotService,
    IMediator mediator,
    IContentService contentService,
    ILogger<GetThemeSnapshotQueryHandler> logger)
    : IQueryHandler<GetThemeSnapshotQuery, ThemeSnapshotResult>
{
    /// <summary>The slug object type of a page from the database.</summary>
    public const string PagesObjectType = "Pages";

    /// <summary>The slug object type of a file page.</summary>
    public const string ContentFileObjectType = "ContentFile";

    /// <summary>The storefront's page rule condition that reads a page setting: <c>{ "setting": { "name", "equals" } }</c>.</summary>
    protected const string SettingConditionName = "setting";

    /// <summary>The page settings that choose a content page's template and layout on their own.</summary>
    protected static readonly IReadOnlyList<string> TemplateSettingNames = ["layout", "template"];

    public virtual async Task<ThemeSnapshotResult> Handle(GetThemeSnapshotQuery request, CancellationToken cancellationToken)
    {
        var storeRequest = AbstractTypeFactory<StoreDomainRequest>.TryCreateInstance();
        storeRequest.StoreId = request.StoreId;
        storeRequest.Domain = request.Domain;
        var store = await storeDomainResolverService.GetStoreAsync(storeRequest);
        // A store without the New Page Builder has no snapshot: answered without looking up the permalink.
        if (store == null || !await themeSnapshotService.IsNewPageBuilderEnabledAsync(store.Id, cancellationToken))
        {
            return null;
        }

        var snapshot = await themeSnapshotService.GetPublishedAsync(store.Id, request.CultureName, cancellationToken);
        if (snapshot == null)
        {
            return null;
        }

        var result = AbstractTypeFactory<ThemeSnapshotResult>.TryCreateInstance();
        result.StoreId = snapshot.StoreId;
        result.CultureName = snapshot.CultureName;
        result.Publication = snapshot.Publication;
        result.Content = snapshot.Content;
        result.Menus = await GetMenusAsync(snapshot);
        result.Slug = await GetSlugAsync(store, request, snapshot, cancellationToken);

        return result;
    }

    protected virtual async Task<IList<Menu>> GetMenusAsync(ThemeSnapshot snapshot)
    {
        if (snapshot.Menus.Count == 0)
        {
            return [];
        }

        var criteria = AbstractTypeFactory<MenuLinkListSearchCriteria>.TryCreateInstance();
        criteria.StoreId = snapshot.StoreId;
        var menuLinkLists = await menuLinkListSearchService.SearchAllAsync(criteria);

        return snapshot.Menus
            .Select(name => (Name: name, Menu: GetMenuQueryHandler.ToMenu(menuLinkLists, snapshot.CultureName, name)))
            .Where(x => x.Menu != null)
            .Select(x =>
            {
                // under the name the theme holds, which the storefront looks it up by
                x.Menu.Name = x.Name;
                return x.Menu;
            })
            .ToList();
    }

    /// <summary>
    /// Looks the permalink up with the storefront's <c>slugInfo</c> query, for an anonymous visitor in the language served
    /// (a URL may name only its language part), and reads a content page found there. Null without a permalink, with
    /// nothing at the permalink (or a redirect) and when the lookup fails.
    /// </summary>
    protected virtual async Task<ThemeSnapshotSlug> GetSlugAsync(Store store, GetThemeSnapshotQuery request, ThemeSnapshot snapshot, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Permalink))
        {
            return null;
        }

        try
        {
            var slugQuery = AbstractTypeFactory<SlugInfoQuery>.TryCreateInstance();
            slugQuery.Permalink = request.Permalink;
            slugQuery.StoreId = store.Id;
            slugQuery.CultureName = snapshot.CultureName;
            var slugInfo = await mediator.Send(slugQuery, cancellationToken);

            var entity = slugInfo?.EntityInfo;
            if (string.IsNullOrEmpty(entity?.ObjectType) || string.IsNullOrEmpty(entity.ObjectId))
            {
                return null;
            }

            var slug = AbstractTypeFactory<ThemeSnapshotSlug>.TryCreateInstance();
            slug.ObjectType = entity.ObjectType;
            slug.ObjectId = entity.ObjectId;
            slug.Page = await GetSlugPageAsync(store, slug, snapshot, cancellationToken);

            return slug;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "The theme snapshot could not look up the permalink {Permalink} of the store {StoreId}", request.Permalink, store.Id);
            return null;
        }
    }

    /// <summary>
    /// Reads a content page at the permalink with the storefront's <c>pageDocument</c> and <c>page</c> queries, keeping
    /// only the settings that choose its template and layout. Null for other object types and a page not found.
    /// </summary>
    protected virtual async Task<ThemeSnapshotSlugPage> GetSlugPageAsync(Store store, ThemeSnapshotSlug slug, ThemeSnapshot snapshot, CancellationToken cancellationToken)
    {
        if (slug.ObjectType is not (PagesObjectType or ContentFileObjectType))
        {
            return null;
        }

        var page = AbstractTypeFactory<ThemeSnapshotSlugPage>.TryCreateInstance();
        JObject settings;

        switch (slug.ObjectType)
        {
            case PagesObjectType:
                {
                    var document = await mediator.Send(new GetSinglePageDocumentQuery { Id = slug.ObjectId }, cancellationToken);
                    if (document == null)
                    {
                        return null;
                    }

                    page.Source = document.Source;
                    page.Permalink = document.Permalink;
                    using var reader = new StringReader(document.Content ?? string.Empty);
                    settings = ReadSettings(reader);
                    break;
                }
            case ContentFileObjectType:
                {
                    var file = await mediator.Send(new GetSinglePageQuery { StoreId = store.Id, CultureName = snapshot.CultureName, Id = slug.ObjectId }, cancellationToken);
                    if (file == null)
                    {
                        return null;
                    }

                    page.Permalink = file.Permalink;
                    await using var stream = await contentService.GetItemStreamAsync(ContentTypes.Pages, store.Id, file.RelativeUrl);
                    using var reader = new StreamReader(stream);
                    settings = ReadSettings(reader);
                    break;
                }
            default:
                return null;
        }

        if (settings != null)
        {
            var names = GetTemplateSettingNames(snapshot);
            page.Settings = new JObject(settings.Properties().Where(x => names.Contains(x.Name))).ToString(Formatting.None);
        }

        return page;
    }

    /// <summary>
    /// The page settings the storefront chooses a content page's template and layout by: <c>layout</c>, <c>template</c>
    /// and the names the snapshot's page rules give their <c>setting</c> condition. The rules are read, never applied.
    /// </summary>
    protected virtual ISet<string> GetTemplateSettingNames(ThemeSnapshot snapshot)
    {
        var names = new HashSet<string>(TemplateSettingNames, StringComparer.Ordinal);

        if (string.IsNullOrEmpty(snapshot?.Content))
        {
            return names;
        }

        using var reader = new StringReader(snapshot.Content);

        if (ReadProperty(reader, "rules") is JObject rules && rules["page"] is JArray pageRules)
        {
            foreach (var rule in pageRules.OfType<JObject>())
            {
                if (rule["when"] is JObject when && when[SettingConditionName] is JObject condition && condition["name"] is JValue { Type: JTokenType.String } name)
                {
                    names.Add((string)name);
                }
            }
        }

        return names;
    }

    /// <summary>The <c>settings</c> object of a page's JSON content; null when the content is not such JSON.</summary>
    protected static JObject ReadSettings(TextReader content)
    {
        return ReadProperty(content, "settings") as JObject;
    }

    /// <summary>
    /// A property of the JSON object the reader holds, read without loading the rest of it. Null without the property and
    /// for content that is not a JSON object.
    /// </summary>
    protected static JToken ReadProperty(TextReader content, string name)
    {
        using var reader = new JsonTextReader(content) { CloseInput = false };

        try
        {
            if (!reader.Read() || reader.TokenType != JsonToken.StartObject)
            {
                return null;
            }

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var propertyName = (string)reader.Value;

                if (!reader.Read())
                {
                    return null;
                }

                if (propertyName == name)
                {
                    return JToken.ReadFrom(reader);
                }

                reader.Skip();
            }
        }
        catch (JsonReaderException)
        {
            // markdown, or another file that is not JSON
        }

        return null;
    }
}
