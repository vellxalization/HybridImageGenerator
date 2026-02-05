using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using HybridImageGenerator.Views;
using SkiaSharp;

namespace HybridImageGenerator.Models;

public class ShaderToImageConverter(SaveImageControl control) {
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    
    public async Task<SKData?> Convert(SKRect size, SKShader image) {
        await _semaphore.WaitAsync();
        var tsc = new TaskCompletionSource<SKData?>();
        control.ImageSize = size;
        control.ImageToSave = image;
        control.Tsc = tsc;
        Dispatcher.UIThread.Post(control.InvalidateVisual);
        var result = await tsc.Task;
        _semaphore.Release();
        
        return result;
    }
}