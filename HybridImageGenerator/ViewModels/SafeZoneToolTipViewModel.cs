using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DialogHostAvalonia;

namespace HybridImageGenerator.ViewModels;

public partial class SafeZoneToolTipViewModel : ViewModelBase {
    private const string DefaultSizesFormat =
        "The default width ({0}) and height ({1}) are optimized for the full-screen Discord desktop client on a 1920x1080 (or higher) monitor. These values include a buffer for various OS taskbars.";

    [ObservableProperty]
    private string _defaultSizesMessage = string.Format(DefaultSizesFormat, App.DefaultDiscordPCFullScreenInnerWidth, App.DefaultDiscordPCFullScreenInnerHeight);
    
    [RelayCommand]
    private void Close() {
        DialogHost.GetDialogSession("MainDialogHost")?.Close();
    }
}