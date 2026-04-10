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

public partial class EditorViewModel : ViewModelBase {
    private readonly ImageFileService _fileService;
    private readonly ImageEditor _editor;
    private readonly Func<ErrorDetails, ErrorViewModel> _errorVmCreator;
    private DiscordImageRescaler _rescaler;
    
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
    
    private int? _mainImageWidth;
    private int? _mainImageHeight;
    [ObservableProperty]
    private bool _useSafeZones = true;

    [ObservableProperty] 
    [NotifyCanExecuteChangedFor(nameof(ApplyNewSafeZonesCommand))]
    private ushort _innerWidth;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyNewSafeZonesCommand))]
    private ushort _innerHeight;
    
    // firefox async command issue: https://github.com/AvaloniaUI/Avalonia/issues/11041
    public RelayCommand LoadMainCommand { get; set; }
    public RelayCommand LoadHiddenCommand { get; set; }
    
    public EditorViewModel(ImageFileService fileService, ImageEditor editor, DiscordImageRescaler rescaler,
        Func<ErrorDetails, ErrorViewModel> errorVmCreator) 
    {
        _fileService = fileService;
        _editor = editor;
        _rescaler = rescaler;
        _errorVmCreator = errorVmCreator;

        _innerWidth = (ushort)rescaler.InnerWindowWidth;
        _innerHeight = (ushort)rescaler.InnerWindowHeight;

        LoadMainCommand = new RelayCommand(LoadMainImage);
        LoadHiddenCommand = new RelayCommand(LoadHiddenImage);
    }
    
    partial void OnOutputLowChanged(byte value) {
        if (_editor.Initialized)
            _editor.OutputLow = value;
    }

    partial void OnOpacityChanged(byte value) {
        if (_editor.Initialized)
            _editor.Opacity = value;
    }

    partial void OnGammaChanged(float value) {
        if (_editor.Initialized)
            _editor.Gamma = value;
    }
    
    private async void LoadMainImage() {
        try {
            SKImage? image = await OpenImage();
            if (image is null) return;

            if (UseSafeZones) {
                bool continueUploading = await CheckRescaling(_rescaler, image.Width, image.Height);
                if (!continueUploading) {
                    image.Dispose();
                    return;
                }
            }
            
            if (!_editor.TrySetMainImage(image, out string? error)) {
                ErrorDetails details = new(false, error!);
                await DialogHost.Show(_errorVmCreator(details));
            }

            _mainImageWidth = image.Width;
            _mainImageHeight = image.Height;
        }
        catch (Exception ex) {
            ErrorDetails details = new(false, ex.Message, ex.StackTrace);
            await DialogHost.Show(_errorVmCreator(details));
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
    
    private async void LoadHiddenImage() {
        try {
            SKImage? image = await OpenImage();
            if (image is null) return;
            
            if (!_editor.TrySetHiddenImage(image, out string? error)) {
                ErrorDetails details = new(false, error!);
                await DialogHost.Show(_errorVmCreator(details));
            }
        }
        catch (Exception ex) {
            bool isFatal = ex is EditorNotInitializedException;
            ErrorDetails details = new(isFatal, ex.Message, ex.StackTrace);
            await DialogHost.Show(_errorVmCreator(details));
        }
    }
    
    private async Task<SKImage?> OpenImage() {
        await using Stream? file = await _fileService.SelectOpenFile();
        if (file is null) return null;

        using MemoryStream memoryStream = new((int)file.Length);
        await file.CopyToAsync(memoryStream);
        memoryStream.Position = 0;
        SKImage? image = SKImage.FromEncodedData(memoryStream);
        return image ?? throw new Exception("Failed to open image");
    }
    
    [RelayCommand(CanExecute=nameof(CanSave))]
    private async Task SaveImage() {
        try {
            using MemoryStream patchedImage = await _editor.SaveAsync();
            await using Stream? file = await _fileService.SelectSaveFile();
            if (file is null || !file.CanWrite) return;

            patchedImage.Position = 0;
            await patchedImage.CopyToAsync(file);
        }
        catch (Exception ex) {
            ErrorDetails details = new(false, ex.Message, ex.StackTrace);
            await DialogHost.Show(_errorVmCreator(details));
        }
    }

    private bool CanSave() => MainShader is not null && HiddenShader is not null;

    [RelayCommand(CanExecute=nameof(CanRemoveMainImage))]
    private void RemoveMainImage() {
        _editor.RemoveMainImage();
        _mainImageWidth = null;
        _mainImageHeight = null;
    }

    private bool CanRemoveMainImage() => MainShader is not null;
    
    [RelayCommand(CanExecute=nameof(CanRemoveHiddenImage))]
    private void RemoveHiddenImage() {
        _editor.RemoveHiddenImage();
    }
    
    private bool CanRemoveHiddenImage() => HiddenShader is not null;

    [RelayCommand]
    private async Task InitializeEditor() {
        if (_editor.Initialized) return;

        try {
            _editor.Initialize();
        }
        catch (Exception ex) {
            ErrorDetails details = new(true, ex.Message, ex.StackTrace);
            await DialogHost.Show(_errorVmCreator(details));
            return;
        }
        
        _editor.MainShaderChanged += (_, shader) => MainShader = shader;
        _editor.HiddenShaderChanged += (_, shader) => HiddenShader = shader;
        _editor.OutputLowShaderChanged += (_, shader) => OutputLowShader = shader;
        _editor.NegativeShaderChanged += (_, shader) => NegativeShader = shader;
        _editor.OverlayShaderChanged += (_, shader) => OverlayShader = shader;
        _editor.StitchShaderChanged += (_, shader) => StitchShader = shader;
        _editor.GammaShaderChanged += (_, shader) => GammaShader = shader;

        _editor.MainSizeChanged += (_, size) => MainSize = size;
        _editor.HiddenSizeChanged += (_, size) => HiddenSize = size;
        _editor.SetRenderSize(ControlsSize);
    }

    [RelayCommand]
    private void UpdateControlsScale(SizeChangedEventArgs args) {
        ControlsSize = args.NewSize;
        if (!_editor.Initialized) return;
        
        _editor.SetRenderSize(args.NewSize);
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