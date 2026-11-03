using System.Collections.Generic;

namespace VirtoCommerce.XCMS.Core.Models;

/// <summary>A store's published Page Builder theme in one language, with the menus its header and footer name.</summary>
public class ThemeSnapshotResult
{
    public string StoreId { get; set; }

    public string CultureName { get; set; }

    public int Publication { get; set; }

    /// <summary>The snapshot JSON, as Page Builder published it.</summary>
    public string Content { get; set; }

    public IList<Menu> Menus { get; set; } = [];
}
