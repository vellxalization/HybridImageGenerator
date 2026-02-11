using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using SkiaSharp;

namespace HybridImageGenerator.Models;

public class EditedImageSaver {
    private const int UnpremulRgba8888BytesPerPixel = 4;
    
    public SKBitmap Save(SKBitmap mainImage, SKBitmap hiddenImage, byte outputLow, byte opacity) {
        if (!IsValidBitmap(mainImage))
            throw new InvalidImageFormatException("Main image must have unpremultiplied alpha and RGBA8888 color type");
        
        if (!IsValidBitmap(hiddenImage))
            throw new InvalidImageFormatException("Hidden image must have unpremultiplied alpha and RGBA8888 color type");
        
        var info = new SKImageInfo(mainImage.Width, mainImage.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        var result = new SKBitmap(info);
        
        mainImage.CopyTo(result);
        ProcessImages(result, hiddenImage, outputLow, (float)opacity / 255);
        return result;
    }

    private static unsafe void ProcessImages(SKBitmap mainImage, SKBitmap hiddenImage, byte outputLow, float opacity) {
        int mainBytesPerRow = mainImage.RowBytes;
        int hiddenBytesPerRow = hiddenImage.RowBytes;
        int usefulBytesPerRow = Math.Min(mainImage.RowBytes, hiddenImage.RowBytes);

        Parallel.For(0, mainImage.Height, y => {
            if (y >= hiddenImage.Height) return;

            int mainRowOffset = y * mainBytesPerRow;
            int hiddenRowOffset = y * hiddenBytesPerRow;

            Span<byte> mainSpan = new Span<byte>(mainImage.GetPixels().ToPointer(), mainImage.ByteCount);
            ReadOnlySpan<byte> hiddenSpan = hiddenImage.GetPixelSpan();

            Span<byte> mainRow = mainSpan.Slice(mainRowOffset, usefulBytesPerRow);
            ReadOnlySpan<byte> hiddenRow = hiddenSpan.Slice(hiddenRowOffset, usefulBytesPerRow);

            if (y % 2 == 0)
                ProcessNonAlternatingRow(mainRow, hiddenRow, outputLow, opacity);
            else
                ProcessAlternatingRow(mainRow, hiddenRow, outputLow, opacity);
        });
    }
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ProcessAlternatingRow(Span<byte> mainRow, ReadOnlySpan<byte> hiddenRow, byte outputLow, float opacity) 
    {
        Span<byte> buffer = stackalloc byte[UnpremulRgba8888BytesPerPixel];
        bool alternative = true; // use blend 
        for (int x = 0; x < mainRow.Length; x += UnpremulRgba8888BytesPerPixel) {
            Span<byte> mainPixel = mainRow.Slice(x, UnpremulRgba8888BytesPerPixel);
            ReadOnlySpan<byte> hiddenPixel = hiddenRow.Slice(x, UnpremulRgba8888BytesPerPixel);
            
            hiddenPixel.CopyTo(buffer);
            ApplyOutputLow(buffer, outputLow);
            
            if (alternative) {
                ApplyNegative(buffer);
                ApplyBlend(mainPixel, buffer, opacity);
            }
            else {
                buffer.CopyTo(mainPixel);
            }
            
            alternative = !alternative;
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ProcessNonAlternatingRow(Span<byte> mainRow, ReadOnlySpan<byte> hiddenRow, byte outputLow, float opacity) 
    {
        Span<byte> buffer = stackalloc byte[UnpremulRgba8888BytesPerPixel];
        for (int x = 0; x < mainRow.Length; x += UnpremulRgba8888BytesPerPixel) {
            Span<byte> mainPixel = mainRow.Slice(x, UnpremulRgba8888BytesPerPixel);
            ReadOnlySpan<byte> hiddenPixel = hiddenRow.Slice(x, UnpremulRgba8888BytesPerPixel);
            
            hiddenPixel.CopyTo(buffer);
            ApplyOutputLow(buffer, outputLow);
            ApplyNegative(buffer);
            ApplyBlend(mainPixel, buffer, opacity);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ApplyBlend(Span<byte> bottomLayer, Span<byte> topLayer, float opacity) 
    {
        float opacityComplement = 1 - opacity;
        bottomLayer[0] = (byte)(topLayer[0] * opacity + bottomLayer[0] * opacityComplement);
        bottomLayer[1] = (byte)(topLayer[1] * opacity + bottomLayer[1] * opacityComplement);
        bottomLayer[2] = (byte)(topLayer[2] * opacity + bottomLayer[2] * opacityComplement);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ApplyOutputLow(Span<byte> pixel, byte outputLow) {
        pixel[0] = Math.Clamp(pixel[0], outputLow, byte.MaxValue);
        pixel[1] = Math.Clamp(pixel[1], outputLow, byte.MaxValue);
        pixel[2] = Math.Clamp(pixel[2], outputLow, byte.MaxValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ApplyNegative(Span<byte> pixel) {
        // byte - byte = int
        // i hate C# sometimes
        pixel[0] = (byte)(byte.MaxValue - pixel[0]);
        pixel[1] = (byte)(byte.MaxValue - pixel[1]);
        pixel[2] = (byte)(byte.MaxValue - pixel[2]);
    }
    
    public SKBitmap ConvertImageToUnpremulRgba8888Bitmap(SKImage image) {
        if (IsValidImage(image))
            return SKBitmap.FromImage(image);
        
        var info = new SKImageInfo(image.Width, image.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        var bitmap = new SKBitmap(info);
        image.ReadPixels(info, bitmap.GetPixels());
        
        return bitmap;
    } 
    
    private static bool IsValidBitmap(SKBitmap bitmap) => 
        bitmap is { AlphaType: SKAlphaType.Unpremul, ColorType: SKColorType.Rgba8888 };
    
    private static bool IsValidImage(SKImage image) => 
        image is { AlphaType: SKAlphaType.Unpremul, ColorType: SKColorType.Rgba8888 };
}

public class InvalidImageFormatException(string message) : Exception(message);