using System.Text.Json;
using ZnodePublishUtility.Utilities.Exceptions;

namespace ZnodePublishUtility.API.Infrastructure.Elasticsearch;

/// <summary>
/// HTTP client implementation for Elasticsearch cluster APIs.
/// </summary>
public class ElasticsearchClient : IElasticsearchClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ElasticsearchClient> _logger;

    public ElasticsearchClient(IHttpClientFactory factory, ILogger<ElasticsearchClient> logger)
    {
        _httpClient = factory.CreateClient("Elasticsearch");
        _logger = logger;
    }

    public async Task<JsonDocument> GetClusterHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/_cluster/health", cancellationToken);
            response.EnsureSuccessStatusCode();
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to retrieve cluster health from Elasticsearch");
            throw new ElasticsearchUnavailableException("Cluster health API unavailable", ex);
        }
    }

    public async Task<JsonDocument> GetNodesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/_nodes?format=json", cancellationToken);
            response.EnsureSuccessStatusCode();
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to retrieve nodes from Elasticsearch");
            throw new ElasticsearchUnavailableException("Nodes API unavailable", ex);
        }
    }

    public async Task<JsonDocument> GetShardsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/_cat/shards?format=json", cancellationToken);
            response.EnsureSuccessStatusCode();
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to retrieve shards from Elasticsearch");
            throw new ElasticsearchUnavailableException("Shards API unavailable", ex);
        }
    }

    public async Task<JsonDocument> GetClusterSettingsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/_cluster/settings", cancellationToken);
            response.EnsureSuccessStatusCode();
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to retrieve cluster settings from Elasticsearch");
            throw new ElasticsearchUnavailableException("Cluster settings API unavailable", ex);
        }
    }

    public async Task<JsonDocument> GetIndexMappingAsync(string indexName, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(indexName))
                throw new ArgumentException("Index name cannot be empty", nameof(indexName));

            var response = await _httpClient.GetAsync($"/{Uri.EscapeDataString(indexName)}/_mapping", cancellationToken);
            response.EnsureSuccessStatusCode();
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to retrieve mapping for index {IndexName}", indexName);
            throw new ElasticsearchUnavailableException($"Index mapping API unavailable for {indexName}", ex);
        }
    }

    public async Task<JsonDocument> GetIndexStatsAsync(string indexName, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(indexName))
                throw new ArgumentException("Index name cannot be empty", nameof(indexName));

            var response = await _httpClient.GetAsync(
                $"/_cat/indices/{Uri.EscapeDataString(indexName)}?format=json&bytes=b", cancellationToken);
            response.EnsureSuccessStatusCode();
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to retrieve stats for index {IndexName}", indexName);
            throw new ElasticsearchUnavailableException($"Index stats API unavailable for {indexName}", ex);
        }
    }
}
