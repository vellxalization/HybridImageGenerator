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

    private ImageEditor _imageEditor;
    private ImageFileService _fileService;
    private ErrorDispatcher _errorDispatcher;

    partial void OnOutputLowChanged(byte value) => _imageEditor.OutputLow = value;

    partial void OnOpacityChanged(byte value) => _imageEditor.Opacity = value;

    partial void OnGammaChanged(float value) => _imageEditor.Gamma = value;

    // partial void OnControlsBoundsChanged(Rect value) => _imageEditor.SetRenderSize(value.Size);

    public EditorViewModel(ImageFileService fileService, ImageEditor editor, ErrorDispatcher errorDispatcher) {
        _imageEditor = editor;
        _fileService = fileService;
        _errorDispatcher = errorDispatcher;
    }
    
    [RelayCommand]
    private async Task LoadMainImage() {
        try {
            await using Stream? file = await _fileService.SelectOpenFile();
            if (file is null) return;

            using var memoryStream = new MemoryStream((int)file.Length);
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            var image = SKImage.FromEncodedData(memoryStream);
            if (!_imageEditor.TrySetMainImage(image, out string? error)) {
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
            await using Stream? file = await _fileService.SelectOpenFile();
            if (file is null) return;

            using var memoryStream = new MemoryStream((int)file.Length);
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            var image = SKImage.FromEncodedData(memoryStream);
            if (!_imageEditor.TrySetHiddenImage(image, out string? error)) {
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
            using var patchedImage = await _imageEditor.SaveAsync();
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
        _imageEditor.RemoveMainImage();
    }

    private bool CanRemoveMainImage() => MainShader is not null;
    
    [RelayCommand(CanExecute=nameof(CanRemoveHiddenImage))]
    private void RemoveHiddenImage() {
        _imageEditor.RemoveHiddenImage();
    }
    
    private bool CanRemoveHiddenImage() => HiddenShader is not null;

    [RelayCommand]
    private async Task InitializeEditor() {
        if (_imageEditor.Initialized) return;

        try {
            _imageEditor.Initialize();
        }
        catch (Exception ex) {
            var details = new ErrorDetails(true, ex.Message, ex.StackTrace);
            await _errorDispatcher.Invoke(details);
            return;
        }
        
        _imageEditor.MainShaderChanged += (_, shader) => MainShader = shader;
        _imageEditor.HiddenShaderChanged += (_, shader) => HiddenShader = shader;
        _imageEditor.OutputLowShaderChanged += (_, shader) => OutputLowShader = shader;
        _imageEditor.NegativeShaderChanged += (_, shader) => NegativeShader = shader;
        _imageEditor.OverlayShaderChanged += (_, shader) => OverlayShader = shader;
        _imageEditor.StitchShaderChanged += (_, shader) => StitchShader = shader;
        _imageEditor.GammaShaderChanged += (_, shader) => GammaShader = shader;

        _imageEditor.MainScaleChanged += (_, size) => MainScale = size;
        _imageEditor.HiddenScaleChanged += (_, size) => HiddenScale = size;
    }
}