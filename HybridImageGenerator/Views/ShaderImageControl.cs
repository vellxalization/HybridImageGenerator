using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Skia;
using SkiaSharp;

namespace HybridImageGenerator.Views;

public class ShaderImageControl : Control {
    public static readonly StyledProperty<Size> ImageSizeProperty =
        AvaloniaProperty.Register<ShaderImageControl, Size>(nameof(ImageSize));

    public Size ImageSize {
        get => GetValue(ImageSizeProperty);
        set => SetValue(ImageSizeProperty, value);
    }
    
    public static readonly StyledProperty<SKShader?> InputShaderProperty =
        AvaloniaProperty.Register<ShaderImageControl, SKShader?>(nameof(InputShader));

    public SKShader? InputShader {
        get => GetValue(InputShaderProperty);
        set => SetValue(InputShaderProperty, value);
    }
    
    private readonly SKPaint _paint = new SKPaint();

    static ShaderImageControl() {
        InputShaderProperty.Changed.AddClassHandler<ShaderImageControl>(
            (x, e) => x._paint.Shader = e.GetNewValue<SKShader?>());
        AffectsRender<ShaderImageControl>(InputShaderProperty);
        AffectsRender<ShaderImageControl>(ImageSizeProperty);
    }
    
    public override void Render(DrawingContext context) {
        if (InputShader is null || InputShader.Handle == IntPtr.Zero) return;

        var renderRect = CalculateRenderRect(Bounds, ImageSize);
        var drawOperation = new DrawToUIOperation(_paint, renderRect.ToAvaloniaRect());
        context.Custom(drawOperation);
    }
    
    private static SKRect CalculateRenderRect(Rect controlBounds, Size imageSize) {
        var renderRect = new SKRect() {
            Location = new SKPoint((float)(controlBounds.Width / 2 - imageSize.Width / 2), (float)(controlBounds.Height / 2 - imageSize.Height / 2)),
            Size = new SKSize((float)imageSize.Width, (float)imageSize.Height)
        };
        
        return renderRect;
    }
}