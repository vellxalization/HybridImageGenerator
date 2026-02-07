using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using HybridImageGenerator.Models.ShaderFactories;
using SkiaSharp;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace HybridImageGenerator.Models;

public class ImageEditor {
    private const int MinSize = 4;
    
    private SKImage? _mainImage;
    private SKImage? _hiddenImage;

    public ShaderToImageConverter? Converter { get; set; }

    public event EventHandler<SKShader?>? MainShaderChanged; 
    public event EventHandler<SKShader?>? HiddenShaderChanged; 
    public event EventHandler<SKShader?>? OutputLowShaderChanged; 
    public event EventHandler<SKShader?>? NegativeShaderChanged; 
    public event EventHandler<SKShader?>? OverlayShaderChanged; 
    public event EventHandler<SKShader?>? StitchShaderChanged; 
    public event EventHandler<SKShader?>? GammaShaderChanged;
    public event EventHandler<Size>? MainScaleChanged;
    public event EventHandler<Size>? HiddenScaleChanged;
    
    private PipelineNode<OutputLowFactory> _outputLowNode;
    private PipelineNode<NegativeFactory> _negativeNode;
    private PipelineNode<OverlayFactory> _overlayNode;
    private PipelineNode<StitchingFactory> _stitchNode;
    private PipelineNode<GammaFactory> _gammaNode;

    private readonly CombinedFactory _combinedFactory = new();
    
    // all images are scaled to fit controls and all controls must be the same size
    private Size _renderSize;

    public byte OutputLow {
        get;
        set {
            field = value;
            _outputLowNode.Factory.OutputLow = value;
            _outputLowNode.SendUpdate();
        }
    }

    public byte Opacity {
        get;
        set {
            field = value;
            _overlayNode.Factory.Opacity = value;
            _overlayNode.SendUpdate();
        }
    }

    public float Gamma {
        get;
        set {
            field = value;
            _gammaNode.Factory.Gamma = value;
            _gammaNode.SendUpdate();
        }
    }

    public ImageEditor() 
    {
        // _converter = converter;
        CreatePipeline();
    }

    private void CreatePipeline() {
        _outputLowNode = new PipelineNode<OutputLowFactory>(new OutputLowFactory());
        _negativeNode = new PipelineNode<NegativeFactory>(new NegativeFactory());
        _overlayNode = new PipelineNode<OverlayFactory>(new OverlayFactory());
        _stitchNode = new PipelineNode<StitchingFactory>(new StitchingFactory());
        _gammaNode = new PipelineNode<GammaFactory>(new GammaFactory());
        
        // ideally we want to dispose old shaders, however it creates a bug when avalonia might reuse old
        // draw operation with disposed shader so we leave it at the mercy of the GC
        _outputLowNode.Link(_negativeNode, (newShader, node) => node.Factory.InputShader = newShader);
        _outputLowNode.Link(_stitchNode, (newShader, node) => node.Factory.OverlayShader = newShader);
        _negativeNode.Link(_overlayNode, (newShader, node) => node.Factory.OverlayShader = newShader);
        _overlayNode.Link(_stitchNode, (newShader, node) => node.Factory.InputShader = newShader);
        _stitchNode.Link(_gammaNode, (newShader, node) => node.Factory.InputShader = newShader);

        MainShaderChanged += (_, newShader) => {
            _overlayNode.Factory.InputShader = newShader;
            _overlayNode.SendUpdate();
        };
        HiddenShaderChanged += (_, newShader) => {
            _outputLowNode.Factory.InputShader = newShader;
            _outputLowNode.SendUpdate();
        };
        _outputLowNode.ShaderUpdated += (_, shader) => OutputLowShaderChanged?.Invoke(this, shader);
        _negativeNode.ShaderUpdated += (_, shader) => NegativeShaderChanged?.Invoke(this, shader);
        _overlayNode.ShaderUpdated += (_, shader) => OverlayShaderChanged?.Invoke(this, shader);
        _stitchNode.ShaderUpdated += (_, shader) => StitchShaderChanged?.Invoke(this, shader);
        _gammaNode.ShaderUpdated += (_, shader) => GammaShaderChanged?.Invoke(this, shader);
    }

    public void SetRenderSize(Size newSize) {
        if (newSize == _renderSize) return;
        
        _renderSize = newSize;
        if (_mainImage is not null) {
            var scaled = ScaleImage(_mainImage, newSize);
            MainScaleChanged?.Invoke(this, scaled.shaderSize);
            MainShaderChanged?.Invoke(this, scaled.shader);
        }
        
        if (_hiddenImage is not null) {
            var scaled = ScaleImage(_hiddenImage, newSize);
            HiddenScaleChanged?.Invoke(this, scaled.shaderSize);
            HiddenShaderChanged?.Invoke(this, scaled.shader);
        }
    }
    
    public bool TrySetMainImage(SKImage image, out string? error) {
        if (!IsValidSkiaObject(image)) 
            throw new SkiaObjectInvalidStateException("Input image is invalid");
        
        if (image.Width < MinSize || image.Height < MinSize) {
            error = $"Image must be at least {MinSize} by {MinSize} pixels";
            return false;
        }

        var scaled = ScaleImage(image, _renderSize);
        
        _mainImage?.Dispose();
        _mainImage = image;

        MainScaleChanged?.Invoke(this, scaled.shaderSize);
        MainShaderChanged?.Invoke(this, scaled.shader);
        
        error = null;
        return true;
    }
    
    public bool TrySetHiddenImage(SKImage image, out string? error) {
        if (!IsValidSkiaObject(image)) 
            throw new SkiaObjectInvalidStateException("Input image is invalid");

        if (image.Width < MinSize || image.Height < MinSize) {
            error = $"Image must be at least {MinSize} by {MinSize} pixels";
            return false;
        }
        
        var scaled = ScaleImage(image, _renderSize);
        
        _hiddenImage?.Dispose();
        _hiddenImage = image;
        
        HiddenScaleChanged?.Invoke(this, scaled.shaderSize);
        HiddenShaderChanged?.Invoke(this, scaled.shader);
        
        error = null;
        return true;
    }
    
    public void RemoveMainImage() {
        if (_mainImage is null) return;
        
        MainShaderChanged?.Invoke(this, null);
        _mainImage.Dispose();
        _mainImage = null;
    }
    
    public void RemoveHiddenImage() {
        if (_hiddenImage is null) return;
        
        HiddenShaderChanged?.Invoke(this, null);
        _hiddenImage.Dispose();
        _hiddenImage = null;
    }
    
    private static (SKShader shader, Size shaderSize) ScaleImage(SKImage image, Size sizeToFit) {
        var scaleMatrix = CalculateScaleMatrix(sizeToFit, image.Width, image.Height);
        var scaledImage = image.ToShader().WithLocalMatrix(scaleMatrix);
        if (!IsValidSkiaObject(scaledImage)) {
            scaledImage?.Dispose();
            throw new SkiaObjectInvalidStateException("Failed to generate shader from main image");
        }
        
        var scaledSize = new Size(image.Width * scaleMatrix.ScaleX, image.Height * scaleMatrix.ScaleY);
        return (scaledImage, scaledSize);
    }
    
    private static SKMatrix CalculateScaleMatrix(Size sizeToFit, int imageWidth, int imageHeight) {
        var widthScale = (float)sizeToFit.Width / imageWidth;
        var heightScale = (float)sizeToFit.Height / imageHeight;
        var scale = Math.Min(widthScale, heightScale); // maintain aspect ratio of the image
        
        var xTranslation = ((float)sizeToFit.Width - (imageWidth * scale)) / 2;
        var yTranslation = ((float)sizeToFit.Height - (imageHeight * scale)) / 2;
        
        return SKMatrix.CreateScaleTranslation(scale, scale, xTranslation, yTranslation);
    }
    
    public async Task<MemoryStream> Save() {
        if (Converter is null)
            throw new ConverterIsMissingException();
        
        if (!IsValidSkiaObject(_mainImage))
            throw new SkiaObjectInvalidStateException("Main image is invalid");
        
        if (!IsValidSkiaObject(_hiddenImage))
            throw new SkiaObjectInvalidStateException("Hidden image is invalid");
        
        var inputShader = _mainImage!.ToShader();
        if (!IsValidSkiaObject(inputShader)) {
            inputShader?.Dispose();
            throw new SkiaObjectInvalidStateException("Failed to generate a shader from main image");
        }
        
        var overlayShader = _hiddenImage!.ToShader();
        if (!IsValidSkiaObject(overlayShader)) {
            overlayShader?.Dispose();
            throw new SkiaObjectInvalidStateException("Failed to generate a shader from hidden image");
        }
        
        _combinedFactory.InputShader?.Dispose();
        _combinedFactory.OverlayShader?.Dispose();
        _combinedFactory.InputShader = inputShader;
        _combinedFactory.OverlayShader = overlayShader;
        _combinedFactory.OutputLow = OutputLow;
        _combinedFactory.Opacity = Opacity;
        
        using var shader = _combinedFactory.GenerateOutputShader();
        var size = new SKRect(0, 0, _mainImage.Width, _mainImage.Height);
        using var data = await Converter.Convert(size, shader!);
        if (!IsValidSkiaObject(data))
            throw new SkiaObjectInvalidStateException("Failed to convert output shader to an image");
        
        var memoryStream = new MemoryStream((int)data!.Size);
        await using var dataStream = data.AsStream();
        await dataStream.CopyToAsync(memoryStream);
        PngPatcher.PatchGamma(memoryStream, Gamma);
        
        return memoryStream;
    }

    private static bool IsValidSkiaObject(SKObject? obj) => obj is not null && obj.Handle != IntPtr.Zero;
    
    private class SkiaObjectInvalidStateException(string message) : Exception(message);

    private class ConverterIsMissingException() : Exception($"{nameof(Converter)} is null");
}