using System;
using System.Threading.Tasks;

namespace HybridImageGenerator.Models.ErrorHandling;

public class ErrorDispatcher {
    public event EventHandler<(ErrorDetails details, TaskCompletionSource src)>? ErrorOccured;

    public Task Invoke(ErrorDetails details) {
        if (ErrorOccured is null) return Task.CompletedTask;
        
        TaskCompletionSource tsc = new();
        ErrorOccured?.Invoke(this, (details, tsc));
        return tsc.Task;
    }
}