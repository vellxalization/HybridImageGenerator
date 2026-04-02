using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using HybridImageGenerator.Models.ImageProcessing.Patching;
using HybridImageGenerator.Models.ImageProcessing.Saving;
using HybridImageGenerator.Models.ImageProcessing.ShaderFactories;
using SkiaSharp;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace HybridImageGenerator.Models.ImageProcessing.Editor;

public class ImageEditor(EditedImageSaver saver) {
    public bool Initialized { get; private set; }
    
    private SKImage? _mainImage;
    private SKImage? _hiddenImage;
    private const int MinSize = 4;
    
    // events for controls
    public event EventHandler<SKShader?>? MainShaderChanged; 
    public event EventHandler<SKShader?>? HiddenShaderChanged; 
    public event EventHandler<SKShader?>? OutputLowShaderChanged; 
    public event EventHandler<SKShader?>? NegativeShaderChanged; 
    public event EventHandler<SKShader?>? OverlayShaderChanged; 
    public event EventHandler<SKShader?>? StitchShaderChanged; 
    public event EventHandler<SKShader?>? GammaShaderChanged;
    public event EventHandler<Size>? MainSizeChanged;
    public event EventHandler<Size>? HiddenSizeChanged;
    private Size _renderSize;
    // all images are scaled to fit controls and all controls must be the same size
    
    private PipelineNode<OutputLowFactory> _previewOutputLowNode; // fully fits the control
    private PipelineNode<NegativeFactory> _previewNegativeNode;  // fully fits the control
    private PipelineNode<OutputLowFactory> _outputLowNode;
    private PipelineNode<NegativeFactory> _negativeNode;
    private PipelineNode<OverlayFactory> _overlayNode;
    private PipelineNode<StitchingFactory> _stitchNode;
    private PipelineNode<GammaFactory> _gammaNode;
    

    public void Initialize() {
        if (Initialized) return;
        
        CreatePipeline();
        Initialized = true;
    }
    
    public byte OutputLow {
        get;
        set {
            EnsureInitialized();
            
            field = value;
            _outputLowNode.Factory.OutputLow = value;
            _outputLowNode.SendUpdate();
            _previewOutputLowNode.Factory.OutputLow = value;
            _previewOutputLowNode.SendUpdate();
        }
    }

    public byte Opacity {
        get;
        set {
            EnsureInitialized();

            field = value;
            _overlayNode.Factory.Opacity = value;
            _overlayNode.SendUpdate();
        }
    }

    public float Gamma {
        get;
        set {
            EnsureInitialized();
            
            field = value;
            _gammaNode.Factory.Gamma = value;
            _gammaNode.SendUpdate();
        }
    }
    
    private void CreatePipeline() {
        _outputLowNode = new PipelineNode<OutputLowFactory>(new OutputLowFactory());
        _negativeNode = new PipelineNode<NegativeFactory>(new NegativeFactory());
        _previewOutputLowNode = new PipelineNode<OutputLowFactory>(new OutputLowFactory());
        _previewNegativeNode = new PipelineNode<NegativeFactory>(new NegativeFactory());
        _overlayNode = new PipelineNode<OverlayFactory>(new OverlayFactory());
        _stitchNode = new PipelineNode<StitchingFactory>(new StitchingFactory());
        _gammaNode = new PipelineNode<GammaFactory>(new GammaFactory());
        
        // ideally we want to dispose old shaders, however it creates a bug when avalonia might reuse old
        // draw operation with disposed shader so we leave it at the mercy of the GC
        _previewOutputLowNode.Link(_previewNegativeNode, (newShader, node) => node.Factory.InputShader = newShader);
        _outputLowNode.Link(_negativeNode, (newShader, node) => node.Factory.InputShader = newShader);
        _outputLowNode.Link(_stitchNode, (newShader, node) => node.Factory.OverlayShader = newShader);
        _negativeNode.Link(_overlayNode, (newShader, node) => node.Factory.OverlayShader = newShader);
        _overlayNode.Link(_stitchNode, (newShader, node) => node.Factory.InputShader = newShader);
        _stitchNode.Link(_gammaNode, (newShader, node) => node.Factory.InputShader = newShader);
        
        _previewOutputLowNode.ShaderUpdated += (_, shader) => OutputLowShaderChanged?.Invoke(this, shader);
        _previewNegativeNode.ShaderUpdated += (_, shader) => NegativeShaderChanged?.Invoke(this, shader);
        _overlayNode.ShaderUpdated += (_, shader) => OverlayShaderChanged?.Invoke(this, shader);
        _stitchNode.ShaderUpdated += (_, shader) => StitchShaderChanged?.Invoke(this, shader);
        _gammaNode.ShaderUpdated += (_, shader) => GammaShaderChanged?.Invoke(this, shader);
    }

    public void SetRenderSize(Size newSize) {
        EnsureInitialized();
        
        if (newSize == _renderSize) return;
        _renderSize = newSize;
        
        if (_mainImage is not null)
            UpdateMain(_mainImage);
        
        if (_hiddenImage is not null)
            UpdateHidden(_hiddenImage);
    }
    
    public bool TrySetMainImage(SKImage image, out string? error) {
        EnsureInitialized();
        
        if (!IsValidSkiaObject(image)) 
            throw new SkiaObjectInvalidStateException("Input image is invalid");
        
        if (image.Width < MinSize || image.Height < MinSize) {
            error = $"Image must be at least {MinSize} by {MinSize} pixels";
            return false;
        }
        
        _mainImage?.Dispose();
        _mainImage = image;
        UpdateMain(image);
        
        error = null;
        return true;
    }

    private void UpdateMain(SKImage image) {
        (SKShader shader, Size shaderSize) scaled = ScaleImageToControl(image, _renderSize);
        MainSizeChanged?.Invoke(this, scaled.shaderSize);
        MainShaderChanged?.Invoke(this, scaled.shader);
        _overlayNode.Factory.InputShader = scaled.shader;
        _overlayNode.SendUpdate();
        if (_hiddenImage is not null) // rescale hidden
            UpdateHidden(_hiddenImage);
    }
    
    public bool TrySetHiddenImage(SKImage image, out string? error) {
        EnsureInitialized();
        
        if (!IsValidSkiaObject(image)) 
            throw new SkiaObjectInvalidStateException("Input image is invalid");

        if (image.Width < MinSize || image.Height < MinSize) {
            error = $"Image must be at least {MinSize} by {MinSize} pixels";
            return false;
        }
        
        _hiddenImage?.Dispose();
        _hiddenImage = image;
        UpdateHidden(image);

        error = null;
        return true;
    }

    private void UpdateHidden(SKImage image) {
        UpdateHiddenOnlyPreviews(image);
        UpdateMainPipeline(image);
    }

    private void UpdateHiddenOnlyPreviews(SKImage image) {
        (SKShader shader, Size shaderSize) scaled = ScaleImageToControl(image, _renderSize);
        HiddenSizeChanged?.Invoke(this, scaled.shaderSize);
        HiddenShaderChanged?.Invoke(this, scaled.shader);
        _previewOutputLowNode.Factory.InputShader = scaled.shader;
        _previewOutputLowNode.SendUpdate();
    }

    private void UpdateMainPipeline(SKImage image) {
        if (_mainImage is null) return;

        SKShader shader = ScaleImageRelativeToMain(image);
        _outputLowNode.Factory.InputShader = shader;
        _outputLowNode.SendUpdate();
    }
    
    public void RemoveMainImage() {
        EnsureInitialized();
        
        if (_mainImage is null) return;
        
        MainShaderChanged?.Invoke(this, null);
        MainSizeChanged?.Invoke(this, new Size(0, 0));
        _mainImage.Dispose();
        _mainImage = null;
        _overlayNode.Factory.InputShader = null;
        _overlayNode.SendUpdate();
    }
    
    public void RemoveHiddenImage() {
        EnsureInitialized();
        
        if (_hiddenImage is null) return;
        
        HiddenShaderChanged?.Invoke(this, null);
        HiddenSizeChanged?.Invoke(this, new Size(0, 0));
        _hiddenImage.Dispose();
        _hiddenImage = null;
        _previewOutputLowNode.Factory.InputShader = null;
        _previewOutputLowNode.SendUpdate();
        _outputLowNode.Factory.InputShader = null;
        _outputLowNode.SendUpdate();
    }

    private SKShader ScaleImageRelativeToMain(SKImage image) {
        SKMatrix scaleMatrix = CalculateScaleMatrix(_renderSize, _mainImage!.Width, _mainImage.Height); 
        SKShader? scaledImage = image.ToShader(SKShaderTileMode.Decal, SKShaderTileMode.Decal).WithLocalMatrix(scaleMatrix);
        if (!IsValidSkiaObject(scaledImage)) {
            scaledImage?.Dispose();
            throw new SkiaObjectInvalidStateException("Failed to generate shader from main image");
        }

        return scaledImage;
    }
    
    private static (SKShader shader, Size shaderSize) ScaleImageToControl(SKImage image, Size sizeToFit) {
        SKMatrix scaleMatrix = CalculateScaleMatrix(sizeToFit, image.Width, image.Height);
        SKShader? scaledImage = image.ToShader().WithLocalMatrix(scaleMatrix);
        if (!IsValidSkiaObject(scaledImage)) {
            scaledImage?.Dispose();
            throw new SkiaObjectInvalidStateException("Failed to generate shader from main image");
        }
        
        Size scaledSize = new(image.Width * scaleMatrix.ScaleX, image.Height * scaleMatrix.ScaleY);
        return (scaledImage, scaledSize);
    }
    
    private static SKMatrix CalculateScaleMatrix(Size sizeToFit, int imageWidth, int imageHeight) {
        float widthScale = (float)sizeToFit.Width / imageWidth;
        float heightScale = (float)sizeToFit.Height / imageHeight;
        float scale = Math.Min(widthScale, heightScale); // maintain aspect ratio of the image
        
        float xTranslation = ((float)sizeToFit.Width - (imageWidth * scale)) / 2;
        float yTranslation = ((float)sizeToFit.Height - (imageHeight * scale)) / 2;
        
        return SKMatrix.CreateScaleTranslation(scale, scale, xTranslation, yTranslation);
    }
    
    public async Task<MemoryStream> SaveAsync() {
        EnsureInitialized();
        
        if (!IsValidSkiaObject(_mainImage))
            throw new SkiaObjectInvalidStateException("Main image is invalid");
        
        if (!IsValidSkiaObject(_hiddenImage))
            throw new SkiaObjectInvalidStateException("Hidden image is invalid");

        using SKBitmap mainBitmap = saver.ConvertImageToUnpremulRgba8888Bitmap(_mainImage!);
        using SKBitmap hiddenBitmap = saver.ConvertImageToUnpremulRgba8888Bitmap(_hiddenImage!);
        using SKBitmap saved = await saver.ApplyEffectsAndSaveAsync(mainBitmap, hiddenBitmap, OutputLow, Opacity);
        using SKData? data = saved.Encode(SKEncodedImageFormat.Png, 100);
        
        if (!IsValidSkiaObject(data))
            throw new SkiaObjectInvalidStateException("Failed to save final image");
        
        MemoryStream memoryStream = new((int)data!.Size);
        await using Stream? dataStream = data.AsStream();
        await dataStream.CopyToAsync(memoryStream);
        PngPatcher.PatchGamma(memoryStream, Gamma);
        
        return memoryStream;
    }

    private static bool IsValidSkiaObject(SKObject? obj) => obj is not null && obj.Handle != IntPtr.Zero;

    private void EnsureInitialized() {
        if (!Initialized)
            throw new EditorNotInitializedException();
    }
}