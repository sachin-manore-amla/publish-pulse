using System.Text.Json;

namespace ZnodePublishUtility.API.Infrastructure.Elasticsearch;

/// <summary>
/// Combines the raw <c>_cat/indices/{index}</c> and <c>{index}/_mapping</c> responses into a
/// single <see cref="IndexInformationDto"/>. Kept separate from <see cref="IElasticsearchClient"/>
/// so the aggregation rules can be unit tested with fixture JSON instead of a live cluster.
/// </summary>
public static class ElasticsearchIndexAggregator
{
    /// <summary>
    /// Aggregates index stats (health, doc count, store size) and mapping into one DTO.
    /// </summary>
    /// <param name="indexName">Index name requested by the caller (used as a fallback label).</param>
    /// <param name="stats">Root document returned by GET /_cat/indices/{index}?format=json&amp;bytes=b.</param>
    /// <param name="mapping">Root document returned by GET /{index}/_mapping.</param>
    /// <remarks>
    /// <c>_cat</c> endpoints always return string-typed values even with <c>format=json</c>, so
    /// doc count/size are parsed defensively and default to 0 when missing or non-numeric.
    /// </remarks>
    public static IndexInformationDto Aggregate(string indexName, JsonDocument stats, JsonDocument mapping)
    {
        var dto = new IndexInformationDto { IndexName = indexName, IndexSize = FormatBytes(0), Mapping = EmptyObject() };

        ApplyStats(stats.RootElement, dto);
        ApplyMapping(mapping.RootElement, dto);

        return dto;
    }

    private static void ApplyStats(JsonElement statsRoot, IndexInformationDto dto)
    {
        if (statsRoot.ValueKind != JsonValueKind.Array || statsRoot.GetArrayLength() == 0)
        {
            return;
        }

        var row = statsRoot[0];

        var resolvedName = GetString(row, "index");
        if (resolvedName.Length > 0) dto.IndexName = resolvedName;

        dto.Health = GetString(row, "health");
        dto.Status = GetString(row, "status");
        dto.DocumentCount = GetLong(row, "docs.count");
        dto.IndexSizeBytes = GetLong(row, "store.size");
        dto.IndexSize = FormatBytes(dto.IndexSizeBytes);
    }

    private static void ApplyMapping(JsonElement mappingRoot, IndexInformationDto dto)
    {
        if (mappingRoot.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        // Response shape: { "<resolved-index-name>": { "mappings": {...} } }
        foreach (var indexEntry in mappingRoot.EnumerateObject())
        {
            if (indexEntry.Value.TryGetProperty("mappings", out var mappings))
            {
                dto.Mapping = mappings.Clone();
            }
            break;
        }
    }

    private static string GetString(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Object &&
        element.TryGetProperty(propertyName, out var value) &&
        value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";

    private static long GetLong(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var value))
        {
            return 0;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt64(out var number) => number,
            JsonValueKind.String when long.TryParse(value.GetString(), out var parsed) => parsed,
            _ => 0,
        };
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0) return "0 B";
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var exponent = Math.Min((int)Math.Floor(Math.Log(bytes, 1024)), units.Length - 1);
        var value = bytes / Math.Pow(1024, exponent);
        return exponent == 0 ? $"{value:F0} {units[exponent]}" : $"{value:F2} {units[exponent]}";
    }

    private static JsonElement EmptyObject()
    {
        using var doc = JsonDocument.Parse("{}");
        return doc.RootElement.Clone();
    }
}
