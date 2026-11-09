namespace VirtoCommerce.XCMS.Core.Models;

/// <summary>
/// What is at the permalink the snapshot was asked with, as the storefront's slug lookup finds it for an anonymous
/// visitor: what the storefront needs to choose the page's layout before the page loads.
/// </summary>
public class ThemeSnapshotSlug
{
    /// <summary>The slug's <c>objectType</c>: <c>Pages</c>, <c>ContentFile</c>, <c>CatalogProduct</c>, <c>Category</c>…</summary>
    public string ObjectType { get; set; }

    public string ObjectId { get; set; }

    /// <summary>A content page (<c>Pages</c>, <c>ContentFile</c>) as it was read; null for other types and a page not read.</summary>
    public ThemeSnapshotSlugPage Page { get; set; }
}
