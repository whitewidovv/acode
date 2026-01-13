using Acode.Domain.Security.PathProtection;

namespace Acode.Application.Security.Queries;

/// <summary>
/// Handler for GetDenylistQuery that retrieves and filters the denylist.
/// </summary>
public sealed class GetDenylistHandler
{
    private readonly IReadOnlyList<DenylistEntry> _denylist;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetDenylistHandler"/> class.
    /// </summary>
    public GetDenylistHandler()
    {
        _denylist = DefaultDenylist.Entries;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GetDenylistHandler"/> class with custom denylist.
    /// </summary>
    /// <param name="denylist">Custom denylist (e.g., including user extensions).</param>
    public GetDenylistHandler(IReadOnlyList<DenylistEntry> denylist)
    {
        _denylist = denylist ?? throw new ArgumentNullException(nameof(denylist));
    }

    /// <summary>
    /// Handles the GetDenylistQuery.
    /// </summary>
    /// <param name="query">The query to handle.</param>
    /// <returns>Filtered denylist entries.</returns>
    public IReadOnlyList<DenylistEntry> Handle(GetDenylistQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var entries = _denylist.AsEnumerable();

        if (query.CategoryFilter.HasValue)
        {
            entries = entries.Where(e => e.Category == query.CategoryFilter.Value);
        }

        if (query.PlatformFilter.HasValue)
        {
            entries = entries.Where(e =>
                e.Platforms.Contains(Platform.All) ||
                e.Platforms.Contains(query.PlatformFilter.Value));
        }

        if (!query.IncludeUserDefined)
        {
            entries = entries.Where(e => e.IsDefault);
        }

        return entries.ToList().AsReadOnly();
    }
}
