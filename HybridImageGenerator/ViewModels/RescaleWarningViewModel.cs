using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DialogHostAvalonia;

namespace HybridImageGenerator.ViewModels;

public partial class RescaleWarningViewModel(int imageWidth, int imageHeight, int maxWidth, int maxHeight) : ViewModelBase {
    private const string ImageTooBigFormat =
        "The selected image ({0} by {1} pixels) exceeds the safe zone limits. Discord will downscale it in full-screen mode, which breaks the hybrid illusion.";
    private const string RescaleFormat = "Consider resizing the image down to {0} by {1} (or lower) pixels before uploading.";
    
    [ObservableProperty]
    private string _rescaleMessage = string.Format(RescaleFormat, maxWidth , maxHeight);
    [ObservableProperty]
    private string _bigImageMessage = string.Format(ImageTooBigFormat, imageWidth, imageHeight);
    
    [RelayCommand]
    private void Ok() => DialogHost.GetDialogSession("MainDialogHost")?.Close(true);

    [RelayCommand]
    private void Cancel() => DialogHost.GetDialogSession("MainDialogHost")?.Close(false);
}