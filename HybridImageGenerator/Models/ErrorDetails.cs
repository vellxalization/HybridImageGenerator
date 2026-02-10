namespace HybridImageGenerator.Models;

public record ErrorDetails(bool IsFatal, string Message, string? StackTrace = null);