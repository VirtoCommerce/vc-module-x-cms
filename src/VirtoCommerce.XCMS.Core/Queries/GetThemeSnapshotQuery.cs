using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCMS.Core.Models;

namespace VirtoCommerce.XCMS.Core.Queries;

public class GetThemeSnapshotQuery : IQuery<ThemeSnapshotResult>
{
    public string Domain { get; set; }

    public string StoreId { get; set; }

    public string CultureName { get; set; }

    public string Permalink { get; set; }
}
