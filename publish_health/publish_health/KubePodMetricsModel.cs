using System.IO;

namespace publish_health
{
    public class KubePodMetricsModel
    {
        public string PodName { get; set; }
        public string Cpu { get; set; }
        public string Memory { get; set; }
        public double CpuCores { get; set; }
        public long MemoryMi { get; set; }
    }
}
