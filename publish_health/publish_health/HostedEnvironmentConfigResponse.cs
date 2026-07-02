namespace publish_health
{
    public class HostedEnvironmentConfigResponse
    {
        public string Status { get; set; }
        public string ZnodeApiGateway { get; set; }
        public string Message { get; set; }
        public bool IsFrontendOnly { get; set; }
        public string ApiDomainName { get; set; }
        public string ApiDomainKey { get; set; }
        public Dictionary<string,string> HostedAppSettings { get; set; }
        public string ConnectionString { get; set; }
    }
}
