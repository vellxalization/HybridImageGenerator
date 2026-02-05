using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Skia;
using SkiaSharp;

namespace HybridImageGenerator.Views;

public class ShaderImageControl : Control {
    public Size ImageSize { get; set; }

    public SKShader? InputShader {
        get => _shader;
        set {
            _shader = value;
            _paint.Shader = _shader;
        }
    }

    private SKShader? _shader;
    
    private readonly SKPaint _paint = new SKPaint();
    
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