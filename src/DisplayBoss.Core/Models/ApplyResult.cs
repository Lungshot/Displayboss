namespace DisplayBoss.Core.Models;

public class ApplyResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int MatchedMonitors { get; set; }
    public List<string> MissingMonitors { get; set; } = new();
    public List<string> SkippedMonitors { get; set; } = new();
    public int ErrorCode { get; set; }

    public static ApplyResult Succeeded(int matched, List<string>? missing = null)
    {
        return new ApplyResult
        {
            Success = true,
            Message = missing?.Count > 0
                ? $"Profile applied ({matched} monitors matched, {missing.Count} missing)"
                : $"Profile applied successfully ({matched} monitors)",
            MatchedMonitors = matched,
            MissingMonitors = missing ?? new()
        };
    }

    public static ApplyResult Failed(string message, int errorCode = 0)
    {
        return new ApplyResult
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode
        };
    }

    public static ApplyResult ValidationFailed(string message, int errorCode = 0)
    {
        return new ApplyResult
        {
            Success = false,
            Message = $"Validation failed: {message}",
            ErrorCode = errorCode
        };
    }
}
