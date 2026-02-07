using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HybridImageGenerator.Models;
using SkiaSharp;

namespace HybridImageGenerator.ViewModels;

public partial class MainViewModel : ViewModelBase {
    [ObservableProperty]
    private Rect _controlsBounds;
    
    [ObservableProperty]
    private SKShader? _mainShader;
    [ObservableProperty]
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
    
    private ImageFileService _fileService;
    private ImageEditor _imageEditor;

    partial void OnOutputLowChanged(byte value) => _imageEditor.OutputLow = value;

    partial void OnOpacityChanged(byte value) => _imageEditor.Opacity = value;

    partial void OnGammaChanged(float value) => _imageEditor.Gamma = value;

    partial void OnControlsBoundsChanged(Rect value) => _imageEditor.SetRenderSize(value.Size);

    public MainViewModel(ImageFileService fileService, ImageEditor editor) {
        _fileService = fileService;
        _imageEditor = editor;
        
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
    
    [RelayCommand]
    private async Task LoadMainImage() {
        await using Stream? file = await _fileService.SelectOpenFile();
        if (file is null) return;
        
        using var memoryStream = new MemoryStream((int)file.Length);
        await file.CopyToAsync(memoryStream);
        memoryStream.Position = 0;
        var image = SKImage.FromEncodedData(memoryStream);
        _imageEditor.TrySetMainImage(image, out _);
    }
    
    [RelayCommand]
    private async Task LoadHiddenImage() {
        await using Stream? file = await _fileService.SelectOpenFile();
        if (file is null) return;
        
        using var memoryStream = new MemoryStream((int)file.Length);
        await file.CopyToAsync(memoryStream);
        memoryStream.Position = 0;
        var image = SKImage.FromEncodedData(memoryStream);
        _imageEditor.TrySetHiddenImage(image, out _);
    }
    
    [RelayCommand]
    private async Task SaveImage() {
        await using Stream? file = await _fileService.SelectSaveFile();
        if (file is null) return;
        
        Console.WriteLine(file.Length);
    }
}