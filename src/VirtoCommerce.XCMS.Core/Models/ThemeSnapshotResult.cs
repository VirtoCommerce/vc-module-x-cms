using System.Collections.Generic;

namespace VirtoCommerce.XCMS.Core.Models;

/// <summary>
/// A store's published Page Builder theme in one language, with the menus its header and footer name and what is at the
/// permalink asked for.
/// </summary>
public class ThemeSnapshotResult
{
    public string StoreId { get; set; }

    public string CultureName { get; set; }

    public int Publication { get; set; }

    /// <summary>The snapshot JSON, as Page Builder published it.</summary>
    public string Content { get; set; }

    public IList<Menu> Menus { get; set; } = [];

    /// <summary>What is at the permalink the snapshot was asked with; null without one or with nothing at it.</summary>
    public ThemeSnapshotSlug Slug { get; set; }
}
