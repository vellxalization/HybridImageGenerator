using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DialogHostAvalonia;
using HybridImageGenerator.Models;

namespace HybridImageGenerator.ViewModels;

public partial class RescaleWarningViewModel(int imageWidth, int imageHeight, int maxWidth, int maxHeight) : ViewModelBase {
    
    private const string WarningMessageFormat = "Image ({0}x{1} pixels) will be shrinked down to {2}x{3} pixels";
    
    [ObservableProperty]
    private string _warningMessage 
        = string.Format(WarningMessageFormat, imageWidth, imageHeight, maxWidth, maxHeight);
    [ObservableProperty]
    private bool _dontShowForThisSize;
    
    [RelayCommand]
    private void Ok() => DialogHost.GetDialogSession("MainDialogHost")?.Close(true);

    [RelayCommand]
    private void Cancel() => DialogHost.GetDialogSession("MainDialogHost")?.Close(false);
}