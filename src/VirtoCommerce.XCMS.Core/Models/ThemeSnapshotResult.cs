using System.Collections.Generic;

namespace VirtoCommerce.XCMS.Core.Models;

public class ThemeSnapshotResult
{
    public string StoreId { get; set; }

    public string CultureName { get; set; }

    public int Publication { get; set; }

    public string Content { get; set; }

    public IList<Menu> Menus { get; set; } = [];

    public ThemeSnapshotSlug Slug { get; set; }
}
