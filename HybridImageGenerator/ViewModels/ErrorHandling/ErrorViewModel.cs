using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DialogHostAvalonia;
using HybridImageGenerator.Models;

namespace HybridImageGenerator.ViewModels.ErrorHandling;

public abstract partial class ErrorViewModel : ViewModelBase {
    [ObservableProperty]
    private string _message;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ShowStackTraceSwitchCommand))]
    private string? _stackTrace;
    [ObservableProperty]
    private string _criticalButtonText;
    [ObservableProperty]
    private bool _isVisible;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ContinueCommand))]
    private bool _isFatalError;

    [ObservableProperty]
    private double _stackTraceOpacity;
    
    protected ErrorViewModel(string criticalButtonText, ErrorDetails details) {
        _criticalButtonText = criticalButtonText;
        _isFatalError = details.IsFatal;
        _message = details.Message;
        _stackTrace = details.StackTrace is null ? null : PrepareStackTrace(details.StackTrace);
    }

    private static string PrepareStackTrace(string stackTrace) {
        string[] split = stackTrace.Split(Environment.NewLine);
        IEnumerable<string> trimmed = split.Select(line => line.TrimStart(' '));
        return string.Concat(string.Join(Environment.NewLine, trimmed), Environment.NewLine); 
        // append newline for better scroll bar visibility
    }
    
    [RelayCommand(CanExecute = nameof(CanShowStackTrace))]
    private void ShowStackTraceSwitch() {
        StackTraceOpacity = StackTraceOpacity > 0 ? 0 : 1;
    }

    private bool CanShowStackTrace() => StackTrace is not null;

    [RelayCommand(CanExecute=nameof(CanContinue))]
    private void Continue() {
        DialogHost.GetDialogSession("MainDialogHost")?.Close();
    }

    private bool CanContinue() => !IsFatalError;

    [RelayCommand]
    protected abstract void Critical();
}