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
    public ImageEditor ImageEditor { get; init; }

    partial void OnOutputLowChanged(byte value) => ImageEditor.OutputLow = value;

    partial void OnOpacityChanged(byte value) => ImageEditor.Opacity = value;

    partial void OnGammaChanged(float value) => ImageEditor.Gamma = value;

    partial void OnControlsBoundsChanged(Rect value) => ImageEditor.SetRenderSize(value.Size);

    public MainViewModel(ImageFileService fileService, ImageEditor editor) {
        _fileService = fileService;
        ImageEditor = editor;
        
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
        await using Stream? file = await _fileService.SelectOpenFile();
        if (file is null) return;
        
        using var memoryStream = new MemoryStream((int)file.Length);
        await file.CopyToAsync(memoryStream);
        memoryStream.Position = 0;
        var image = SKImage.FromEncodedData(memoryStream);
        ImageEditor.TrySetMainImage(image, out _);
    }
    
    [RelayCommand]
    private async Task LoadHiddenImage() {
        await using Stream? file = await _fileService.SelectOpenFile();
        if (file is null) return;
        
        using var memoryStream = new MemoryStream((int)file.Length);
        await file.CopyToAsync(memoryStream);
        memoryStream.Position = 0;
        var image = SKImage.FromEncodedData(memoryStream);
        ImageEditor.TrySetHiddenImage(image, out _);
    }
    
    [RelayCommand]
    private async Task SaveImage() {
        using var patchedImage = await ImageEditor.Save();
        await using Stream? file = await _fileService.SelectSaveFile();
        if (file is null || !file.CanWrite) return;

        patchedImage.Position = 0;
        await patchedImage.CopyToAsync(file);
    }
}