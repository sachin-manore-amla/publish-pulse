namespace ZnodePublishUtility.Utilities.Helpers;

public static class LoggerHelper
{
    public static string FormatLogMessage(string level, string message, string? details = null)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var baseMessage = $"[{timestamp}] [{level.ToUpper()}] {message}";
        
        if (!string.IsNullOrEmpty(details))
            baseMessage += $" | Details: {details}";
        
        return baseMessage;
    }

    public static string GetCorrelationId(string prefix = "")
    {
        var id = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
        return string.IsNullOrEmpty(prefix) ? id : $"{prefix}-{id}";
    }
}
