using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Skia;
using SkiaSharp;

namespace HybridImageGenerator.Views.Controls;

public class ShaderImageControl : Control {
    public static readonly DirectProperty<ShaderImageControl, Size> ImageSizeProperty =
        AvaloniaProperty.RegisterDirect<ShaderImageControl, Size>(nameof(ImageSize),
            owner => owner.ImageSize,
            (owner, size) => owner.ImageSize = size);
    
    public Size ImageSize {
        get;
        set => SetAndRaise(ImageSizeProperty, ref field, value);
    }
    
    public static readonly DirectProperty<ShaderImageControl, SKShader?> InputShaderProperty =
        AvaloniaProperty.RegisterDirect<ShaderImageControl, SKShader?>(nameof(InputShader),
            owner => owner.InputShader,
            (owner, shader) => owner.InputShader = shader);

    public SKShader? InputShader {
        get;
        set => SetAndRaise(InputShaderProperty, ref field, value);
    }

    private readonly SKPaint _paint = new();

    static ShaderImageControl() {
        InputShaderProperty.Changed.AddClassHandler<ShaderImageControl>(
            (owner, args) => owner._paint.Shader = args.GetNewValue<SKShader?>());
        AffectsRender<ShaderImageControl>(InputShaderProperty, ImageSizeProperty);
    }
    
    public override void Render(DrawingContext context) {
        if (InputShader is null || InputShader.Handle == IntPtr.Zero) return;
        
        SKRect renderRect = CalculateRenderRect(Bounds, ImageSize);
        DrawToUIOperation drawOperation = new(_paint, renderRect.ToAvaloniaRect());
        context.Custom(drawOperation);
    }
    
    private static SKRect CalculateRenderRect(Rect controlBounds, Size imageSize) {
        SKRect renderRect = new() {
            Location = new SKPoint((float)(controlBounds.Width / 2 - imageSize.Width / 2), (float)(controlBounds.Height / 2 - imageSize.Height / 2)),
            Size = new SKSize((float)imageSize.Width, (float)imageSize.Height)
        };
        
        return renderRect;
    }
}