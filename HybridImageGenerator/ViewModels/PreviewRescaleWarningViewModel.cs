using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DialogHostAvalonia;
using HybridImageGenerator.Models;

namespace HybridImageGenerator.ViewModels;

public partial class PreviewRescaleWarningViewModel(int imageWidth, int imageHeight) : ViewModelBase {
    private const string ImageTooSmallFormat =
        "The selected image ({0} by {1} pixels) is too small. Discord won't downscale it in preview, which breaks the hybrid illusion.";
    private const string RescaleFormat = "Consider resizing the image up to {0} by {1} (or higher) pixels before uploading.";

    [ObservableProperty]
    private string _rescaleMessage 
        = string.Format(RescaleFormat, DiscordImageRescaler.PreviewMaxWidth + 1, DiscordImageRescaler.PreviewMaxHeight + 1);

    [ObservableProperty] 
    private string _imageTooSmallMessage =
        string.Format(ImageTooSmallFormat, imageWidth, imageHeight);
    
    [RelayCommand] 
    private void Ok() => DialogHost.GetDialogSession("MainDialogHost")?.Close(true);

    [RelayCommand]
    private void Cancel() => DialogHost.GetDialogSession("MainDialogHost")?.Close(false);
}