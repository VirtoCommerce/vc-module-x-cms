using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCMS.Core.Models;

namespace VirtoCommerce.XCMS.Core.Queries;

public class GetThemeSnapshotQuery : IQuery<ThemeSnapshotResult>
{
    /// <summary>The store by domain, used when <see cref="StoreId"/> is empty.</summary>
    public string Domain { get; set; }

    /// <summary>The store by id; wins over <see cref="Domain"/>.</summary>
    public string StoreId { get; set; }

    /// <summary>The language asked for; the store's closest language is served.</summary>
    public string CultureName { get; set; }

    /// <summary>The page the visitor landed on, as the storefront resolves its slug; optional.</summary>
    public string Permalink { get; set; }
}
