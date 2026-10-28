namespace VirtoCommerce.XCMS.Core.Models;

/// <summary>
/// A translation of a page document: where the same page lives in another language.
/// </summary>
public class PageDocumentAlternate
{
    public string Id { get; set; }

    public string CultureName { get; set; }

    public string Permalink { get; set; }
}
