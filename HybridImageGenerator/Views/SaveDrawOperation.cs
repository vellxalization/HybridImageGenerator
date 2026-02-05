using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace HybridImageGenerator.Views;

public class SaveDrawOperation(SKRect renderRect, SKShader shader, TaskCompletionSource<SKData?> tsc) : ICustomDrawOperation {
    
    public bool HitTest(Point p) => Bounds.Contains(p);
    
    public void Render(ImmediateDrawingContext context) {
        using var lease = context.PlatformImpl.GetFeature<ISkiaSharpApiLeaseFeature>()?.Lease();
        if (lease is null || shader.Handle == IntPtr.Zero) {
            tsc.SetResult(null);
            return;
        }
        
        using var surface = SKSurface.Create(lease.GrContext, false, new SKImageInfo((int)renderRect.Width, (int)renderRect.Height));
        using var paint = new SKPaint();
        paint.Shader = shader;
        surface.Canvas.DrawRect(renderRect, paint);
        using var snapshot = surface.Snapshot();
        var encoded = snapshot.Encode();
        tsc.SetResult(encoded);
    }
    
    public Rect Bounds { get; } = new Rect(0, 0, 1, 1);

    public bool Equals(ICustomDrawOperation? other) => this == other;
    
    public void Dispose() { }
}