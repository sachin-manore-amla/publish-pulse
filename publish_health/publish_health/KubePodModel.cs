namespace publish_health
{
    public class KubePodModel
    {
        public string Name { get; set; }
        public string Status { get; set; }
        public string IpAddress { get; set; }
        public int RestartCount { get; set; }
        public int  AgeDays { get; set; }
        public string ApiGroup { get; set; }
        public int ContainerPort { get; set; }

        public string NodeName { get; set; }

        public int TotalContainers { get; set; }
        public string ContainerReason { get; set; }

        public int ReadyContainers { get; set; }
    }
}
