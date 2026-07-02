using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ZnodeSphere.Interfaces;
using ZnodeSphere.Models;
using ZnodeSphere.Utilities;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using k8s;
using k8s.Models;
using publish_health;

namespace ZnodeSphere.Services
{
    /// <summary>
    /// Comprehensive Kubernetes and infrastructure management service.
    /// Implements the same functionality as Python's kube_manager.py with Fernet decryption support.
    /// </summary>
    public class KubeService : IKubeService
    {
        private readonly ILogger<KubeService> _logger;
        private readonly string _kubeConfigDir;
        private readonly string _kubectlExecutable;
        private readonly bool _bundledKubectlAvailable;
        private bool _isInitialized;

        private static readonly string[] HostedAppSettingKeys = new[]
        {
            "appsettings__AdminWebsiteUrl",
            "appsettings__CustomAPIRootUri",
            "appsettings__PluginApiRootUri",
            "appsettings__ZnodeApiDomainKey",
            "appsettings__ZnodeApiDomainName",
            "appsettings__ZnodeApiGateway",
            "appsettings__ZnodeApiRootUri",
            "appsettings__ZnodeApiV2RootUri",
            "appsettings__ZnodeCommercePortalRootUri",
            "ConnectionStrings__HangfireConnection",
            "ConnectionStrings__Znode_Entities",
            "ConnectionStrings__ZnodeECommerceDB",
            "ConnectionStrings__ZnodePublish_Entities",
            "ConnectionStrings__ZnodeCustomTableEntities",
            "ConnectionStrings__ZnodeCustomStoreProcedureDB",
            "ConnectionStrings__Znode_Entities_CustomSP",
            "ConnectionStrings__ZnodeFramework_Entities",
            "ConnectionStrings__ZnodeKlaviyo_Entities",
            "ConnectionStrings__ZnodeMongoDBForLog",
            "API_DOMAIN",
            "API_V2_DOMAIN",
            "API_KEY"
        };

        private static readonly string[] HostedAppSettingConfigMaps = new[]
        {
            "znode10xadmin-configmap",
            "znode10xapi-configmap",
        };

        private static readonly string[] HostedAppSettingNamespaces = new[]
        {
            "znode",
            "znode-admin",
            "default"
        };

        public KubeService(ILogger<KubeService> logger)
        {
            _logger = logger;

            // Setup paths - read directly from KubeConfigs folder, no decryption needed
            string baseDir = AppContext.BaseDirectory;
            _kubeConfigDir = Path.Combine(baseDir, "KubeConfigs");
            string bundledKubectlPath = Path.Combine(baseDir, "SharedExecutables", "kubectl.exe");
            if (File.Exists(bundledKubectlPath))
            {
                _kubectlExecutable = bundledKubectlPath;
                _bundledKubectlAvailable = true;
            }
            else
            {
                _kubectlExecutable = "kubectl";
                _bundledKubectlAvailable = false;
            }

            _isInitialized = false;
        }

        /// <summary>
        /// Initializes the service by verifying KubeConfigs folder exists.
        /// No decryption needed - files are read directly.
        /// </summary>
        public async Task<bool> InitializeAsync()
        {
            // If already initialized and folder exists, return true
            if (_isInitialized && Directory.Exists(_kubeConfigDir))
            {
                return true;
            }

            return await Task.Run(() => {
                try
                {
                    // Verify KubeConfigs directory exists
                    if (!Directory.Exists(_kubeConfigDir))
                    {
                        _logger.LogError($"❌ KubeConfigs directory not found at {_kubeConfigDir}");
                        return false;
                    }

                    _logger.LogInformation($"✅ KubeConfigs directory found at {_kubeConfigDir}");
                    var entries = Directory.GetFileSystemEntries(_kubeConfigDir);
                    _logger.LogInformation($"✅ Found {entries.Length} kubeconfig files/directories");

                    if (_bundledKubectlAvailable)
                    {
                        _logger.LogInformation($"✅ Using bundled kubectl at {_kubectlExecutable}");
                    }
                    else
                    {
                        _logger.LogInformation("ℹ️ Using kubectl from system PATH");
                    }

                    _isInitialized = true;
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Init failed: {ex.Message}");
                    return false;
                }
            });
        }



        /// <summary>
        /// Gets all available Kubernetes environments from KubeConfigs directory.
        /// </summary>
        public async Task<List<KubeEnvironmentModel>> GetEnvironmentsAsync()
        {
            return await Task.Run(() =>
            {
                var environments = new List<KubeEnvironmentModel>();

                try
                {
                    // Auto-initialize if not already done
                    if (!_isInitialized)
                    {
                        _logger.LogInformation("Auto-initializing KubeService for environment lookup...");
                        bool initSuccess = InitializeAsync().GetAwaiter().GetResult();
                        if (!initSuccess)
                        {
                            _logger.LogError("Auto-initialization failed");
                            return environments;
                        }
                    }

                    _logger.LogInformation($"Looking for kube configs in: {_kubeConfigDir}");
                    _logger.LogInformation($"Directory exists: {Directory.Exists(_kubeConfigDir)}");

                    if (!Directory.Exists(_kubeConfigDir))
                    {
                        _logger.LogWarning($"❌ Kube config directory not found: {_kubeConfigDir}");
                        return environments;
                    }

                    var entries = Directory.GetFileSystemEntries(_kubeConfigDir);
                    _logger.LogInformation($"Found {entries.Length} entries in kube config directory");

                    foreach (var item in entries)
                    {
                        _logger.LogInformation($"  - {Path.GetFileName(item)}");
                        string name = Path.GetFileName(item);
                        if (name.StartsWith(".") || name.StartsWith("__"))
                        {
                            _logger.LogInformation($"    Skipping (starts with . or __)");
                            continue;
                        }

                        environments.Add(new KubeEnvironmentModel
                        {
                            Name = name,
                            DisplayName = name.ToUpper().Replace("_", " "),
                            IsAvailable = true
                        });
                    }

                    _logger.LogInformation($"✅ Found {environments.Count} Kubernetes environments");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"❌ Error fetching environments: {ex.Message}\n{ex.StackTrace}");
                }

                return environments.OrderBy(e => e.Name).ToList();
            });
        }

        /// <summary>
        /// Gets all pods in a specific Kubernetes environment.
        /// Requires kubectl and valid kubeconfig.
        /// </summary>
        public async Task<List<KubePodModel>> GetPodsAsync(string environmentName, bool excludeNonAppServices = false)
        {
            return await Task.Run(() =>
            {
                var pods = new List<KubePodModel>();

                try
                {
                    string kubeConfigPath = ResolveKubeConfigFile(environmentName);
                    if (string.IsNullOrEmpty(kubeConfigPath))
                    {
                        _logger.LogWarning($"Kubeconfig file not resolved for environment: {environmentName}");
                        return pods;
                    }

                    // Use kubectl to list pods in JSON format
                    string output = ExecuteKubectl($"get pods --kubeconfig=\"{kubeConfigPath}\" -n znode -o json");

                    if (string.IsNullOrEmpty(output))
                    {
                        _logger.LogWarning($"No output from kubectl for environment: {environmentName}");
                        return pods;
                    }

                    // Parse JSON output
                    try
                    {
                        var options = new JsonSerializerOptions 
                        { 
                            PropertyNameCaseInsensitive = true,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        };

                        using (JsonDocument doc = JsonDocument.Parse(output))
                        {
                            var root = doc.RootElement;

                            // Check if we have items array
                            if (root.TryGetProperty("items", out JsonElement itemsElement) && itemsElement.ValueKind == JsonValueKind.Array)
                            {
                                foreach (JsonElement item in itemsElement.EnumerateArray())
                                {
                                    try
                                    {
                                        var pod = ParsePodFromJson(item, environmentName);
                                        
                                        // Filter non-app services if requested
                                        if (excludeNonAppServices && IsNonAppService(pod.Name))
                                        {
                                            continue;
                                        }

                                        pods.Add(pod);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning($"Failed to parse pod item: {ex.Message}");
                                        continue;
                                    }
                                }
                            }
                        }

                        _logger.LogInformation($"✅ Successfully retrieved {pods.Count} pods from environment: {environmentName}");
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError($"❌ Failed to parse kubectl JSON output: {ex.Message}");
                        _logger.LogDebug($"Raw output: {output}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"❌ Error fetching pods: {ex.Message}");
                }

                return pods;
            });
        }

        /// <summary>
        /// Parses a single pod from Kubernetes JSON output.
        /// </summary>
        private KubePodModel ParsePodFromJson(JsonElement podElement, string environmentName)
        {
            var pod = new KubePodModel
            {
                Name = "Unknown",
                Status = "Unknown",
                IpAddress = "N/A",
                RestartCount = 0,
                AgeDays = 0,
                ApiGroup = environmentName,
                ContainerPort = 8080  // Default port, will be overridden
            };

            try
            {
                // Extract metadata
                if (podElement.TryGetProperty("metadata", out JsonElement metadata))
                {
                    if (metadata.TryGetProperty("name", out JsonElement nameElem))
                    {
                        pod.Name = nameElem.GetString() ?? "Unknown";
                    }

                    if (metadata.TryGetProperty("creationTimestamp", out JsonElement createdElem))
                    {
                        var createdStr = createdElem.GetString();
                        if (DateTime.TryParse(createdStr, out DateTime created))
                        {
                            pod.AgeDays = (int)(DateTime.UtcNow - created).TotalDays;
                        }
                    }
                }

                // Extract container port, total containers, and nodeName from spec
                if (podElement.TryGetProperty("spec", out JsonElement spec))
                {
                    if (spec.TryGetProperty("nodeName", out JsonElement nodeNameElem))
                        pod.NodeName = nodeNameElem.GetString() ?? string.Empty;

                    if (spec.TryGetProperty("containers", out JsonElement containers) && containers.ValueKind == JsonValueKind.Array)
                    {
                        pod.TotalContainers = containers.GetArrayLength();
                        foreach (JsonElement container in containers.EnumerateArray())
                        {
                            if (container.TryGetProperty("ports", out JsonElement ports) && ports.ValueKind == JsonValueKind.Array)
                            {
                                foreach (JsonElement port in ports.EnumerateArray())
                                {
                                    if (port.TryGetProperty("containerPort", out JsonElement portNum))
                                    {
                                        pod.ContainerPort = portNum.GetInt32();
                                        break;
                                    }
                                }
                                if (pod.ContainerPort != 8080) break;
                            }
                        }
                    }
                }

                // Extract status
                if (podElement.TryGetProperty("status", out JsonElement statusObj))
                {
                    if (statusObj.TryGetProperty("phase", out JsonElement phaseElem))
                        pod.Status = phaseElem.GetString() ?? "Unknown";

                    if (statusObj.TryGetProperty("podIP", out JsonElement ipElem))
                        pod.IpAddress = ipElem.GetString() ?? "N/A";

                    if (statusObj.TryGetProperty("containerStatuses", out JsonElement containerStatusesElem)
                        && containerStatusesElem.ValueKind == JsonValueKind.Array)
                    {
                        int totalRestarts = 0;
                        int readyCount = 0;
                        foreach (JsonElement cs in containerStatusesElem.EnumerateArray())
                        {
                            if (cs.TryGetProperty("restartCount", out JsonElement restartElem))
                                totalRestarts += restartElem.GetInt32();

                            bool isReady = false;
                            if (cs.TryGetProperty("ready", out JsonElement readyElem))
                                isReady = readyElem.GetBoolean();
                            if (isReady) readyCount++;

                            // Extract reason from waiting or terminated state
                            if (string.IsNullOrEmpty(pod.ContainerReason))
                            {
                                if (cs.TryGetProperty("state", out JsonElement stateEl))
                                {
                                    if (stateEl.TryGetProperty("waiting", out JsonElement waiting)
                                        && waiting.TryGetProperty("reason", out JsonElement wr))
                                        pod.ContainerReason = wr.GetString() ?? string.Empty;
                                    else if (stateEl.TryGetProperty("terminated", out JsonElement terminated)
                                        && terminated.TryGetProperty("reason", out JsonElement tr))
                                        pod.ContainerReason = tr.GetString() ?? string.Empty;
                                }
                                // Fallback: lastState terminated reason (e.g. OOMKilled from previous run)
                                if (string.IsNullOrEmpty(pod.ContainerReason) && cs.TryGetProperty("lastState", out JsonElement lastState))
                                {
                                    if (lastState.TryGetProperty("terminated", out JsonElement lastTerm)
                                        && lastTerm.TryGetProperty("reason", out JsonElement ltr))
                                        pod.ContainerReason = ltr.GetString() ?? string.Empty;
                                }
                            }

                            if (!isReady && pod.Status == "Running")
                                pod.Status = "Degraded";
                        }
                        pod.RestartCount = totalRestarts;
                        pod.ReadyContainers = readyCount;
                        if (pod.TotalContainers == 0)
                            pod.TotalContainers = containerStatusesElem.GetArrayLength();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error parsing pod details: {ex.Message}");
            }

            return pod;
        }

        /// <summary>
        /// Determines if a pod is a non-app service (system pods, etc.)
        /// </summary>
        private bool IsNonAppService(string podName)
        {
            // List of prefixes that indicate system/non-app services
            var systemPrefixes = new[] 
            { 
                "kube-", 
                "coredns", 
                "etcd-", 
                "kube-apiserver",
                "kube-controller",
                "kube-scheduler",
                "kube-proxy",
                "calico",
                "nginx-ingress",
                "prometheus",
                "grafana"
            };

            return systemPrefixes.Any(prefix => podName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets pod logs for a specific environment and pod name.
        /// </summary>
        public async Task<string> GetPodLogsAsync(string environmentName, string podName, int sinceMinutes = 15)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string kubeConfigPath = ResolveKubeConfigFile(environmentName);
                    if (string.IsNullOrEmpty(kubeConfigPath))
                    {
                        return $"❌ Error: Kube config file not resolved for {environmentName}.";
                    }

                    string output = ExecuteKubectl($"logs {podName} --kubeconfig=\"{kubeConfigPath}\" -n znode --since={sinceMinutes}m --tail=200 --timestamps");

                    _logger.LogInformation($"✅ Log fetch success for {podName}");
                    return output;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"❌ Error fetching pod logs: {ex.Message}");
                    return $"❌ Error retrieving logs: {ex.Message}";
                }
            });
        }

        /// <summary>
        /// Gets database connection string from Kubernetes ConfigMap or Secret.
        /// Updated to handle both LOCAL_VM and hosted/VM environments.
        /// For LOCAL_VM: Returns empty/default connection string
        /// For hosted/VM: Fetches from kubeconfig ConfigMap/Secret
        /// </summary>
        public async Task<DatabaseConnectionResponse> GetDatabaseConnectionStringAsync(string environmentName, string configKey)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Check if LOCAL_VM - return empty for local development
                    if (environmentName.Equals("LOCAL_VM", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation($"Environment is LOCAL_VM - returning local connection string");
                        return new DatabaseConnectionResponse
                        {
                            Status = "success",
                            ConnectionString = "Server=(local);Database=z10_dev_db;Integrated Security=true;",
                            Message = "Local connection string (LOCAL_VM)",
                            IsLocal = true
                        };
                    }

                    // For VM/Hosted environments: Fetch from kubeconfig
                    string kubeConfigPath = ResolveKubeConfigFile(environmentName);
                    if (string.IsNullOrEmpty(kubeConfigPath))
                    {
                        return new DatabaseConnectionResponse
                        {
                            Status = "error",
                            Message = $"Kube config file not resolved for environment: {environmentName}"
                        };
                    }

                    _logger.LogInformation($"Fetching connection string from kubeconfig for environment: {environmentName}");

                    var kubeConfig = KubernetesClientConfiguration.BuildConfigFromConfigFile(kubeConfigPath);
                    using var client = new Kubernetes(kubeConfig);

                    string connectionString = TryReadConfigMapValue(client, configKey);

                    if (string.IsNullOrWhiteSpace(connectionString))
                    {
                        _logger.LogWarning($"ConfigMap '{configKey}' not found, trying Kubernetes Secret");
                        connectionString = TryReadSecretValue(client, configKey);
                    }

                    if (string.IsNullOrWhiteSpace(connectionString))
                    {
                        _logger.LogError($"Failed to find connection string for key '{configKey}' in environment '{environmentName}'");
                        return new DatabaseConnectionResponse
                        {
                            Status = "error",
                            Message = $"Connection string key '{configKey}' not found in kubeconfig for {environmentName}",
                            IsLocal = false
                        };
                    }

                    _logger.LogInformation($"✅ Successfully fetched connection string from {environmentName}");
                    return new DatabaseConnectionResponse
                    {
                        Status = "success",
                        ConnectionString = connectionString,
                        Message = "Connection string retrieved from Kubernetes",
                        IsLocal = false
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError($"❌ Error fetching connection string: {ex.Message}");
                    return new DatabaseConnectionResponse
                    {
                        Status = "error",
                        Message = $"Error: {ex.Message}",
                        IsLocal = false
                    };
                }
            });
        }

        /// <summary>
        /// Gets hosted environment configuration (database connection string or gateway URL).
        /// For database deployments: Returns connection string from kubeconfig
        /// For frontend-only deployments: Returns gateway/API URL from kubeconfig
        /// </summary>
        public async Task<HostedEnvironmentConfigResponse> GetHostedEnvironmentConfigAsync(string environmentName, bool isFrontendOnly = false)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    // Validate it's not LOCAL_VM for hosted operations
                    if (environmentName.Equals("LOCAL_VM", StringComparison.OrdinalIgnoreCase))
                    {
                        return new HostedEnvironmentConfigResponse
                        {
                            Status = "error",
                            Message = "Hosted configuration not applicable for LOCAL_VM"
                        };
                    }

                    string kubeConfigPath = ResolveKubeConfigFile(environmentName);
                    if (string.IsNullOrEmpty(kubeConfigPath))
                    {
                        return new HostedEnvironmentConfigResponse
                        {
                            Status = "error",
                            Message = $"Kube config file not resolved for environment: {environmentName}"
                        };
                    }

                    _logger.LogInformation($"Fetching hosted environment config for '{environmentName}' (FrontendOnly: {isFrontendOnly})");

                    if (isFrontendOnly)
                    {
                        // For frontend-only apps: Get gateway/API URL from ConfigMap
                        var kubeConfig = KubernetesClientConfiguration.BuildConfigFromConfigFile(kubeConfigPath);
                        using var client = new Kubernetes(kubeConfig);

                        var gatewayKeyCandidates = new[]
                        {
                            "appsettings__AdminWebsiteUrl"
                        };

                        var configMapCandidates = HostedAppSettingConfigMaps;
                        var namespaceCandidates = HostedAppSettingNamespaces;

                        string gatewayUrl = TryReadConfigMapValueFromSources(client, gatewayKeyCandidates, configMapCandidates, namespaceCandidates);
                        if (!string.IsNullOrWhiteSpace(gatewayUrl) && !gatewayUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        {
                            gatewayUrl = $"https://{gatewayUrl.TrimStart('/')}";
                        }

                        // Replace 'admin' with 'gateways' in the gateway URL (e.g., https://admin-z10-dev1.znodecorp.com -> https://gateways-z10-dev1.znodecorp.com)
                        if (!string.IsNullOrWhiteSpace(gatewayUrl) && gatewayUrl.Contains("admin-", StringComparison.OrdinalIgnoreCase))
                        {
                            gatewayUrl = Regex.Replace(gatewayUrl, @"admin-", "apigateways-", RegexOptions.IgnoreCase);
                            _logger.LogInformation($"[GATEWAY URL] Replaced 'admin' with 'gateways' in gateway URL: {gatewayUrl}");
                        }

                        gatewayUrl = gatewayUrl?.TrimEnd('/');
                        var hostedAppSettings = TryReadHostedAppSettings(client, configMapCandidates, namespaceCandidates);
                        var (domainName, domainKey) = TryReadDomainSettings(client);

                        if (string.IsNullOrWhiteSpace(gatewayUrl))
                        {
                            _logger.LogWarning("Gateway URL missing from known ConfigMaps. Falling back to service ingress lookup.");
                            string serviceHost = TryReadServiceHostname(client, "znode-api");
                            if (!string.IsNullOrWhiteSpace(serviceHost))
                            {
                                gatewayUrl = serviceHost.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                                    ? serviceHost
                                    : $"https://{serviceHost}";
                                gatewayUrl = gatewayUrl.TrimEnd('/');
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(gatewayUrl))
                        {
                            _logger.LogInformation($"✅ Retrieved gateway URL for {environmentName}: {gatewayUrl}");
                            return new HostedEnvironmentConfigResponse
                            {
                                Status = "success",
                                ZnodeApiGateway = gatewayUrl,
                                Message = "Gateway URL retrieved from Kubernetes",
                                IsFrontendOnly = true,
                                ApiDomainName = string.IsNullOrWhiteSpace(domainName) ? null : domainName,
                                ApiDomainKey = string.IsNullOrWhiteSpace(domainKey) ? null : domainKey,
                                HostedAppSettings = hostedAppSettings.Count == 0 ? null : hostedAppSettings
                            };
                        }

                        _logger.LogWarning($"Gateway URL not found for {environmentName}");
                        return new HostedEnvironmentConfigResponse
                        {
                            Status = "error",
                            Message = "Gateway/API URL not found in Kubernetes resources"
                        };
                    }
                    else
                    {
                        // For full deployments: Get database connection string
                        var dbResponse = await GetDatabaseConnectionStringAsync(environmentName, "ConnectionStrings__ZnodeECommerceDB");

                        string domainName = null;
                        string domainKey = null;
                        Dictionary<string, string> hostedAppSettings = null;

                        try
                        {
                            var kubeConfig = KubernetesClientConfiguration.BuildConfigFromConfigFile(kubeConfigPath);
                            using var client = new Kubernetes(kubeConfig);
                            (domainName, domainKey) = TryReadDomainSettings(client);
                            hostedAppSettings = TryReadHostedAppSettings(client, HostedAppSettingConfigMaps, HostedAppSettingNamespaces);
                        }
                        catch (Exception domainEx)
                        {
                            _logger.LogWarning($"Failed to read hosted domain settings: {domainEx.Message}");
                        }

                        return new HostedEnvironmentConfigResponse
                        {
                            Status = dbResponse.Status,
                            ConnectionString = dbResponse.ConnectionString,
                            Message = dbResponse.Message,
                            IsFrontendOnly = false,
                            ApiDomainName = string.IsNullOrWhiteSpace(domainName) ? null : domainName,
                            ApiDomainKey = string.IsNullOrWhiteSpace(domainKey) ? null : domainKey,
                            HostedAppSettings = hostedAppSettings != null && hostedAppSettings.Count > 0 ? hostedAppSettings : null
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"❌ Error fetching hosted config: {ex.Message}");
                    return new HostedEnvironmentConfigResponse
                    {
                        Status = "error",
                        Message = $"Error: {ex.Message}"
                    };
                }
            });
        }

        /// <summary>
        /// Returns CPU and memory metrics per pod from kubectl top pods.
        /// </summary>
        public async Task<List<KubePodMetricsModel>> GetPodMetricsAsync(string environmentName)
        {
            return await Task.Run(() =>
            {
                var metrics = new List<KubePodMetricsModel>();
                try
                {
                    string kubeConfigPath = ResolveKubeConfigFile(environmentName);
                    if (string.IsNullOrEmpty(kubeConfigPath)) return metrics;

                    string output = ExecuteKubectl($"top pods --kubeconfig=\"{kubeConfigPath}\" -n znode --no-headers");
                    if (string.IsNullOrEmpty(output)) return metrics;

                    foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var parts = line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 3)
                        {
                            metrics.Add(new KubePodMetricsModel
                            {
                                PodName = parts[0],
                                Cpu = parts[1],
                                Memory = parts[2],
                                CpuCores = ParseCpuMillicores(parts[1]),
                                MemoryMi = ParseMemoryMi(parts[2])
                            });
                        }
                    }
                }
                catch (Exception ex) { _logger.LogError($"Error fetching pod metrics: {ex.Message}"); }
                return metrics;
            });
        }

        private static double ParseCpuMillicores(string cpu)
        {
            if (string.IsNullOrEmpty(cpu)) return 0;
            if (cpu.EndsWith("m") && double.TryParse(cpu.TrimEnd('m'), out var m)) return m / 1000.0;
            if (double.TryParse(cpu, out var c)) return c;
            return 0;
        }

        private static long ParseMemoryMi(string mem)
        {
            if (string.IsNullOrEmpty(mem)) return 0;
            if (mem.EndsWith("Ki") && long.TryParse(mem.Replace("Ki", ""), out var ki)) return ki / 1024;
            if (mem.EndsWith("Mi") && long.TryParse(mem.Replace("Mi", ""), out var mi)) return mi;
            if (mem.EndsWith("Gi") && long.TryParse(mem.Replace("Gi", ""), out var gi)) return gi * 1024;
            if (long.TryParse(mem, out var b)) return b / (1024 * 1024);
            return 0;
        }

        /// <summary>
        /// Executes a kubectl command (e.g., for Redis operations).
        /// </summary>
        public async Task<string> ExecuteKubectlCommandAsync(string environmentName, string deploymentName, string commandString, string namespaceName = "znode")
        {
            return await Task.Run(() =>
            {
                try
                {
                    string kubeConfigPath = ResolveKubeConfigFile(environmentName);
                    if (string.IsNullOrEmpty(kubeConfigPath))
                    {
                        return $"❌ Kubeconfig not resolved for environment: {environmentName}";
                    }

                    // 1. Try to find the pod by label or by name filter
                    string podsJson = ExecuteKubectl($"get pods --kubeconfig=\"{kubeConfigPath}\" -n {namespaceName} -o json");
                    if (string.IsNullOrWhiteSpace(podsJson))
                    {
                        return $"❌ Unable to retrieve pods for deployment: {deploymentName}";
                    }

                    using (JsonDocument doc = JsonDocument.Parse(podsJson))
                    {
                        var pod = doc.RootElement.GetProperty("items").EnumerateArray()
                            .FirstOrDefault(p => p.GetProperty("metadata").GetProperty("name").GetString().Contains(deploymentName)
                                             && p.GetProperty("status").GetProperty("phase").GetString() == "Running");

                        if (pod.ValueKind == JsonValueKind.Undefined)
                        {
                            return $"❌ No running pod found for deployment: {deploymentName}";
                        }

                        string podName = pod.GetProperty("metadata").GetProperty("name").GetString();

                        // 2. Execute the Redis command
                        string cleanCommand = commandString.Replace("\"", "").Trim();
                        return ExecuteKubectl($"exec {podName} --kubeconfig=\"{kubeConfigPath}\" -n {namespaceName} -- {cleanCommand}");
                    }
                }
                catch (Exception ex)
                {
                    return $"❌ Error: {ex.Message}";
                }
            });
        }

        public async Task<KubeOperationResponse> HandleRedisOperationAsync(RedisOperationRequest request)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    string deploymentName = MapInstanceToDeployment(request.InstanceName);

                    if (request.Operation == "check_keys")
                    {
                        // Execute only the lightweight KEYS command to get names
                        string command = "redis-cli KEYS *";
                        string output = await ExecuteKubectlCommandAsync(request.Environment, deploymentName, command);

                        if (string.IsNullOrEmpty(output))
                        {
                            return new KubeOperationResponse { Status = "success", Message = "No keys found", Data = new string[] { } };
                        }

                        // Split output by newlines to create the key list
                        string[] keys = output.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                        return new KubeOperationResponse
                        {
                            Status = "success",
                            Message = $"{keys.Length} keys retrieved",
                            Data = keys
                        };
                    }
                    else if (request.Operation == "clear")
                    {
                        string command = "redis-cli FLUSHALL";
                        await ExecuteKubectlCommandAsync(request.Environment, deploymentName, command);

                        return new KubeOperationResponse { Status = "success", Message = "Redis cache cleared" };
                    }

                    return new KubeOperationResponse { Status = "error", Message = $"Unsupported operation: {request.Operation}" };
                }
                catch (Exception ex)
                {
                    _logger.LogError($"❌ Redis operation failed: {ex.Message}");
                    return new KubeOperationResponse { Status = "error", Message = ex.Message };
                }
            });
        }

        /// <summary>
        /// Gets content of a single Redis key.
        /// </summary>
        public async Task<RedisKeyModel> GetRedisKeyContentAsync(string environmentName, string instanceName, string key)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string deploymentName = MapInstanceToDeployment(instanceName);
                    string commandResult = ExecuteKubectlCommandAsync(environmentName, deploymentName, $"redis-cli GET {key}").Result;

                    bool isJson = (commandResult?.StartsWith("{") ?? false) || (commandResult?.StartsWith("[") ?? false);

                    return new RedisKeyModel
                    {
                        Key = key,
                        Type = "string",
                        Content = commandResult,
                        IsJson = isJson
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError($"❌ Error fetching Redis key: {ex.Message}");
                    return new RedisKeyModel
                    {
                        Key = key,
                        Type = "unknown",
                        Content = $"Error: {ex.Message}",
                        IsJson = false
                    };
                }
            });
        }

        /// <summary>
        /// Initiates a database backup operation (runs asynchronously).
        /// </summary>
        public async Task<DatabaseBackupResponse> StartDatabaseBackupAsync(DatabaseBackupRequest request)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    await LogActivityAsync("Database Backup Initiated", new Dictionary<string, string>
                    {
                        { "database", request.DatabaseName },
                        { "environment", request.Environment }
                    });

                    // Start background task
                    _ = Task.Run(() => PerformDatabaseBackup(request));

                    return new DatabaseBackupResponse
                    {
                        Status = "started",
                        ProgressPercentage = 0,
                        Message = $"Database backup started for {request.DatabaseName}"
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError($"❌ Error starting database backup: {ex.Message}");
                    return new DatabaseBackupResponse
                    {
                        Status = "error",
                        ProgressPercentage = 0,
                        Message = $"Error: {ex.Message}"
                    };
                }
            });
        }

        /// <summary>
        /// Performs the actual database backup operation.
        /// </summary>
        private async Task PerformDatabaseBackup(DatabaseBackupRequest request)
        {
            try
            {
                // Get database connection string from Kube
                var connResponse = await GetDatabaseConnectionStringAsync(request.Environment, "ConnectionStrings__ZnodeECommerceDB");

                if (connResponse.Status != "success")
                {
                    _logger.LogError($"❌ Failed to retrieve connection string for {request.Environment}");
                    return;
                }

                _logger.LogInformation($"Starting database backup for {request.DatabaseName}");
                // Implement sqlpackage-based backup here
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Database backup failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Logs activity to database for audit trail (fire and forget).
        /// </summary>
        public async Task LogActivityAsync(string action, Dictionary<string, string> details)
        {
            try
            {
                // Non-blocking activity logging
                await Task.Run(() =>
                {
                    try
                    {
                        // Log activity
                        _logger.LogInformation($"[Activity] {action}: {string.Join(", ", details.Select(kv => $"{kv.Key}={kv.Value}"))}");
                    }
                    catch
                    {
                        // Silently fail - don't break main flow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error logging activity: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper: Resolves the actual kubeconfig filename based on environment name.
        /// </summary>
        private string ResolveKubeConfigFile(string environmentName)
        {
            if (!Directory.Exists(_kubeConfigDir))
            {
                _logger.LogWarning($"Kube config directory does not exist: {_kubeConfigDir}");
                return null;
            }

            // 1. Check for exact match
            var items = Directory.GetFileSystemEntries(_kubeConfigDir);
            var exactMatch = items.FirstOrDefault(item => Path.GetFileName(item).Equals(environmentName, StringComparison.OrdinalIgnoreCase));
            if (exactMatch != null)
            {
                _logger.LogInformation($"✅ Found exact match for environment: {environmentName} -> {exactMatch}");
                return exactMatch;
            }

            // 2. Check for partial match
            var partialMatch = items.FirstOrDefault(item =>
                Path.GetFileName(item).Contains(environmentName, StringComparison.OrdinalIgnoreCase) ||
                environmentName.Contains(Path.GetFileName(item), StringComparison.OrdinalIgnoreCase)
            );
            if (partialMatch != null)
            {
                _logger.LogInformation($"✅ Found partial match for environment: {environmentName} -> {partialMatch}");
                return partialMatch;
            }

            _logger.LogWarning($"❌ No kubeconfig file resolved for environment: {environmentName}");
            return null;
        }

        /// <summary>
        /// Maps user-friendly Redis instance names to Kubernetes deployment prefixes.
        /// </summary>
        private string MapInstanceToDeployment(string instanceName)
        {
            return instanceName switch
            {
                "Redis Webstore" => "rediswebstore-node", // Updated to match node labels
                "Redis API" => "redisapi-node",           // Updated to match node labels
                _ => instanceName.Replace("Redis ", "").ToLower().Replace(" ", "")
            };
        }

        /// <summary>
        /// Fetches all Elasticsearch indices from the cluster in the specified environment.
        /// </summary>
        public async Task<List<ElasticIndexModel>> GetElasticIndicesAsync(string environmentName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string kubeConfigPath = ResolveKubeConfigFile(environmentName);
                    string elasticPod = GetElasticPodName(environmentName);

                    if (string.IsNullOrEmpty(elasticPod) || string.IsNullOrEmpty(kubeConfigPath))
                    {
                        _logger.LogWarning($"⚠️ Missing requirements for {environmentName}: Pod={elasticPod}, Config={kubeConfigPath}");
                        return new List<ElasticIndexModel>();
                    }

                    // REMOVED -it flags. ADDED --kubeconfig.
                    // Using 127.0.0.1 often works better than 'localhost' inside containers.
                    string result = ExecuteKubectl($"exec {elasticPod} --kubeconfig=\"{kubeConfigPath}\" -n znode -- curl -s http://127.0.0.1:9200/_cat/indices?format=json");

                    if (string.IsNullOrEmpty(result))
                    {
                        _logger.LogError("❌ Elasticsearch command returned empty output.");
                        return new List<ElasticIndexModel>();
                    }

                    // ... parse results as you were doing before
                    return ParseElasticIndices(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"❌ Error: {ex.Message}");
                    return new List<ElasticIndexModel>();
                }
            });
        }

        /// <summary>
        /// Parses raw JSON output from Elasticsearch _cat/indices into a list of models.
        /// </summary>
        /// <param name="rawJson">The raw JSON string from the kubectl exec command.</param>
        /// <returns>A list of structured ElasticIndexModel objects.</returns>
        private List<ElasticIndexModel> ParseElasticIndices(string rawJson)
        {
            var indices = new List<ElasticIndexModel>();

            if (string.IsNullOrEmpty(rawJson))
            {
                return indices;
            }

            try
            {
                // Elasticsearch _cat/indices?format=json returns an array of objects
                using (JsonDocument doc = JsonDocument.Parse(rawJson))
                {
                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var element in doc.RootElement.EnumerateArray())
                        {
                            try
                            {
                                indices.Add(new ElasticIndexModel
                                {
                                    // Map the shorthand keys from Elasticsearch JSON output
                                    IndexName = element.TryGetProperty("index", out var idx) ? idx.GetString() : "unknown",
                                    Status = element.TryGetProperty("status", out var st) ? st.GetString() : "unknown",
                                    Health = element.TryGetProperty("health", out var h) ? h.GetString() : "unknown",

                                    // Convert string numbers from JSON to integers
                                    DocumentCount = int.TryParse(element.TryGetProperty("docs.count", out var dc) ? dc.GetString() : "0", out int docs) ? docs : 0,
                                    ShardCount = int.TryParse(element.TryGetProperty("pri", out var pri) ? pri.GetString() : "0", out int p) ? p : 0,
                                    ReplicaCount = int.TryParse(element.TryGetProperty("rep", out var rep) ? rep.GetString() : "0", out int r) ? r : 0,

                                    IndexSize = element.TryGetProperty("store.size", out var sz) ? sz.GetString() : "0B",
                                    CreatedDate = DateTime.UtcNow
                                });
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning($"⚠️ Skipping individual index entry due to parse error: {ex.Message}");
                                continue;
                            }
                        }
                    }
                }

                _logger.LogInformation($"✅ Successfully parsed {indices.Count} Elasticsearch indices.");
            }
            catch (JsonException ex)
            {
                _logger.LogError($"❌ Failed to parse Elasticsearch JSON response: {ex.Message}");
                // Log the first 200 characters of raw data for debugging
                _logger.LogDebug($"Raw Data Snippet: {rawJson.Substring(0, Math.Min(rawJson.Length, 200))}");
            }

            return indices.OrderBy(i => i.IndexName).ToList();
        }

       

        /// <summary>
        /// Fetches Elasticsearch cluster health information.
        /// </summary>
        public async Task<ElasticClusterHealthModel> GetElasticClusterHealthAsync(string environmentName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string kubeConfigPath = ResolveKubeConfigFile(environmentName);
                    string elasticPod = GetElasticPodName(environmentName);
                    if (string.IsNullOrEmpty(elasticPod)) return new ElasticClusterHealthModel { Status = "offline" };

                    // Use http://127.0.0.1:9200 for internal container communication
                    string result = ExecuteKubectl($"exec {elasticPod} --kubeconfig=\"{kubeConfigPath}\" -n znode -- curl -s http://127.0.0.1:9200/_cluster/health");

                    if (string.IsNullOrEmpty(result)) return new ElasticClusterHealthModel { Status = "unknown" };

                    using (JsonDocument doc = JsonDocument.Parse(result))
                    {
                        var root = doc.RootElement;
                        return new ElasticClusterHealthModel
                        {
                            Status = root.GetProperty("status").GetString() ?? "unknown",
                            ClusterName = root.GetProperty("cluster_name").GetString() ?? "default",
                            ActiveNodes = root.GetProperty("number_of_nodes").GetInt32(),
                            TotalNodes = root.GetProperty("number_of_data_nodes").GetInt32(),
                            ActiveShards = root.GetProperty("active_shards").GetInt32(),
                            UnassignedShards = root.GetProperty("unassigned_shards").GetInt32()
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"❌ Health fetch failed: {ex.Message}");
                    return new ElasticClusterHealthModel { Status = "error" };
                }
            });
        }

        /// <summary>
        /// Fetches JVM heap, OS memory, and filesystem stats from all Elasticsearch nodes.
        /// </summary>
        public async Task<ElasticNodeStatsModel> GetElasticNodeStatsAsync(string environmentName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string kubeConfigPath = ResolveKubeConfigFile(environmentName);
                    string elasticPod = GetElasticPodName(environmentName);
                    if (string.IsNullOrEmpty(elasticPod) || string.IsNullOrEmpty(kubeConfigPath))
                        return new ElasticNodeStatsModel { Available = false, ErrorMessage = "Elasticsearch pod not found" };

                    string result = ExecuteKubectl(
                        $"exec {elasticPod} --kubeconfig=\"{kubeConfigPath}\" -n znode -- curl -s http://127.0.0.1:9200/_nodes/stats/jvm,os,fs");

                    if (string.IsNullOrEmpty(result))
                        return new ElasticNodeStatsModel { Available = false, ErrorMessage = "Empty response from _nodes/stats" };

                    using var doc = JsonDocument.Parse(result);
                    var root = doc.RootElement;

                    if (!root.TryGetProperty("nodes", out var nodesEl))
                        return new ElasticNodeStatsModel { Available = false, ErrorMessage = "No nodes element in response" };

                    long totalHeapMax = 0, totalHeapUsed = 0;
                    long totalRam = 0, totalRamUsed = 0;
                    long totalDisk = 0, availDisk = 0;
                    int nodeCount = 0;

                    foreach (var node in nodesEl.EnumerateObject())
                    {
                        nodeCount++;
                        try
                        {
                            if (node.Value.TryGetProperty("jvm", out var jvm) &&
                                jvm.TryGetProperty("mem", out var jvmMem))
                            {
                                if (jvmMem.TryGetProperty("heap_max_in_bytes", out var hMax)) totalHeapMax += hMax.GetInt64();
                                if (jvmMem.TryGetProperty("heap_used_in_bytes", out var hUsed)) totalHeapUsed += hUsed.GetInt64();
                            }
                            if (node.Value.TryGetProperty("os", out var os) &&
                                os.TryGetProperty("mem", out var osMem))
                            {
                                if (osMem.TryGetProperty("total_in_bytes", out var rTotal)) totalRam += rTotal.GetInt64();
                                if (osMem.TryGetProperty("used_in_bytes", out var rUsed)) totalRamUsed += rUsed.GetInt64();
                            }
                            if (node.Value.TryGetProperty("fs", out var fs) &&
                                fs.TryGetProperty("total", out var fsTotal))
                            {
                                if (fsTotal.TryGetProperty("total_in_bytes", out var dTotal)) totalDisk += dTotal.GetInt64();
                                if (fsTotal.TryGetProperty("available_in_bytes", out var dAvail)) availDisk += dAvail.GetInt64();
                            }
                        }
                        catch (Exception nodeEx)
                        {
                            _logger.LogWarning($"Skipping node stats parse: {nodeEx.Message}");
                        }
                    }

                    return new ElasticNodeStatsModel
                    {
                        Available = true,
                        NodeCount = nodeCount,
                        TotalHeapMaxMb = totalHeapMax / 1024 / 1024,
                        TotalHeapUsedMb = totalHeapUsed / 1024 / 1024,
                        HeapUsagePercent = totalHeapMax > 0 ? (int)(totalHeapUsed * 100L / totalHeapMax) : 0,
                        TotalRamMb = totalRam / 1024 / 1024,
                        TotalRamUsedMb = totalRamUsed / 1024 / 1024,
                        TotalDiskGb = totalDisk / 1024 / 1024 / 1024,
                        AvailableDiskGb = availDisk / 1024 / 1024 / 1024,
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError($"GetElasticNodeStatsAsync error: {ex.Message}");
                    return new ElasticNodeStatsModel { Available = false, ErrorMessage = ex.Message };
                }
            });
        }

        public async Task<ElasticQueryResultModel> ExecuteElasticQueryAsync(string environmentName, string indexName, string query, string httpMethod = "POST")
        {
            return await Task.Run(() =>
            {
                try
                {
                    string kubeConfigPath = ResolveKubeConfigFile(environmentName);
                    string elasticPod = GetElasticPodName(environmentName);

                    // 1. Sanitize the query: remove newlines and escape quotes for the internal JSON
                    string cleanQuery = query.Replace("\r", "").Replace("\n", " ").Replace("\"", "\\\"").Trim();

                    // 2. Command construction using double-quote strategy for Windows host
                    // We use -H \"Content-Type: application/json\" so it passes as a valid header
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    string result = ExecuteKubectl($"exec {elasticPod} --kubeconfig=\"{kubeConfigPath}\" -n znode -- curl -s -X {httpMethod.ToUpper()} \"http://127.0.0.1:9200/{indexName}/_search\" -H \"Content-Type: application/json\" -d \"{cleanQuery}\"");
                    sw.Stop();

                    return new ElasticQueryResultModel
                    {
                        Status = "success",
                        ResponseTime = $"{sw.ElapsedMilliseconds}ms",
                        Results = result // This is the string result for the Search_Result_Buffer
                    };
                }
                catch (Exception ex)
                {
                    return new ElasticQueryResultModel { Status = "error", Results = ex.Message };
                }
            });
        }

        /// <summary>
        /// Initiates port forwarding to a pod for local development/testing.
        /// </summary>
        public async Task<PortForwardResponse> StartPortForwardAsync(PortForwardRequest request)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Validate ports
                    if (request.LocalPort < 1024 || request.LocalPort > 65535)
                    {
                        return new PortForwardResponse
                        {
                            Status = "error",
                            Message = "Invalid local port number (must be 1024-65535)"
                        };
                    }

                    // Resolve kubeconfig file
                    string kubeConfigPath = ResolveKubeConfigFile(request.Environment);
                    if (string.IsNullOrEmpty(kubeConfigPath))
                    {
                        return new PortForwardResponse
                        {
                            Status = "error",
                            Message = $"Kubeconfig not found for environment: {request.Environment}"
                        };
                    }

                    // Build port forward command with kubeconfig
                    string command = BuildKubectlShellCommand($"port-forward pod/{request.PodName} {request.LocalPort}:{request.PodPort} -n {request.Namespace} --kubeconfig=\"{kubeConfigPath}\"");
                    
                    // Start as background process
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c {command}",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    var process = Process.Start(processInfo);
                    
                    Task.Delay(1500).Wait(); // Allow process to start and connect

                    if (process != null && !process.HasExited)
                    {
                        _logger.LogInformation($"✓ Port forwarding started: {request.LocalPort} -> {request.PodName}:{request.PodPort} (PID: {process.Id})");
                        
                        return new PortForwardResponse
                        {
                            Status = "success",
                            Message = $"Port forwarding active on localhost:{request.LocalPort}",
                            ForwardingUrl = $"http://localhost:{request.LocalPort}",
                            ProcessId = process.Id
                        };
                    }
                    else
                    {
                        // Check for error output
                        string errorOutput = process?.StandardError.ReadToEnd() ?? "Unknown error";
                        _logger.LogError($"❌ Port forward process exited: {errorOutput}");
                        return new PortForwardResponse
                        {
                            Status = "error",
                            Message = "Failed to start port forwarding process"
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"❌ Error starting port forward: {ex.Message}");
                    return new PortForwardResponse
                    {
                        Status = "error",
                        Message = $"Error: {ex.Message}"
                    };
                }
            });
        }

        /// <summary>
        /// Stops an active port forwarding process.
        /// </summary>
        public async Task<KubeOperationResponse> StopPortForwardAsync(int processId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var process = Process.GetProcessById(processId);
                    process.Kill();
                    process.WaitForExit();

                    _logger.LogInformation($"✓ Port forwarding stopped: PID {processId}");
                    
                    return new KubeOperationResponse
                    {
                        Status = "success",
                        Message = $"Port forwarding stopped successfully"
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError($"❌ Error stopping port forward: {ex.Message}");
                    return new KubeOperationResponse
                    {
                        Status = "error",
                        Message = $"Error stopping port forward: {ex.Message}"
                    };
                }
            });
        }

        /// <summary>
        /// Gets Elasticsearch pod name for the specified environment.
        /// </summary>
        private string GetElasticPodName(string environmentName)
        {
            try
            {
                string kubeConfigPath = ResolveKubeConfigFile(environmentName);
                if (string.IsNullOrEmpty(kubeConfigPath)) return null;

                // Fetch ALL pods in znode namespace as JSON to avoid label selector issues
                string output = ExecuteKubectl($"get pods --kubeconfig=\"{kubeConfigPath}\" -n znode -o json");

                if (string.IsNullOrEmpty(output)) return null;

                using (JsonDocument doc = JsonDocument.Parse(output))
                {
                    var items = doc.RootElement.GetProperty("items").EnumerateArray();

                    // Search for any pod whose name contains "elasticsearch" and is currently "Running"
                    var pod = items.FirstOrDefault(p => {
                        string name = p.GetProperty("metadata").GetProperty("name").GetString()?.ToLower() ?? "";
                        string phase = p.GetProperty("status").GetProperty("phase").GetString() ?? "";
                        return (name.Contains("elasticsearch") || name.Contains("es-")) && phase == "Running";
                    });

                    if (pod.ValueKind != JsonValueKind.Undefined)
                    {
                        return pod.GetProperty("metadata").GetProperty("name").GetString();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Fatal error finding Elastic pod: {ex.Message}");
            }
            return null;
        }

        private string ExecuteKubectl(string arguments)
        {
            if (_bundledKubectlAvailable)
            {
                return ExecuteCommand(_kubectlExecutable, arguments);
            }

            return ExecuteCommand("kubectl", arguments);
        }

        private string BuildKubectlShellCommand(string arguments)
        {
            if (_bundledKubectlAvailable)
            {
                return $"\"{_kubectlExecutable}\" {arguments}";
            }

            return $"kubectl {arguments}";
        }

        /// <summary>
        /// Executes a system command and returns the output.
        /// </summary>
        private string ExecuteCommand(string fileName, string arguments)
        {
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo.FileName = fileName;
                    process.StartInfo.Arguments = arguments;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;

                    process.Start();

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    process.WaitForExit();

                    if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
                    {
                        _logger.LogWarning($"Command execution warning: {error}");
                    }

                    return output.Trim();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error executing command: {ex.Message}");
                return string.Empty;
            }
        }

        private string TryReadConfigMapValue(
            Kubernetes client,
            string configKey,
            string configMapName = "znode10xapi-configmap",
            string namespaceName = "znode",
            bool suppressMissingLog = false)
        {
            try
            {
                var configMap = client.ReadNamespacedConfigMapAsync(configMapName, namespaceName).GetAwaiter().GetResult();
                if (configMap?.Data != null && configMap.Data.TryGetValue(configKey, out var value) && !string.IsNullOrEmpty(value))
                {
                    return value;
                }

                if (!suppressMissingLog)
                {
                    _logger.LogDebug($"ConfigMap value missing for key '{configKey}' in '{configMapName}'.");
                }
            }
            catch (k8s.Autorest.HttpOperationException httpEx)
            {
                if (!suppressMissingLog)
                {
                    _logger.LogWarning($"ConfigMap fetch failed for key '{configKey}' in '{configMapName}'. Status: {httpEx.Response?.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                if (!suppressMissingLog)
                {
                    _logger.LogError($"Error reading ConfigMap value: {ex.Message}");
                }
            }

            return string.Empty;
        }

        private string TryReadConfigMapValueFromSources(
            Kubernetes client,
            IEnumerable<string> keys,
            IEnumerable<string> configMapNames,
            IEnumerable<string> namespaces)
        {
            foreach (var ns in namespaces)
            {
                foreach (var mapName in configMapNames)
                {
                    foreach (var key in keys)
                    {
                        var value = TryReadConfigMapValue(client, key, mapName, ns, suppressMissingLog: true);
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            _logger.LogDebug($"ConfigMap '{mapName}' (ns='{ns}') provided value for key '{key}'.");
                            return value;
                        }
                    }
                }
            }

            return string.Empty;
        }

        private (string domainName, string domainKey) TryReadDomainSettings(Kubernetes client)
        {
            var configMapCandidates = new[]
            {
                "znode10xadmin-configmap",
            };

            var namespaceCandidates = new[]
            {
                "znode"
            };

            var domainKeyCandidates = new[]
            {
                "appsettings__ZnodeApiDomainKey"
            };

            var domainNameCandidates = new[]
            {
                "appsettings__ZnodeApiDomainName"
            };

            string domainName = TryReadConfigMapValueFromSources(client, domainNameCandidates, configMapCandidates, namespaceCandidates);
            if (string.IsNullOrWhiteSpace(domainName))
            {
                foreach (var ns in namespaceCandidates)
                {
                    domainName = TryReadSecretValue(client, "ZnodeApiDomainName", ns);
                    if (!string.IsNullOrWhiteSpace(domainName))
                    {
                        break;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(domainName))
            {
                foreach (var ns in namespaceCandidates)
                {
                    domainName = TryReadSecretValue(client, "ApiDomainName", ns);
                    if (!string.IsNullOrWhiteSpace(domainName))
                    {
                        break;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(domainName))
            {
                var fallbackHost = TryReadServiceHostname(client, "znode-api");
                if (!string.IsNullOrWhiteSpace(fallbackHost))
                {
                    domainName = fallbackHost.Replace("https://", string.Empty).Replace("http://", string.Empty).Trim('/');
                    _logger.LogDebug("Derived domain name from znode-api service hostname.");
                }
            }

            if (string.IsNullOrWhiteSpace(domainName))
            {
                _logger.LogWarning("Hosted domain name not found in ConfigMaps, Secrets, or service host fallback.");
            }

            string domainKey = TryReadConfigMapValueFromSources(client, domainKeyCandidates, configMapCandidates, namespaceCandidates);
            if (string.IsNullOrWhiteSpace(domainKey))
            {
                foreach (var ns in namespaceCandidates)
                {
                    domainKey = TryReadSecretValue(client, "ZnodeApiDomainKey", ns);
                    if (!string.IsNullOrWhiteSpace(domainKey))
                    {
                        break;
                    }
                }

                if (string.IsNullOrWhiteSpace(domainKey))
                {
                    foreach (var ns in namespaceCandidates)
                    {
                        domainKey = TryReadSecretValue(client, "ApiDomainKey", ns);
                        if (!string.IsNullOrWhiteSpace(domainKey))
                        {
                            break;
                        }
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(domainKey))
            {
                _logger.LogWarning("Hosted domain key not found in ConfigMaps or Secrets.");
            }

            return (domainName?.Trim(), domainKey?.Trim());
        }

        private Dictionary<string, string> TryReadHostedAppSettings(Kubernetes client, IEnumerable<string> configMapNames, IEnumerable<string> namespaces)
        {
            var results = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var key in HostedAppSettingKeys)
            {
                var value = TryReadConfigMapValueFromSources(client, new[] { key }, configMapNames, namespaces);

                if (string.IsNullOrWhiteSpace(value) && key.StartsWith("ConnectionStrings__", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var ns in namespaces)
                    {
                        value = TryReadSecretValue(client, key, ns);
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            break;
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(value))
                {
                    results[key] = value.Trim();
                }
            }

            return results;
        }

        private string TryReadSecretValue(Kubernetes client, string configKey, string namespaceName = "znode")
        {
            try
            {
                var secrets = client.ListNamespacedSecretAsync(namespaceName).GetAwaiter().GetResult();
                foreach (var secret in secrets?.Items ?? Enumerable.Empty<V1Secret>())
                {
                    if (secret?.Data != null && secret.Data.TryGetValue(configKey, out var rawBytes) && rawBytes != null)
                    {
                        return Encoding.UTF8.GetString(rawBytes);
                    }

                    if (secret?.StringData != null && secret.StringData.TryGetValue(configKey, out var rawString) && !string.IsNullOrEmpty(rawString))
                    {
                        return rawString;
                    }
                }

                _logger.LogDebug($"Secret value missing for key '{configKey}' in namespace '{namespaceName}'.");
            }
            catch (k8s.Autorest.HttpOperationException httpEx)
            {
                _logger.LogWarning($"Secret fetch failed for key '{configKey}' in namespace '{namespaceName}'. Status: {httpEx.Response?.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error reading Secret value: {ex.Message}");
            }

            return string.Empty;
        }

        private string TryReadServiceHostname(Kubernetes client, string serviceName, string namespaceName = "znode")
        {
            try
            {
                var service = client.ReadNamespacedServiceAsync(serviceName, namespaceName).GetAwaiter().GetResult();
                var ingress = service?.Status?.LoadBalancer?.Ingress?.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(ingress?.Hostname))
                {
                    return ingress.Hostname.Trim();
                }

                if (!string.IsNullOrWhiteSpace(ingress?.Ip))
                {
                    return ingress.Ip.Trim();
                }

                _logger.LogDebug($"Service '{serviceName}' has no ingress host or IP exposed.");
            }
            catch (k8s.Autorest.HttpOperationException httpEx)
            {
                _logger.LogWarning($"Service fetch failed for '{serviceName}'. Status: {httpEx.Response?.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error reading Service '{serviceName}': {ex.Message}");
            }

            return string.Empty;
        }

        // ── NODE HEALTH ───────────────────────────────────────────────────────
        public async Task<List<KubeNodeModel>> GetNodesAsync(string environmentName)
        {
            return await Task.Run(() =>
            {
                var nodes = new List<KubeNodeModel>();
                try
                {
                    string kc = ResolveKubeConfigFile(environmentName);
                    if (string.IsNullOrEmpty(kc)) return nodes;

                    string json = ExecuteKubectl($"get nodes --kubeconfig=\"{kc}\" -o json");
                    if (string.IsNullOrEmpty(json)) return nodes;

                    // Pod counts per node
                    string podsJson = ExecuteKubectl($"get pods --kubeconfig=\"{kc}\" -A -o json");
                    var podCountByNode = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    if (!string.IsNullOrEmpty(podsJson))
                    {
                        using var pd = JsonDocument.Parse(podsJson);
                        foreach (var p in pd.RootElement.GetProperty("items").EnumerateArray())
                        {
                            if (p.TryGetProperty("spec", out var sp) && sp.TryGetProperty("nodeName", out var nn))
                            {
                                var n = nn.GetString() ?? "";
                                podCountByNode[n] = podCountByNode.GetValueOrDefault(n, 0) + 1;
                            }
                        }
                    }

                    // Node metrics
                    string topOut = ExecuteKubectl($"top nodes --kubeconfig=\"{kc}\" --no-headers");
                    var nodeCpuMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    var nodeMemMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    var nodeCpuPctMap = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                    var nodeMemPctMap = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                    if (!string.IsNullOrEmpty(topOut))
                    {
                        foreach (var ln in topOut.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                        {
                            var parts = ln.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 5)
                            {
                                nodeCpuMap[parts[0]] = parts[1];
                                nodeCpuPctMap[parts[0]] = double.TryParse(parts[2].TrimEnd('%'), out var cp) ? cp : 0;
                                nodeMemMap[parts[0]] = parts[3];
                                nodeMemPctMap[parts[0]] = double.TryParse(parts[4].TrimEnd('%'), out var mp) ? mp : 0;
                            }
                        }
                    }

                    using var doc = JsonDocument.Parse(json);
                    foreach (var item in doc.RootElement.GetProperty("items").EnumerateArray())
                    {
                        var node = new KubeNodeModel();
                        if (item.TryGetProperty("metadata", out var meta))
                        {
                            node.Name = meta.GetProperty("name").GetString() ?? "";
                            if (meta.TryGetProperty("creationTimestamp", out var ts) && DateTime.TryParse(ts.GetString(), out var dt))
                                node.AgeDays = (int)(DateTime.UtcNow - dt).TotalDays;
                            if (meta.TryGetProperty("labels", out var labels))
                            {
                                var roles = new List<string>();
                                foreach (var lbl in labels.EnumerateObject())
                                    if (lbl.Name.StartsWith("node-role.kubernetes.io/")) roles.Add(lbl.Name.Split('/').Last());
                                node.Roles = roles.Count > 0 ? string.Join(",", roles) : "worker";
                            }
                        }
                        if (item.TryGetProperty("status", out var status))
                        {
                            if (status.TryGetProperty("nodeInfo", out var info))
                            {
                                node.KernelVersion = info.TryGetProperty("kernelVersion", out var kv) ? kv.GetString() : "";
                                node.OsImage = info.TryGetProperty("osImage", out var os) ? os.GetString() : "";
                                node.ContainerRuntime = info.TryGetProperty("containerRuntimeVersion", out var cr) ? cr.GetString() : "";
                                node.Architecture = info.TryGetProperty("architecture", out var arch) ? arch.GetString() : "";
                            }
                            if (status.TryGetProperty("capacity", out var cap))
                            {
                                node.CpuCapacity = cap.TryGetProperty("cpu", out var cc) ? cc.GetString() : "";
                                node.MemoryCapacity = cap.TryGetProperty("memory", out var mc) ? mc.GetString() : "";
                            }
                            if (status.TryGetProperty("conditions", out var conds) && conds.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var c in conds.EnumerateArray())
                                {
                                    var ctype = c.TryGetProperty("type", out var ct) ? ct.GetString() : "";
                                    var cstatus = c.TryGetProperty("status", out var cs) ? cs.GetString() : "";
                                    var isTrue = cstatus == "True";
                                    if (ctype == "Ready") { node.IsReady = isTrue; if (isTrue) node.Status = "Ready"; else node.Status = "NotReady"; }
                                    if (ctype == "MemoryPressure") node.MemoryPressure = isTrue;
                                    if (ctype == "DiskPressure") node.DiskPressure = isTrue;
                                    if (ctype == "PIDPressure") node.PidPressure = isTrue;
                                    if (isTrue && ctype != "Ready") node.Conditions.Add(ctype);
                                }
                            }
                        }
                        node.PodCount = podCountByNode.GetValueOrDefault(node.Name, 0);
                        node.CpuUsage = nodeCpuMap.GetValueOrDefault(node.Name, "—");
                        node.MemoryUsage = nodeMemMap.GetValueOrDefault(node.Name, "—");
                        node.CpuUsagePct = nodeCpuPctMap.GetValueOrDefault(node.Name, 0);
                        node.MemoryUsagePct = nodeMemPctMap.GetValueOrDefault(node.Name, 0);
                        if (string.IsNullOrEmpty(node.Status)) node.Status = "Unknown";
                        nodes.Add(node);
                    }
                }
                catch (Exception ex) { _logger.LogError($"GetNodesAsync error: {ex.Message}"); }
                return nodes;
            });
        }

        // ── EVENTS ────────────────────────────────────────────────────────────
        public async Task<List<KubeEventModel>> GetEventsAsync(string environmentName, string eventType = "all")
        {
            return await Task.Run(() =>
            {
                var events = new List<KubeEventModel>();
                try
                {
                    string kc = ResolveKubeConfigFile(environmentName);
                    if (string.IsNullOrEmpty(kc)) return events;

                    string json = ExecuteKubectl($"get events --kubeconfig=\"{kc}\" -A -o json --sort-by=.lastTimestamp");
                    if (string.IsNullOrEmpty(json)) return events;

                    using var doc = JsonDocument.Parse(json);
                    foreach (var item in doc.RootElement.GetProperty("items").EnumerateArray())
                    {
                        var ev = new KubeEventModel();
                        if (item.TryGetProperty("metadata", out var meta))
                            ev.Namespace = meta.TryGetProperty("namespace", out var ns) ? ns.GetString() : "";

                        ev.Type = item.TryGetProperty("type", out var tp) ? tp.GetString() : "Normal";
                        ev.Reason = item.TryGetProperty("reason", out var rs) ? rs.GetString() : "";
                        ev.Message = item.TryGetProperty("message", out var mg) ? mg.GetString() : "";
                        ev.Count = item.TryGetProperty("count", out var cnt) ? cnt.GetInt32() : 1;

                        if (item.TryGetProperty("involvedObject", out var obj))
                        {
                            var kind = obj.TryGetProperty("kind", out var k) ? k.GetString() : "";
                            var name = obj.TryGetProperty("name", out var n) ? n.GetString() : "";
                            ev.Object = $"{kind}/{name}";
                        }
                        if (item.TryGetProperty("source", out var src))
                            ev.Source = src.TryGetProperty("component", out var comp) ? comp.GetString() : "";

                        if (item.TryGetProperty("lastTimestamp", out var lt) && DateTime.TryParse(lt.GetString(), out var ldt))
                            ev.LastSeen = (DateTime.UtcNow - ldt).TotalMinutes < 60
                                ? $"{(int)(DateTime.UtcNow - ldt).TotalMinutes}m ago"
                                : $"{(int)(DateTime.UtcNow - ldt).TotalHours}h ago";
                        else
                            ev.LastSeen = "Unknown";

                        if (eventType == "warning" && (ev.Type ?? "").ToLower() != "warning") continue;
                        events.Add(ev);
                    }
                    events.Reverse(); // newest first
                }
                catch (Exception ex) { _logger.LogError($"GetEventsAsync error: {ex.Message}"); }
                return events;
            });
        }

        // ── DEPLOYMENTS ───────────────────────────────────────────────────────
        public async Task<List<KubeDeploymentModel>> GetDeploymentsAsync(string environmentName)
        {
            return await Task.Run(() =>
            {
                var deployments = new List<KubeDeploymentModel>();
                try
                {
                    string kc = ResolveKubeConfigFile(environmentName);
                    if (string.IsNullOrEmpty(kc)) return deployments;

                    string json = ExecuteKubectl($"get deployments --kubeconfig=\"{kc}\" -A -o json");
                    if (string.IsNullOrEmpty(json)) return deployments;

                    using var doc = JsonDocument.Parse(json);
                    foreach (var item in doc.RootElement.GetProperty("items").EnumerateArray())
                    {
                        var d = new KubeDeploymentModel();
                        if (item.TryGetProperty("metadata", out var meta))
                        {
                            d.Name = meta.TryGetProperty("name", out var n) ? n.GetString() : "";
                            d.Namespace = meta.TryGetProperty("namespace", out var ns) ? ns.GetString() : "";
                            if (meta.TryGetProperty("creationTimestamp", out var ts) && DateTime.TryParse(ts.GetString(), out var dt))
                                d.AgeDays = (int)(DateTime.UtcNow - dt).TotalDays;
                        }
                        if (item.TryGetProperty("spec", out var spec))
                        {
                            d.Desired = spec.TryGetProperty("replicas", out var rep) ? rep.GetInt32() : 0;
                            if (spec.TryGetProperty("strategy", out var strat) && strat.TryGetProperty("type", out var st))
                                d.Strategy = st.GetString() ?? "";
                            if (spec.TryGetProperty("template", out var tmpl) &&
                                tmpl.TryGetProperty("spec", out var tspec) &&
                                tspec.TryGetProperty("containers", out var cons) &&
                                cons.ValueKind == JsonValueKind.Array)
                            {
                                var first = cons.EnumerateArray().FirstOrDefault();
                                if (first.ValueKind != JsonValueKind.Undefined && first.TryGetProperty("image", out var img))
                                    d.Image = img.GetString() ?? "";
                            }
                        }
                        if (item.TryGetProperty("status", out var status))
                        {
                            d.Ready = status.TryGetProperty("readyReplicas", out var rr) ? rr.GetInt32() : 0;
                            d.Available = status.TryGetProperty("availableReplicas", out var ar) ? ar.GetInt32() : 0;
                            d.UpToDate = status.TryGetProperty("updatedReplicas", out var ur) ? ur.GetInt32() : 0;
                        }
                        d.Status = d.Ready >= d.Desired && d.Desired > 0 ? "Healthy" : d.Ready > 0 ? "Degraded" : "Unavailable";
                        deployments.Add(d);
                    }
                }
                catch (Exception ex) { _logger.LogError($"GetDeploymentsAsync error: {ex.Message}"); }
                return deployments.OrderBy(d => d.Namespace).ThenBy(d => d.Name).ToList();
            });
        }

        // ── JOBS ──────────────────────────────────────────────────────────────
        public async Task<List<KubeJobModel>> GetJobsAsync(string environmentName)
        {
            return await Task.Run(() =>
            {
                var jobs = new List<KubeJobModel>();
                try
                {
                    string kc = ResolveKubeConfigFile(environmentName);
                    if (string.IsNullOrEmpty(kc)) return jobs;

                    string json = ExecuteKubectl($"get jobs --kubeconfig=\"{kc}\" -A -o json");
                    if (string.IsNullOrEmpty(json)) return jobs;

                    using var doc = JsonDocument.Parse(json);
                    foreach (var item in doc.RootElement.GetProperty("items").EnumerateArray())
                    {
                        var j = new KubeJobModel { Type = "Job" };
                        if (item.TryGetProperty("metadata", out var meta))
                        {
                            j.Name = meta.TryGetProperty("name", out var n) ? n.GetString() : "";
                            j.Namespace = meta.TryGetProperty("namespace", out var ns) ? ns.GetString() : "";
                            if (meta.TryGetProperty("creationTimestamp", out var ts) && DateTime.TryParse(ts.GetString(), out var dt))
                                j.AgeDays = (int)(DateTime.UtcNow - dt).TotalDays;
                        }
                        if (item.TryGetProperty("status", out var status))
                        {
                            j.Active = status.TryGetProperty("active", out var a) ? a.GetInt32() : 0;
                            j.Succeeded = status.TryGetProperty("succeeded", out var s) ? s.GetInt32() : 0;
                            j.Failed = status.TryGetProperty("failed", out var f) ? f.GetInt32() : 0;
                            if (status.TryGetProperty("completionTime", out var ct) && DateTime.TryParse(ct.GetString(), out var cdt))
                                j.CompletionTime = cdt.ToString("yyyy-MM-dd HH:mm");
                        }
                        j.Status = j.Active > 0 ? "Running" : j.Succeeded > 0 ? "Completed" : j.Failed > 0 ? "Failed" : "Pending";
                        jobs.Add(j);
                    }
                }
                catch (Exception ex) { _logger.LogError($"GetJobsAsync error: {ex.Message}"); }
                return jobs;
            });
        }

        // ── CRONJOBS ──────────────────────────────────────────────────────────
        public async Task<List<KubeJobModel>> GetCronJobsAsync(string environmentName)
        {
            return await Task.Run(() =>
            {
                var jobs = new List<KubeJobModel>();
                try
                {
                    string kc = ResolveKubeConfigFile(environmentName);
                    if (string.IsNullOrEmpty(kc)) return jobs;

                    string json = ExecuteKubectl($"get cronjobs --kubeconfig=\"{kc}\" -A -o json");
                    if (string.IsNullOrEmpty(json)) return jobs;

                    using var doc = JsonDocument.Parse(json);
                    foreach (var item in doc.RootElement.GetProperty("items").EnumerateArray())
                    {
                        var j = new KubeJobModel { Type = "CronJob" };
                        if (item.TryGetProperty("metadata", out var meta))
                        {
                            j.Name = meta.TryGetProperty("name", out var n) ? n.GetString() : "";
                            j.Namespace = meta.TryGetProperty("namespace", out var ns) ? ns.GetString() : "";
                            if (meta.TryGetProperty("creationTimestamp", out var ts) && DateTime.TryParse(ts.GetString(), out var dt))
                                j.AgeDays = (int)(DateTime.UtcNow - dt).TotalDays;
                        }
                        if (item.TryGetProperty("spec", out var spec))
                        {
                            j.Schedule = spec.TryGetProperty("schedule", out var sch) ? sch.GetString() : "";
                            j.Suspended = spec.TryGetProperty("suspend", out var sus) && sus.GetBoolean();
                        }
                        if (item.TryGetProperty("status", out var status))
                        {
                            j.Active = status.TryGetProperty("active", out var a) && a.ValueKind == JsonValueKind.Array ? a.GetArrayLength() : 0;
                            if (status.TryGetProperty("lastScheduleTime", out var ls) && DateTime.TryParse(ls.GetString(), out var ldt))
                                j.LastSchedule = ldt.ToString("yyyy-MM-dd HH:mm");
                        }
                        j.Status = j.Suspended ? "Suspended" : j.Active > 0 ? "Running" : "Scheduled";
                        jobs.Add(j);
                    }
                }
                catch (Exception ex) { _logger.LogError($"GetCronJobsAsync error: {ex.Message}"); }
                return jobs;
            });
        }

        // ── RESOURCE LIMITS ───────────────────────────────────────────────────
        public async Task<List<KubeContainerResourceModel>> GetResourceLimitsAsync(string environmentName)
        {
            return await Task.Run(() =>
            {
                var result = new List<KubeContainerResourceModel>();
                try
                {
                    string kc = ResolveKubeConfigFile(environmentName);
                    if (string.IsNullOrEmpty(kc)) return result;

                    string json = ExecuteKubectl($"get pods --kubeconfig=\"{kc}\" -A -o json");
                    if (string.IsNullOrEmpty(json)) return result;

                    // metrics map
                    string topOut = ExecuteKubectl($"top pods --kubeconfig=\"{kc}\" -A --no-headers");
                    var cpuActual = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    var memActual = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    if (!string.IsNullOrEmpty(topOut))
                    {
                        foreach (var ln in topOut.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                        {
                            var p = ln.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            if (p.Length >= 3) { cpuActual[p[1]] = p[2]; memActual[p[1]] = p[3 < p.Length ? 3 : 2]; }
                        }
                    }

                    using var doc = JsonDocument.Parse(json);
                    foreach (var pod in doc.RootElement.GetProperty("items").EnumerateArray())
                    {
                        string podName = "", ns = "";
                        if (pod.TryGetProperty("metadata", out var meta))
                        {
                            podName = meta.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                            ns = meta.TryGetProperty("namespace", out var nse) ? nse.GetString() ?? "" : "";
                        }
                        if (!pod.TryGetProperty("spec", out var spec)) continue;
                        if (!spec.TryGetProperty("containers", out var containers)) continue;
                        foreach (var c in containers.EnumerateArray())
                        {
                            var row = new KubeContainerResourceModel { PodName = podName, Namespace = ns };
                            row.ContainerName = c.TryGetProperty("name", out var cn) ? cn.GetString() ?? "" : "";
                            if (c.TryGetProperty("resources", out var res))
                            {
                                if (res.TryGetProperty("requests", out var req))
                                {
                                    row.CpuRequest = req.TryGetProperty("cpu", out var cr) ? cr.GetString() ?? "" : "";
                                    row.MemoryRequest = req.TryGetProperty("memory", out var mr) ? mr.GetString() ?? "" : "";
                                }
                                if (res.TryGetProperty("limits", out var lim))
                                {
                                    row.CpuLimit = lim.TryGetProperty("cpu", out var cl) ? cl.GetString() ?? "" : "";
                                    row.MemoryLimit = lim.TryGetProperty("memory", out var ml) ? ml.GetString() ?? "" : "";
                                }
                            }
                            row.CpuActual = cpuActual.GetValueOrDefault(podName, "—");
                            row.MemoryActual = memActual.GetValueOrDefault(podName, "—");
                            result.Add(row);
                        }
                    }
                }
                catch (Exception ex) { _logger.LogError($"GetResourceLimitsAsync error: {ex.Message}"); }
                return result;
            });
        }

        // ── HPA ───────────────────────────────────────────────────────────────
        public async Task<List<KubeHpaModel>> GetHpasAsync(string environmentName)
        {
            return await Task.Run(() =>
            {
                var hpas = new List<KubeHpaModel>();
                try
                {
                    string kc = ResolveKubeConfigFile(environmentName);
                    if (string.IsNullOrEmpty(kc)) return hpas;

                    string json = ExecuteKubectl($"get hpa --kubeconfig=\"{kc}\" -A -o json");
                    if (string.IsNullOrEmpty(json)) return hpas;

                    using var doc = JsonDocument.Parse(json);
                    foreach (var item in doc.RootElement.GetProperty("items").EnumerateArray())
                    {
                        var h = new KubeHpaModel();
                        if (item.TryGetProperty("metadata", out var meta))
                        {
                            h.Name = meta.TryGetProperty("name", out var n) ? n.GetString() : "";
                            h.Namespace = meta.TryGetProperty("namespace", out var ns) ? ns.GetString() : "";
                            if (meta.TryGetProperty("creationTimestamp", out var ts) && DateTime.TryParse(ts.GetString(), out var dt))
                                h.AgeDays = (int)(DateTime.UtcNow - dt).TotalDays;
                        }
                        if (item.TryGetProperty("spec", out var spec))
                        {
                            h.MinReplicas = spec.TryGetProperty("minReplicas", out var mn) ? mn.GetInt32() : 1;
                            h.MaxReplicas = spec.TryGetProperty("maxReplicas", out var mx) ? mx.GetInt32() : 0;
                            if (spec.TryGetProperty("scaleTargetRef", out var tref))
                                h.Target = (tref.TryGetProperty("kind", out var k) ? k.GetString() : "") + "/" +
                                           (tref.TryGetProperty("name", out var tn) ? tn.GetString() : "");
                            if (spec.TryGetProperty("metrics", out var mets) && mets.ValueKind == JsonValueKind.Array)
                            {
                                var mList = new List<string>();
                                foreach (var m in mets.EnumerateArray())
                                {
                                    var mtype = m.TryGetProperty("type", out var mt) ? mt.GetString() : "";
                                    if (mtype == "Resource" && m.TryGetProperty("resource", out var rr))
                                    {
                                        var rname = rr.TryGetProperty("name", out var rn) ? rn.GetString() : "";
                                        if (rr.TryGetProperty("target", out var tg))
                                        {
                                            var val = tg.TryGetProperty("averageUtilization", out var av) ? av.GetInt32().ToString() + "%" :
                                                      tg.TryGetProperty("averageValue", out var avv) ? avv.GetString() : "";
                                            mList.Add($"{rname}:{val}");
                                        }
                                    }
                                    else if (mtype == "External" || mtype == "Pods") mList.Add(mtype);
                                }
                                h.Metrics = string.Join(", ", mList);
                            }
                        }
                        if (item.TryGetProperty("status", out var status))
                        {
                            h.CurrentReplicas = status.TryGetProperty("currentReplicas", out var cr) ? cr.GetInt32() : 0;
                            h.DesiredReplicas = status.TryGetProperty("desiredReplicas", out var dr) ? dr.GetInt32() : 0;
                        }
                        hpas.Add(h);
                    }
                }
                catch (Exception ex) { _logger.LogError($"GetHpasAsync error: {ex.Message}"); }
                return hpas;
            });
        }

        // ── CONTAINER DETAILS ─────────────────────────────────────────────────
        public async Task<List<KubeContainerDetailModel>> GetContainerDetailsAsync(string environmentName, string podName)
        {
            return await Task.Run(() =>
            {
                var details = new List<KubeContainerDetailModel>();
                try
                {
                    string kc = ResolveKubeConfigFile(environmentName);
                    if (string.IsNullOrEmpty(kc)) return details;

                    string filter = string.IsNullOrWhiteSpace(podName) ? "-A" : $"-n znode";
                    string json = ExecuteKubectl($"get pods {filter} --kubeconfig=\"{kc}\" -o json");
                    if (string.IsNullOrEmpty(json)) return details;

                    using var doc = JsonDocument.Parse(json);
                    var pods = doc.RootElement.GetProperty("items").EnumerateArray()
                        .Where(p => string.IsNullOrWhiteSpace(podName) ||
                            (p.TryGetProperty("metadata", out var m) && m.TryGetProperty("name", out var n) && n.GetString() == podName));

                    foreach (var pod in pods)
                    {
                        string pname = "";
                        if (pod.TryGetProperty("metadata", out var meta))
                            pname = meta.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";

                        // spec containers
                        var specContainers = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
                        if (pod.TryGetProperty("spec", out var spec) && spec.TryGetProperty("containers", out var spCons))
                            foreach (var c in spCons.EnumerateArray())
                                if (c.TryGetProperty("name", out var cn)) specContainers[cn.GetString() ?? ""] = c;

                        if (!pod.TryGetProperty("status", out var status)) continue;
                        if (!status.TryGetProperty("containerStatuses", out var statuses)) continue;

                        foreach (var cs in statuses.EnumerateArray())
                        {
                            var d = new KubeContainerDetailModel { PodName = pname };
                            d.ContainerName = cs.TryGetProperty("name", out var cn) ? cn.GetString() ?? "" : "";
                            d.Image = cs.TryGetProperty("image", out var img) ? img.GetString() ?? "" : "";
                            d.Ready = cs.TryGetProperty("ready", out var rdy) && rdy.GetBoolean();
                            d.RestartCount = cs.TryGetProperty("restartCount", out var rc) ? rc.GetInt32() : 0;
                            if (cs.TryGetProperty("state", out var st))
                            {
                                if (st.TryGetProperty("running", out _)) { d.State = "Running"; }
                                else if (st.TryGetProperty("waiting", out var wt)) { d.State = "Waiting"; d.StateReason = wt.TryGetProperty("reason", out var wr) ? wr.GetString() : ""; }
                                else if (st.TryGetProperty("terminated", out var tt)) { d.State = "Terminated"; d.StateReason = tt.TryGetProperty("reason", out var tr) ? tr.GetString() : ""; }
                            }
                            if (specContainers.TryGetValue(d.ContainerName, out var specC))
                            {
                                if (specC.TryGetProperty("resources", out var res))
                                {
                                    if (res.TryGetProperty("requests", out var req)) { d.CpuRequest = req.TryGetProperty("cpu", out var cr) ? cr.GetString() : ""; d.MemoryRequest = req.TryGetProperty("memory", out var mr) ? mr.GetString() : ""; }
                                    if (res.TryGetProperty("limits", out var lim)) { d.CpuLimit = lim.TryGetProperty("cpu", out var cl) ? cl.GetString() : ""; d.MemoryLimit = lim.TryGetProperty("memory", out var ml) ? ml.GetString() : ""; }
                                }
                                d.LivenessProbe = specC.TryGetProperty("livenessProbe", out _);
                                d.ReadinessProbe = specC.TryGetProperty("readinessProbe", out _);
                                d.StartupProbe = specC.TryGetProperty("startupProbe", out _);
                            }
                            details.Add(d);
                        }
                    }
                }
                catch (Exception ex) { _logger.LogError($"GetContainerDetailsAsync error: {ex.Message}"); }
                return details;
            });
        }

        // ── STATEFULSETS ──────────────────────────────────────────────────────
        public async Task<List<KubeWorkloadModel>> GetStatefulSetsAsync(string environmentName)
        {
            return await Task.Run(() => GetWorkloadsInternal(environmentName, "statefulsets", "StatefulSet"));
        }

        // ── DAEMONSETS ────────────────────────────────────────────────────────
        public async Task<List<KubeWorkloadModel>> GetDaemonSetsAsync(string environmentName)
        {
            return await Task.Run(() => GetWorkloadsInternal(environmentName, "daemonsets", "DaemonSet"));
        }

        private List<KubeWorkloadModel> GetWorkloadsInternal(string environmentName, string resource, string workloadType)
        {
            var list = new List<KubeWorkloadModel>();
            try
            {
                string kc = ResolveKubeConfigFile(environmentName);
                if (string.IsNullOrEmpty(kc)) return list;

                string json = ExecuteKubectl($"get {resource} --kubeconfig=\"{kc}\" -A -o json");
                if (string.IsNullOrEmpty(json)) return list;

                using var doc = JsonDocument.Parse(json);
                foreach (var item in doc.RootElement.GetProperty("items").EnumerateArray())
                {
                    var w = new KubeWorkloadModel { WorkloadType = workloadType };
                    if (item.TryGetProperty("metadata", out var meta))
                    {
                        w.Name = meta.TryGetProperty("name", out var n) ? n.GetString() : "";
                        w.Namespace = meta.TryGetProperty("namespace", out var ns) ? ns.GetString() : "";
                        if (meta.TryGetProperty("creationTimestamp", out var ts) && DateTime.TryParse(ts.GetString(), out var dt))
                            w.AgeDays = (int)(DateTime.UtcNow - dt).TotalDays;
                    }
                    if (item.TryGetProperty("spec", out var spec))
                    {
                        w.Desired = spec.TryGetProperty("replicas", out var rep) ? rep.GetInt32() : 0;
                        if (spec.TryGetProperty("template", out var tmpl) &&
                            tmpl.TryGetProperty("spec", out var ts2) &&
                            ts2.TryGetProperty("containers", out var cons) && cons.ValueKind == JsonValueKind.Array)
                        {
                            var first = cons.EnumerateArray().FirstOrDefault();
                            if (first.ValueKind != JsonValueKind.Undefined && first.TryGetProperty("image", out var img))
                                w.Image = img.GetString() ?? "";
                        }
                    }
                    if (item.TryGetProperty("status", out var status))
                    {
                        w.Ready = status.TryGetProperty("readyReplicas", out var rr) ? rr.GetInt32() : 0;
                        w.Available = status.TryGetProperty("availableReplicas", out var ar) ? ar.GetInt32() : 0;
                        w.Current = status.TryGetProperty("currentReplicas", out var cr) ? cr.GetInt32() : 0;
                        w.Updated = status.TryGetProperty("updatedReplicas", out var ur) ? ur.GetInt32() : 0;
                        // DaemonSets use different field names
                        if (workloadType == "DaemonSet")
                        {
                            w.Desired = status.TryGetProperty("desiredNumberScheduled", out var ds) ? ds.GetInt32() : w.Desired;
                            w.Ready = status.TryGetProperty("numberReady", out var nr) ? nr.GetInt32() : w.Ready;
                            w.Available = status.TryGetProperty("numberAvailable", out var na) ? na.GetInt32() : w.Available;
                            w.Current = status.TryGetProperty("currentNumberScheduled", out var cn) ? cn.GetInt32() : w.Current;
                            w.Updated = status.TryGetProperty("updatedNumberScheduled", out var un) ? un.GetInt32() : w.Updated;
                        }
                    }
                    w.Status = w.Ready >= w.Desired && w.Desired > 0 ? "Healthy" : w.Ready > 0 ? "Degraded" : "Unavailable";
                    list.Add(w);
                }
            }
            catch (Exception ex) { _logger.LogError($"GetWorkloadsInternal({workloadType}) error: {ex.Message}"); }
            return list.OrderBy(w => w.Namespace).ThenBy(w => w.Name).ToList();
        }

        // ── PVC / STORAGE ─────────────────────────────────────────────────────
        public async Task<List<KubePvcModel>> GetPvcsAsync(string environmentName)
        {
            return await Task.Run(() =>
            {
                var pvcs = new List<KubePvcModel>();
                try
                {
                    string kc = ResolveKubeConfigFile(environmentName);
                    if (string.IsNullOrEmpty(kc)) return pvcs;

                    string json = ExecuteKubectl($"get pvc --kubeconfig=\"{kc}\" -A -o json");
                    if (string.IsNullOrEmpty(json)) return pvcs;

                    using var doc = JsonDocument.Parse(json);
                    foreach (var item in doc.RootElement.GetProperty("items").EnumerateArray())
                    {
                        var p = new KubePvcModel();
                        if (item.TryGetProperty("metadata", out var meta))
                        {
                            p.Name = meta.TryGetProperty("name", out var n) ? n.GetString() : "";
                            p.Namespace = meta.TryGetProperty("namespace", out var ns) ? ns.GetString() : "";
                            if (meta.TryGetProperty("creationTimestamp", out var ts) && DateTime.TryParse(ts.GetString(), out var dt))
                                p.AgeDays = (int)(DateTime.UtcNow - dt).TotalDays;
                        }
                        if (item.TryGetProperty("spec", out var spec))
                        {
                            p.StorageClass = spec.TryGetProperty("storageClassName", out var sc) ? sc.GetString() : "";
                            p.VolumeName = spec.TryGetProperty("volumeName", out var vn) ? vn.GetString() : "";
                            if (spec.TryGetProperty("accessModes", out var am) && am.ValueKind == JsonValueKind.Array)
                                p.AccessModes = string.Join(",", am.EnumerateArray().Select(x => x.GetString()));
                        }
                        if (item.TryGetProperty("status", out var status))
                        {
                            p.Status = status.TryGetProperty("phase", out var ph) ? ph.GetString() : "Unknown";
                            if (status.TryGetProperty("capacity", out var cap) && cap.TryGetProperty("storage", out var stor))
                                p.Capacity = stor.GetString() ?? "";
                        }
                        pvcs.Add(p);
                    }
                }
                catch (Exception ex) { _logger.LogError($"GetPvcsAsync error: {ex.Message}"); }
                return pvcs.OrderBy(p => p.Namespace).ThenBy(p => p.Name).ToList();
            });
        }

        // ── ROLLOUT HISTORY ───────────────────────────────────────────────────
        public async Task<List<KubeRolloutRevisionModel>> GetRolloutHistoryAsync(string environmentName, string deploymentName = "")
        {
            return await Task.Run(() =>
            {
                var history = new List<KubeRolloutRevisionModel>();
                try
                {
                    string kc = ResolveKubeConfigFile(environmentName);
                    if (string.IsNullOrEmpty(kc)) return history;

                    // Get all deployment names if not specified
                    var deployNames = new List<string>();
                    if (string.IsNullOrWhiteSpace(deploymentName))
                    {
                        string listOut = ExecuteKubectl($"get deployments --kubeconfig=\"{kc}\" -n znode -o jsonpath='{{.items[*].metadata.name}}'");
                        deployNames = (listOut ?? "").Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    }
                    else deployNames.Add(deploymentName);

                    foreach (var dname in deployNames.Take(20))
                    {
                        string rolloutOut = ExecuteKubectl($"rollout history deployment/{dname} --kubeconfig=\"{kc}\" -n znode");
                        if (string.IsNullOrEmpty(rolloutOut)) continue;

                        string imageOut = "";
                        string replicaJson = ExecuteKubectl($"get replicasets --kubeconfig=\"{kc}\" -n znode -o json -l app={dname}");

                        foreach (var line in rolloutOut.Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(2))
                        {
                            var parts = line.Trim().Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 1 && int.TryParse(parts[0], out var rev))
                            {
                                string changeReason = parts.Length > 1 ? parts[1].Trim() : "<none>";
                                if (changeReason == "<none>") changeReason = "";
                                history.Add(new KubeRolloutRevisionModel
                                {
                                    DeploymentName = dname,
                                    Revision = rev,
                                    ChangeReason = changeReason,
                                    Image = ""
                                });
                            }
                        }
                    }
                    history = history.OrderByDescending(h => h.Revision).ToList();
                }
                catch (Exception ex) { _logger.LogError($"GetRolloutHistoryAsync error: {ex.Message}"); }
                return history;
            });
        }

        public async Task<List<KubeConfigMapModel>> GetConfigMapsAsync(string environmentName)
        {
            return await Task.Run(() =>
            {
                var list = new List<KubeConfigMapModel>();
                try
                {
                    string kc = ResolveKubeConfigFile(environmentName);
                    if (string.IsNullOrEmpty(kc)) return list;
                    var json = ExecuteKubectl($"get configmaps -A --kubeconfig=\"{kc}\" -o json");
                    if (string.IsNullOrWhiteSpace(json)) return list;
                    using var doc = JsonDocument.Parse(json);
                    var items = doc.RootElement.GetProperty("items");
                    foreach (var item in items.EnumerateArray())
                    {
                        try
                        {
                            var meta = item.GetProperty("metadata");
                            var name = meta.TryGetProperty("name", out var n) ? n.GetString() : "";
                            var ns   = meta.TryGetProperty("namespace", out var ns2) ? ns2.GetString() : "";

                            // Skip internal kubernetes system configmaps
                            if (name == "kube-root-ca.crt") continue;

                            var data = new Dictionary<string, string>();
                            if (item.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == JsonValueKind.Object)
                            {
                                foreach (var kv in dataEl.EnumerateObject())
                                    data[kv.Name] = kv.Value.GetString() ?? "";
                            }
                            // Also capture binaryData keys (values as base64 placeholder)
                            if (item.TryGetProperty("binaryData", out var binEl) && binEl.ValueKind == JsonValueKind.Object)
                            {
                                foreach (var kv in binEl.EnumerateObject())
                                    data[kv.Name] = "[binary data]";
                            }

                            int ageDays = 0;
                            if (meta.TryGetProperty("creationTimestamp", out var ts) && ts.ValueKind != JsonValueKind.Null)
                            {
                                if (DateTime.TryParse(ts.GetString(), out var created))
                                    ageDays = (int)(DateTime.UtcNow - created.ToUniversalTime()).TotalDays;
                            }

                            list.Add(new KubeConfigMapModel
                            {
                                Name      = name,
                                Namespace = ns,
                                KeyCount  = data.Count,
                                AgeDays   = ageDays,
                                Data      = data
                            });
                        }
                        catch { /* skip malformed entry */ }
                    }
                }
                catch (Exception ex) { _logger.LogError($"GetConfigMapsAsync error: {ex.Message}"); }
                return list;
            });
        }
    }
}
