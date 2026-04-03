using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DialogHostAvalonia;
using HybridImageGenerator.Models;
using HybridImageGenerator.Models.ImageProcessing;
using HybridImageGenerator.Models.ImageProcessing.Editor;
using HybridImageGenerator.ViewModels.ErrorHandling;
using SkiaSharp;

namespace HybridImageGenerator.ViewModels;

public partial class EditorViewModel(ImageFileService fileService, ImageEditor editor, DiscordImageRescaler rescaler, 
    Func<ErrorDetails, ErrorViewModel> errorVmCreator) : ViewModelBase {
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveImageCommand))]
    [NotifyCanExecuteChangedFor(nameof(RemoveMainImageCommand))]
    private SKShader? _mainShader;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveImageCommand))]
    [NotifyCanExecuteChangedFor(nameof(RemoveHiddenImageCommand))]
    private SKShader? _hiddenShader;
    [ObservableProperty]
    private SKShader? _outputLowShader;
    [ObservableProperty]
    private SKShader? _negativeShader;
    [ObservableProperty]
    private SKShader? _overlayShader;
    [ObservableProperty]
    private SKShader? _stitchShader;
    [ObservableProperty]
    private SKShader? _gammaShader;

    [ObservableProperty]
    private byte _outputLow;
    [ObservableProperty]
    private byte _opacity;
    [ObservableProperty]
    private float _gamma;

    [ObservableProperty]
    private Size _mainSize;
    [ObservableProperty]
    private Size _hiddenSize;

    [ObservableProperty]
    private Size _controlsSize;

    private DiscordImageRescaler _rescaler = rescaler;
    private int? _mainImageWidth;
    private int? _mainImageHeight;
    [ObservableProperty]
    private bool _useSafeZones = true;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyNewSafeZonesCommand))]
    private ushort _innerWidth = (ushort)rescaler.InnerWindowWidth;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyNewSafeZonesCommand))]
    private ushort _innerHeight = (ushort)rescaler.InnerWindowHeight;

    partial void OnOutputLowChanged(byte value) {
        if (editor.Initialized)
            editor.OutputLow = value;
    }

    partial void OnOpacityChanged(byte value) {
        if (editor.Initialized)
            editor.Opacity = value;
    }

    partial void OnGammaChanged(float value) {
        if (editor.Initialized)
            editor.Gamma = value;
    }

    [RelayCommand]
    private async Task LoadMainImage() {
        try {
            await using Stream? file = await fileService.SelectOpenFile();
            if (file is null) return;

            using MemoryStream memoryStream = new((int)file.Length);
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            SKImage? image = SKImage.FromEncodedData(memoryStream);

            if (UseSafeZones) {
                bool continueUploading = await CheckRescaling(_rescaler, image.Width, image.Height);
                if (!continueUploading) {
                    image.Dispose();
                    return;
                }
            }
            
            if (!editor.TrySetMainImage(image, out string? error)) {
                ErrorDetails details = new(false, error!);
                await DialogHost.Show(errorVmCreator(details));
            }

            _mainImageWidth = image.Width;
            _mainImageHeight = image.Height;
        }
        catch (Exception ex) {
            ErrorDetails details = new(false, ex.Message, ex.StackTrace);
            await DialogHost.Show(errorVmCreator(details));
        }
    }

    private static async Task<bool> CheckRescaling(DiscordImageRescaler rescaler, int imageWidth, int imageHeight) {
        (int rescaledWidth, int rescaledHeight) fullScreenRescaled = rescaler.RescaleFullScreen(imageWidth, imageHeight);
        bool imageTooBig = fullScreenRescaled.rescaledWidth != imageWidth || fullScreenRescaled.rescaledHeight != imageHeight;
        bool imageTooSmall = !DiscordImageRescaler.WillPreviewRescale(imageWidth, imageHeight);
        if (!imageTooSmall && !imageTooBig) return true;

        ViewModelBase viewModel = imageTooSmall 
            ? new PreviewRescaleWarningViewModel(imageWidth, imageHeight)
            : new FullScreenRescaleWarningViewModel(imageWidth, imageHeight, fullScreenRescaled.rescaledWidth, fullScreenRescaled.rescaledHeight);
        
        bool agreed = (bool)(await DialogHost.Show(viewModel, "MainDialogHost"))!;
        return agreed;
    }

    [RelayCommand]
    private async Task LoadHiddenImage() {
        try {
            await using Stream? file = await fileService.SelectOpenFile();
            if (file is null) return;

            using MemoryStream memoryStream = new((int)file.Length);
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            SKImage? image = SKImage.FromEncodedData(memoryStream);
            if (!editor.TrySetHiddenImage(image, out string? error)) {
                ErrorDetails details = new(false, error!);
                await DialogHost.Show(errorVmCreator(details));
            }
        }
        catch (Exception ex) {
            bool isFatal = ex is EditorNotInitializedException;
            ErrorDetails details = new(isFatal, ex.Message, ex.StackTrace);
            await DialogHost.Show(errorVmCreator(details));
        }
    }
    
    [RelayCommand(CanExecute=nameof(CanSave))]
    private async Task SaveImage() {
        try {
            using MemoryStream patchedImage = await editor.SaveAsync();
            await using Stream? file = await fileService.SelectSaveFile();
            if (file is null || !file.CanWrite) return;

            patchedImage.Position = 0;
            await patchedImage.CopyToAsync(file);
        }
        catch (Exception ex) {
            ErrorDetails details = new(false, ex.Message, ex.StackTrace);
            await DialogHost.Show(errorVmCreator(details));
        }
    }

    private bool CanSave() => MainShader is not null && HiddenShader is not null;

    [RelayCommand(CanExecute=nameof(CanRemoveMainImage))]
    private void RemoveMainImage() {
        editor.RemoveMainImage();
        _mainImageWidth = null;
        _mainImageHeight = null;
    }

    private bool CanRemoveMainImage() => MainShader is not null;
    
    [RelayCommand(CanExecute=nameof(CanRemoveHiddenImage))]
    private void RemoveHiddenImage() {
        editor.RemoveHiddenImage();
    }
    
    private bool CanRemoveHiddenImage() => HiddenShader is not null;

    [RelayCommand]
    private async Task InitializeEditor() {
        if (editor.Initialized) return;

        try {
            editor.Initialize();
        }
        catch (Exception ex) {
            ErrorDetails details = new(true, ex.Message, ex.StackTrace);
            await DialogHost.Show(errorVmCreator(details));
            return;
        }
        
        editor.MainShaderChanged += (_, shader) => MainShader = shader;
        editor.HiddenShaderChanged += (_, shader) => HiddenShader = shader;
        editor.OutputLowShaderChanged += (_, shader) => OutputLowShader = shader;
        editor.NegativeShaderChanged += (_, shader) => NegativeShader = shader;
        editor.OverlayShaderChanged += (_, shader) => OverlayShader = shader;
        editor.StitchShaderChanged += (_, shader) => StitchShader = shader;
        editor.GammaShaderChanged += (_, shader) => GammaShader = shader;

        editor.MainSizeChanged += (_, size) => MainSize = size;
        editor.HiddenSizeChanged += (_, size) => HiddenSize = size;
        editor.SetRenderSize(ControlsSize);
    }

    [RelayCommand]
    private void UpdateControlsScale(SizeChangedEventArgs args) {
        ControlsSize = args.NewSize;
        if (!editor.Initialized) return;
        
        editor.SetRenderSize(args.NewSize);
    }

    [RelayCommand]
    private async Task ShowSafeZoneToolTip() {
        await DialogHost.Show(new SafeZoneToolTipViewModel(), "MainDialogHost");
    }

    [RelayCommand(CanExecute=nameof(SafeZonesAreDifferent))]
    private async Task ApplyNewSafeZones() {
        if (!UseSafeZones) return;
        if (!SafeZonesAreDifferent()) return;

        DiscordImageRescaler newRescaler = new(InnerWidth, InnerHeight);
        if (_mainImageWidth is null || _mainImageHeight is null) {
            _rescaler = newRescaler;
            return;
        }
        
        bool updateRescaler = await CheckRescaling(newRescaler, _mainImageWidth.Value, _mainImageHeight.Value);
        if (updateRescaler)
            _rescaler = newRescaler;
    }

    private bool SafeZonesAreDifferent() =>
        InnerHeight != _rescaler.InnerWindowHeight || InnerWidth != _rescaler.InnerWindowWidth;

}