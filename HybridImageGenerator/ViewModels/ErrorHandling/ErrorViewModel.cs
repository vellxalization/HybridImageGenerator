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
    private bool _stackTraceVisible;
    [ObservableProperty]
    private bool _isVisible;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ContinueCommand))]
    private bool _isFatalError;
    
    protected ErrorViewModel(string criticalButtonText, ErrorDetails details) {
        _criticalButtonText = criticalButtonText;
        _isFatalError = details.IsFatal;
        _message = details.Message;
        _stackTrace = details.StackTrace;
    }
    
    [RelayCommand(CanExecute=nameof(CanShowStackTrace))]
    private void ShowStackTraceSwitch() => StackTraceVisible = !StackTraceVisible;

    private bool CanShowStackTrace() => StackTrace is not null;

    [RelayCommand(CanExecute=nameof(CanContinue))]
    private void Continue() {
        DialogHost.GetDialogSession("MainDialogHost")?.Close();
    }

    private bool CanContinue() => !IsFatalError;

    [RelayCommand]
    protected abstract void Critical();
}