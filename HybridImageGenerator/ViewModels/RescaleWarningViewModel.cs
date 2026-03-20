using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DialogHostAvalonia;
using HybridImageGenerator.Models;

namespace HybridImageGenerator.ViewModels;

public partial class RescaleWarningViewModel : ViewModelBase {
    [ObservableProperty]
    private bool _dontShowForThisSize;
    
    [RelayCommand]
    private void Ok() {
        RescaleWarningResponse response = new(DontShowForThisSize, false);
        DialogHost.GetDialogSession("MainDialogHost")?.Close(response);
    }

    [RelayCommand]
    private void Cancel() {
        RescaleWarningResponse response = new(DontShowForThisSize, true);
        DialogHost.GetDialogSession("MainDialogHost")?.Close(response);
    }
}