namespace publish_health
{
    public class KubeEnvironmentModel
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public bool IsAvailable { get; set; }

    }
}
