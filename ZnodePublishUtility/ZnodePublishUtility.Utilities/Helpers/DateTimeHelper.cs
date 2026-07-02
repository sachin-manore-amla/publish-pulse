namespace ZnodePublishUtility.Utilities.Helpers;

public static class DateTimeHelper
{
    public static string FormatDuration(DateTime start, DateTime? end)
    {
        if (end == null)
            return "In Progress";

        var duration = end.Value - start;
        
        if (duration.TotalSeconds < 1)
            return "< 1s";
        
        if (duration.TotalMinutes < 1)
            return $"{(int)duration.TotalSeconds}s";
        
        if (duration.TotalHours < 1)
            return $"{(int)duration.TotalMinutes}m {duration.Seconds}s";
        
        return $"{(int)duration.TotalHours}h {duration.Minutes}m {duration.Seconds}s";
    }

    public static string FormatEstimatedTime(int remainingItems, int itemsPerSecond)
    {
        if (itemsPerSecond <= 0)
            return "Unknown";

        var seconds = remainingItems / itemsPerSecond;
        
        if (seconds < 60)
            return $"{seconds}s";
        
        var minutes = seconds / 60;
        return $"{minutes}m";
    }
}
