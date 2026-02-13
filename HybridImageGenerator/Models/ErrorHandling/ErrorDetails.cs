namespace HybridImageGenerator.Models.ErrorHandling;

public record ErrorDetails(bool IsFatal, string Message, string? StackTrace = null);