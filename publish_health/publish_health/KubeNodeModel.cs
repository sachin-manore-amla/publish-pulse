namespace publish_health
{
    public class KubeNodeModel
    {
        public string Name { get; set; }
        public int AgeDays { get; set; }
        public string Roles { get; set; }
        public string KernelVersion { get; set; }
        public string OsImage { get; set; }
        public string ContainerRuntime { get; set; }
        public string Architecture { get; set; }
        public int ReadyContainers { get; set; }
        public string CpuCapacity { get; set; }
        public string MemoryCapacity { get; set; }
        public bool IsReady { get; set; }
        public string Status { get; set; }
        public bool MemoryPressure { get; set; }
        public bool DiskPressure { get; set; }
        public bool PidPressure { get; set; }
        public List<string> Conditions { get; set; } = new();
        public int PodCount { get; set; }
        public string CpuUsage { get; set; }
        public string MemoryUsage { get; set; }
        public double CpuUsagePct { get; set; }
        public double MemoryUsagePct { get; set; }
    }
}
