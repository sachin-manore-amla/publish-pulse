namespace ZnodeSphere.Models
{
    public class RedisOperationRequest
    {
        public string Operation { get; set; }
        public string InstanceName { get; set; }
        public string Environment { get; set; }
    }

    public class KubeOperationResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
    }

    public class ElasticIndexModel
    {
        public string IndexName { get; set; }
        public string Status { get; set; }
        public string Health { get; set; }
        public int DocumentCount { get; set; }
        public int ShardCount { get; set; }
        public int ReplicaCount { get; set; }
        public string IndexSize { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class ElasticClusterHealthModel
    {
        public string Status { get; set; }
        public string ClusterName { get; set; }
        public int ActiveNodes { get; set; }
        public int TotalNodes { get; set; }
        public int ActiveShards { get; set; }
        public int UnassignedShards { get; set; }
    }

    public class ElasticNodeStatsModel
    {
        public bool Available { get; set; }
        public int NodeCount { get; set; }
        public long TotalHeapMaxMb { get; set; }
        public long TotalHeapUsedMb { get; set; }
        public int HeapUsagePercent { get; set; }
        public long TotalRamMb { get; set; }
        public long TotalRamUsedMb { get; set; }
        public long TotalDiskGb { get; set; }
        public long AvailableDiskGb { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class ElasticQueryResultModel
    {
        public string Status { get; set; }
        public string ResponseTime { get; set; }
        public string Results { get; set; }
    }

    public class PortForwardRequest
    {
        public int LocalPort { get; set; }
        public int PodPort { get; set; }
        public string PodName { get; set; }
        public string Namespace { get; set; }
        public string Environment { get; set; }
    }

    public class PortForwardResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public string ForwardingUrl { get; set; }
        public int ProcessId { get; set; }
    }

    public class DatabaseBackupResponse
    {
        public string Status { get; set; }
        public int ProgressPercentage { get; set; }
        public string Message { get; set; }
    }

    public class DatabaseBackupRequest
    {
        public string DatabaseName { get; set; }
        public string Environment { get; set; }
    }

    public class RedisKeyModel
    {
        public string Key { get; set; }
        public string Type { get; set; }
        public string Content { get; set; }
        public bool IsJson { get; set; }
    }

    public class KubeEventModel
    {
        public string Namespace { get; set; }
        public string Type { get; set; }
        public string Reason { get; set; }
        public string Message { get; set; }
        public int Count { get; set; }
        public string Object { get; set; }
        public string Source { get; set; }
        public string LastSeen { get; set; }
    }

    public class KubeDeploymentModel
    {
        public string Name { get; set; }
        public string Namespace { get; set; }
        public int AgeDays { get; set; }
        public int Desired { get; set; }
        public string Strategy { get; set; }
        public string Image { get; set; }
        public int Ready { get; set; }
        public int Available { get; set; }
        public int UpToDate { get; set; }
        public string Status { get; set; }
    }

    public class KubeJobModel
    {
        public string Name { get; set; }
        public string Namespace { get; set; }
        public int AgeDays { get; set; }
        public int Active { get; set; }
        public int Succeeded { get; set; }
        public int Failed { get; set; }
        public string CompletionTime { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public string Schedule { get; set; }
        public bool Suspended { get; set; }
        public string LastSchedule { get; set; }
    }

    public class KubeContainerResourceModel
    {
        public string PodName { get; set; }
        public string Namespace { get; set; }
        public string ContainerName { get; set; }
        public string CpuRequest { get; set; }
        public string MemoryRequest { get; set; }
        public string CpuLimit { get; set; }
        public string MemoryLimit { get; set; }
        public string CpuActual { get; set; }
        public string MemoryActual { get; set; }
    }

    public class KubeHpaModel
    {
        public string Name { get; set; }
        public string Namespace { get; set; }
        public int AgeDays { get; set; }
        public int MinReplicas { get; set; }
        public int MaxReplicas { get; set; }
        public string Target { get; set; }
        public string Metrics { get; set; }
        public int CurrentReplicas { get; set; }
        public int DesiredReplicas { get; set; }
    }

    public class KubeContainerDetailModel
    {
        public string PodName { get; set; }
        public string ContainerName { get; set; }
        public string Image { get; set; }
        public bool Ready { get; set; }
        public int RestartCount { get; set; }
        public string State { get; set; }
        public string StateReason { get; set; }
        public string CpuRequest { get; set; }
        public string MemoryRequest { get; set; }
        public string CpuLimit { get; set; }
        public string MemoryLimit { get; set; }
        public bool LivenessProbe { get; set; }
        public bool ReadinessProbe { get; set; }
        public bool StartupProbe { get; set; }
    }

    public class KubeWorkloadModel
    {
        public string WorkloadType { get; set; }
        public string Name { get; set; }
        public string Namespace { get; set; }
        public int AgeDays { get; set; }
        public int Desired { get; set; }
        public string Image { get; set; }
        public int Ready { get; set; }
        public int Available { get; set; }
        public int Current { get; set; }
        public int Updated { get; set; }
        public string Status { get; set; }
    }

    public class KubePvcModel
    {
        public string Name { get; set; }
        public string Namespace { get; set; }
        public int AgeDays { get; set; }
        public string StorageClass { get; set; }
        public string VolumeName { get; set; }
        public string AccessModes { get; set; }
        public string Status { get; set; }
        public string Capacity { get; set; }
    }

    public class KubeRolloutRevisionModel
    {
        public string DeploymentName { get; set; }
        public int Revision { get; set; }
        public string ChangeReason { get; set; }
        public string Image { get; set; }
    }

    public class KubeConfigMapModel
    {
        public string Name { get; set; }
        public string Namespace { get; set; }
        public int KeyCount { get; set; }
        public int AgeDays { get; set; }
        public Dictionary<string, string> Data { get; set; }
    }
}

namespace ZnodeSphere.Utilities
{
    // Placeholder namespace — utilities live in the ZnodeSphere main library
}
