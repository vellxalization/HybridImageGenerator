using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DialogHostAvalonia;
using HybridImageGenerator.Models;

namespace HybridImageGenerator.ViewModels;

public partial class SafeZoneToolTipViewModel : ViewModelBase {
    private const string DefaultSizesFormat =
        "The default width ({0}) and height ({1}) are optimized for the full-screen Discord desktop client on a 1920x1080 (or higher) monitor. These values include a buffer for various OS taskbars.";

    private const string PreviewRescaleFormat = "Discord also downscales images that are above {0} by {1} pixels for previews.";
    
    [ObservableProperty] 
    private string _defaultSizesMessage
        = string.Format(DefaultSizesFormat, App.DefaultDiscordPCFullScreenInnerWidth, App.DefaultDiscordPCFullScreenInnerHeight);
    
    [ObservableProperty]
    private string _previewRescaleMessage 
        = string.Format(PreviewRescaleFormat, DiscordImageRescaler.PreviewMaxWidth, DiscordImageRescaler.PreviewMaxHeight);
    
    [RelayCommand]
    private void Close() {
        DialogHost.GetDialogSession("MainDialogHost")?.Close();
    }
}