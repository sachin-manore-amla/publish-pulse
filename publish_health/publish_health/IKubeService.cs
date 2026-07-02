using publish_health;
using ZnodeSphere.Models;

namespace ZnodeSphere.Interfaces
{
    public interface IKubeService
    {
        Task<bool> InitializeAsync();
        Task<List<KubeEnvironmentModel>> GetEnvironmentsAsync();
        Task<List<KubePodModel>> GetPodsAsync(string environmentName, bool excludeNonAppServices = false);
        Task<List<KubePodMetricsModel>> GetPodMetricsAsync(string environmentName);
        Task<string> GetPodLogsAsync(string environmentName, string podName, int sinceMinutes = 15);
        Task<List<KubeNodeModel>> GetNodesAsync(string environmentName);
        Task<List<KubeEventModel>> GetEventsAsync(string environmentName, string eventType = "all");
        Task<List<KubeDeploymentModel>> GetDeploymentsAsync(string environmentName);
        Task<List<KubeJobModel>> GetJobsAsync(string environmentName);
        Task<List<KubeJobModel>> GetCronJobsAsync(string environmentName);
        Task<List<KubeContainerResourceModel>> GetResourceLimitsAsync(string environmentName);
        Task<List<KubeHpaModel>> GetHpasAsync(string environmentName);
        Task<List<KubeContainerDetailModel>> GetContainerDetailsAsync(string environmentName, string podName);
        Task<List<KubeWorkloadModel>> GetStatefulSetsAsync(string environmentName);
        Task<List<KubeWorkloadModel>> GetDaemonSetsAsync(string environmentName);
        Task<List<KubePvcModel>> GetPvcsAsync(string environmentName);
        Task<List<KubeRolloutRevisionModel>> GetRolloutHistoryAsync(string environmentName, string deploymentName = "");
        Task<List<KubeConfigMapModel>> GetConfigMapsAsync(string environmentName);
        Task<ElasticClusterHealthModel> GetElasticClusterHealthAsync(string environmentName);
        Task<List<ElasticIndexModel>> GetElasticIndicesAsync(string environmentName);
        Task<ElasticNodeStatsModel> GetElasticNodeStatsAsync(string environmentName);
        Task<ElasticQueryResultModel> ExecuteElasticQueryAsync(string environmentName, string indexName, string query, string httpMethod = "POST");
        Task<DatabaseConnectionResponse> GetDatabaseConnectionStringAsync(string environmentName, string configKey);
        Task<HostedEnvironmentConfigResponse> GetHostedEnvironmentConfigAsync(string environmentName, bool isFrontendOnly = false);
        Task<KubeOperationResponse> HandleRedisOperationAsync(RedisOperationRequest request);
        Task<RedisKeyModel> GetRedisKeyContentAsync(string environmentName, string instanceName, string key);
        Task<DatabaseBackupResponse> StartDatabaseBackupAsync(DatabaseBackupRequest request);
        Task<string> ExecuteKubectlCommandAsync(string environmentName, string deploymentName, string commandString, string namespaceName = "znode");
        Task<PortForwardResponse> StartPortForwardAsync(PortForwardRequest request);
        Task<KubeOperationResponse> StopPortForwardAsync(int processId);
        Task LogActivityAsync(string action, Dictionary<string, string> details);
    }
}
