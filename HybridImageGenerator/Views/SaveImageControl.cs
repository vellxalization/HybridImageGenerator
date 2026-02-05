using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using SkiaSharp;

namespace HybridImageGenerator.Views;

public class SaveImageControl : Control {
    public SKRect ImageSize { get; set; }
    public SKShader? ImageToSave { get; set; }
    public TaskCompletionSource<SKData?>? Tsc { get; set; }

    public override void Render(DrawingContext context) {
        if (ImageToSave is null || ImageToSave.Handle == IntPtr.Zero || Tsc is null || Tsc.Task.IsCompleted) return;

        var saveOperation = new SaveDrawOperation(ImageSize, ImageToSave, Tsc);
        context.Custom(saveOperation);
    }
}