using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using HybridImageGenerator.Models;

namespace HybridImageGenerator.ViewModels.ErrorHandling;

public sealed class DesktopErrorViewModel(ErrorDetails details) : ErrorViewModel("Terminate app", details) {
    protected override void Critical() {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            lifetime.Shutdown();
        else
            Environment.Exit(0);
    }
}