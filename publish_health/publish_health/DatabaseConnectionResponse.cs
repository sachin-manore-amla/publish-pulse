namespace publish_health
{
    public class DatabaseConnectionResponse
    {
        public string Status { get; set; }
        public string ConnectionString { get; set; }
        public string Message { get; set; }
        public bool IsLocal { get; set; }
    }
}
