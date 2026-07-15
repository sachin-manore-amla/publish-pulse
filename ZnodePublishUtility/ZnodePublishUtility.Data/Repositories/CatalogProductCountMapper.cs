using ZnodePublishUtility.Models.DTOs;

namespace ZnodePublishUtility.Data.Repositories;

/// <summary>
/// Maps a raw <c>dbo.GetCatalogProductCounts</c> result row to <see cref="CatalogProductCountDto"/>.
/// Kept separate from <see cref="CatalogProductCountRepository"/> so the mapping can be unit
/// tested without a live SQL connection.
/// </summary>
public static class CatalogProductCountMapper
{
    /// <summary>
    /// Maps by column position — 1st column is the catalog id, 2nd is the draft product count —
    /// rather than by column name, since the stored procedure's exact column names/casing
    /// aren't guaranteed.
    /// </summary>
    public static CatalogProductCountDto MapRow(IDictionary<string, object> row)
    {
        var values = row.Values.ToList();

        return new CatalogProductCountDto
        {
            CatalogId = values.ElementAtOrDefault(0)?.ToString() ?? "",
            Count = TryToInt32(values.ElementAtOrDefault(1)),
        };
    }

    private static int TryToInt32(object? value)
    {
        if (value is null or DBNull) return 0;
        return value switch
        {
            int i => i,
            _ when int.TryParse(value.ToString(), out var parsed) => parsed,
            _ => 0,
        };
    }
}
