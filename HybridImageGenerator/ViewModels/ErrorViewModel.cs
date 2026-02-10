using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HybridImageGenerator.Models;

namespace HybridImageGenerator.ViewModels;

public abstract partial class ErrorViewModel : ViewModelBase {
    [ObservableProperty]
    private string _message = "";
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ShowStackTraceSwitchCommand))]
    private string? _stackTrace;
    [ObservableProperty]
    private string _criticalButtonText;
    [ObservableProperty]
    private bool _stackTraceVisible;
    [ObservableProperty]
    private bool _isVisible;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ContinueCommand))]
    private bool _isFatalError;
    private TaskCompletionSource? _currentErrorTsc;
    
    protected ErrorViewModel(ErrorDispatcher errorDispatcher, string criticalButtonText) {
        _criticalButtonText = criticalButtonText;
        errorDispatcher.ErrorOccured += HandleIncomingError;
    }

    private void HandleIncomingError(object? sender, (ErrorDetails details, TaskCompletionSource tsc) args) {
        IsVisible = true;
        _currentErrorTsc = args.tsc;
        
        var details = args.details;
        IsFatalError = details.IsFatal;
        Message = details.Message;
        StackTrace = details.StackTrace;
    }

    [RelayCommand(CanExecute=nameof(CanShowStackTrace))]
    private void ShowStackTraceSwitch() => StackTraceVisible = !StackTraceVisible;

    private bool CanShowStackTrace() => StackTrace is not null;

    [RelayCommand(CanExecute=nameof(CanContinue))]
    private void Continue() {
        _currentErrorTsc?.SetResult();
        IsVisible = false;
    }

    private bool CanContinue() => !IsFatalError;

    [RelayCommand]
    protected abstract void Critical();
}