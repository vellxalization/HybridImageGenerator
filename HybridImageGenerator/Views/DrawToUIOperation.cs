using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace HybridImageGenerator.Views;

public class DrawToUIOperation(SKPaint paint, Rect bounds) : ICustomDrawOperation {
    
    public void Render(ImmediateDrawingContext context) {
        if (paint.Handle == IntPtr.Zero || paint.Shader is null || paint.Shader.Handle == IntPtr.Zero) return;
        
        using var lease = context.PlatformImpl.GetFeature<ISkiaSharpApiLeaseFeature>()?.Lease();
        lease?.SkCanvas.DrawRect(Bounds.ToSKRect(), paint);
    }
    
    public Rect Bounds { get; } = bounds;
    
    public bool HitTest(Point p) => Bounds.Contains(p);
    
    public bool Equals(ICustomDrawOperation? other) => this == other;

    public void Dispose() { }
}