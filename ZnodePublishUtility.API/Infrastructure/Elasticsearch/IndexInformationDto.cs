using System.Text.Json;

namespace ZnodePublishUtility.API.Infrastructure.Elasticsearch;

/// <summary>
/// Document count, store size and mapping for a single Elasticsearch index, aggregated from
/// <c>_cat/indices/{index}</c> and <c>{index}/_mapping</c>.
/// </summary>
public class IndexInformationDto
{
    public string IndexName { get; set; } = "";
    public string Health { get; set; } = "";
    public string Status { get; set; } = "";
    public long DocumentCount { get; set; }
    public long IndexSizeBytes { get; set; }
    public string IndexSize { get; set; } = "";
    public JsonElement Mapping { get; set; }
}
