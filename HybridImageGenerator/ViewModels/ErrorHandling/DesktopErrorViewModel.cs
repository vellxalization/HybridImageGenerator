using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using HybridImageGenerator.Models.ErrorHandling;

namespace HybridImageGenerator.ViewModels.ErrorHandling;

public sealed class DesktopErrorViewModel(ErrorDispatcher errorDispatcher) : ErrorViewModel(errorDispatcher, "Terminate app") {
    protected override void Critical() {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            lifetime.Shutdown();
        else
            Environment.Exit(0);
    }
}