namespace VirtoCommerce.XCMS.Core.Models;

/// <summary>The parts of a content page the storefront chooses its template and layout by.</summary>
public class ThemeSnapshotSlugPage
{
    /// <summary>A page from the database: its document's source (<c>page-builder</c>, <c>builder.io</c>). Null for a file page.</summary>
    public string Source { get; set; }

    /// <summary>The page's permalink, as the page document or the file holds it.</summary>
    public string Permalink { get; set; }

    /// <summary>
    /// A JSON object of the page's own settings that choose its template and layout: <c>layout</c>, <c>template</c> and
    /// the settings the page rules' <c>setting</c> conditions name, those the page has. Null when the page's content is
    /// not a JSON object with a <c>settings</c> object (a markdown file, a document of another source).
    /// </summary>
    public string Settings { get; set; }
}
