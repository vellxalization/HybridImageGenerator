using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HybridImageGenerator.Models;
using SkiaSharp;

namespace HybridImageGenerator.ViewModels;

public partial class EditorViewModel : ViewModelBase {
    [ObservableProperty]
    private Rect _controlsBounds;
    
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
    
    public ImageEditor ImageEditor { get; init; }
    private ImageFileService _fileService;
    private ErrorDispatcher _errorDispatcher;

    partial void OnOutputLowChanged(byte value) => ImageEditor.OutputLow = value;

    partial void OnOpacityChanged(byte value) => ImageEditor.Opacity = value;

    partial void OnGammaChanged(float value) => ImageEditor.Gamma = value;

    partial void OnControlsBoundsChanged(Rect value) => ImageEditor.SetRenderSize(value.Size);

    public EditorViewModel(ImageFileService fileService, ImageEditor editor, ErrorDispatcher errorDispatcher) {
        ImageEditor = editor;
        _fileService = fileService;
        _errorDispatcher = errorDispatcher;
        
        ImageEditor.MainShaderChanged += (_, shader) => MainShader = shader;
        ImageEditor.HiddenShaderChanged += (_, shader) => HiddenShader = shader;
        ImageEditor.OutputLowShaderChanged += (_, shader) => OutputLowShader = shader;
        ImageEditor.NegativeShaderChanged += (_, shader) => NegativeShader = shader;
        ImageEditor.OverlayShaderChanged += (_, shader) => OverlayShader = shader;
        ImageEditor.StitchShaderChanged += (_, shader) => StitchShader = shader;
        ImageEditor.GammaShaderChanged += (_, shader) => GammaShader = shader;

        ImageEditor.MainScaleChanged += (_, size) => MainScale = size;
        ImageEditor.HiddenScaleChanged += (_, size) => HiddenScale = size;
    }
    
    [RelayCommand]
    private async Task LoadMainImage() {
        try {
            // TODO: i don't like this. I need to rewrite this
            if (!ImageEditor.Initialized)
                ImageEditor.Initialize();
        }
        catch (Exception ex) {
            var details = new ErrorDetails(true, ex.Message, ex.StackTrace);
            await _errorDispatcher.Invoke(details);
            return;
        }
        
        try {
            await using Stream? file = await _fileService.SelectOpenFile();
            if (file is null) return;

            using var memoryStream = new MemoryStream((int)file.Length);
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            var image = SKImage.FromEncodedData(memoryStream);
            if (!ImageEditor.TrySetMainImage(image, out string? error)) {
                var details = new ErrorDetails(false, error!);
                await _errorDispatcher.Invoke(details);
            }
        }
        catch (Exception ex) {
            var details = new ErrorDetails(false, ex.Message, ex.StackTrace);
            await _errorDispatcher.Invoke(details);
        }
    }
    
    [RelayCommand]
    private async Task LoadHiddenImage() {
        try {
            if (!ImageEditor.Initialized)
                ImageEditor.Initialize();
        }
        catch (Exception ex) {
            var details = new ErrorDetails(true, ex.Message, ex.StackTrace);
            await _errorDispatcher.Invoke(details);
            return;
        }
        
        try {
            await using Stream? file = await _fileService.SelectOpenFile();
            if (file is null) return;

            using var memoryStream = new MemoryStream((int)file.Length);
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            var image = SKImage.FromEncodedData(memoryStream);
            if (!ImageEditor.TrySetHiddenImage(image, out string? error)) {
                var details = new ErrorDetails(false, error!);
                await _errorDispatcher.Invoke(details);
            }
        }
        catch (Exception ex) {
            var isFatal = ex is ImageEditor.EditorNotInitialized;
            var details = new ErrorDetails(isFatal, ex.Message, ex.StackTrace);
            await _errorDispatcher.Invoke(details);
        }
    }
    
    [RelayCommand(CanExecute=nameof(CanSave))]
    private async Task SaveImage() {
        try {
            if (!ImageEditor.Initialized)
                ImageEditor.Initialize();
        }
        catch (Exception ex) {
            var details = new ErrorDetails(true, ex.Message, ex.StackTrace);
            await _errorDispatcher.Invoke(details);
            return;
        }
        
        try {
            using var patchedImage = await ImageEditor.Save();
            await using Stream? file = await _fileService.SelectSaveFile();
            if (file is null || !file.CanWrite) return;

            patchedImage.Position = 0;
            await patchedImage.CopyToAsync(file);
        }
        catch (Exception ex) {
            var details = new ErrorDetails(false, ex.Message, ex.StackTrace);
            await _errorDispatcher.Invoke(details);
        }
    }

    private bool CanSave() => MainShader is not null && HiddenShader is not null;

    [RelayCommand(CanExecute=nameof(CanRemoveMainImage))]
    private void RemoveMainImage() {
        ImageEditor.RemoveMainImage();
    }

    private bool CanRemoveMainImage() => MainShader is not null;
    
    [RelayCommand(CanExecute=nameof(CanRemoveHiddenImage))]
    private void RemoveHiddenImage() {
        ImageEditor.RemoveHiddenImage();
    }
    
    private bool CanRemoveHiddenImage() => HiddenShader is not null;
}