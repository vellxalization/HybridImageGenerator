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

public partial class EditorViewModel(ImageFileService fileService, ImageEditor editor, DiscordFullScreenRescaler rescaler, 
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
    private Size _mainScale;
    [ObservableProperty]
    private Size _hiddenScale;

    [ObservableProperty]
    private Size _controlsSize;

    private bool _checkRescale = true;

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

            if (!(await CheckRescaling(image.Width, image.Height))) {
                image.Dispose();
                return;
            }
            
            if (!editor.TrySetMainImage(image, out string? error)) {
                ErrorDetails details = new(false, error!);
                await DialogHost.Show(errorVmCreator(details));
            }
        }
        catch (Exception ex) {
            ErrorDetails details = new(false, ex.Message, ex.StackTrace);
            await DialogHost.Show(errorVmCreator(details));
        }
    }

    private async Task<bool> CheckRescaling(int imageWidth, int imageHeight) {
        if (!_checkRescale)
            return true;
        
        (int rescaledWidth, int rescaledHeight) rescaled = rescaler.Rescale(imageWidth, imageHeight);
        if (rescaled == (imageWidth, imageHeight))
            return true;
        
        RescaleWarningViewModel viewModel = new RescaleWarningViewModel(imageWidth, imageHeight, rescaled.rescaledWidth, rescaled.rescaledHeight);
        RescaleWarningResponse response = (RescaleWarningResponse)(await DialogHost.Show(viewModel))!;
        _checkRescale = !response.DontShowForThisSize;
        
        return !response.LoadingCancelled;
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

        editor.MainScaleChanged += (_, size) => MainScale = size;
        editor.HiddenScaleChanged += (_, size) => HiddenScale = size;
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
}