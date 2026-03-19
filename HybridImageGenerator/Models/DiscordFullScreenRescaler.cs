using System;

namespace HybridImageGenerator.Models;

public class DiscordFullScreenRescaler(int innerWindowWidth, int innerWindowHeight) {
    private const int FirstHorizontalPadding = 24; // _
    private const int SecondHorizontalPadding = 272; // m
    private const int FirstVerticalPadding = 88; // h
    private const int SecondVerticalPadding = 36; // f
    // i have no idea what these paddings are but discord
    // uses them to determine the new size of a picture
    
    public (int rescaledWidth, int rescaledHeight) Rescale(int imageWidth, int imageHeight) {
        int firstWidth = innerWindowWidth - FirstHorizontalPadding * 2;
        int secondWidth = innerWindowWidth - SecondHorizontalPadding * 2;
        int firstHeight = innerWindowHeight - FirstVerticalPadding * 2;
        int secondHeight = innerWindowHeight - SecondVerticalPadding * 2;
        
        (int rescaledWidth, int rescaledHeight) firstRescale = InternalRescale(imageWidth, imageHeight, firstWidth, firstHeight);
        (int rescaledWidth, int rescaledHeight) secondRescale = InternalRescale(imageWidth, imageHeight, secondWidth, secondHeight);
        
        return firstRescale.rescaledWidth > secondRescale.rescaledWidth ? firstRescale : secondRescale;
    }

    private static (int rescaledWidth, int rescaledHeight) InternalRescale(int width, int height, int maxWidth, int maxHeight) {
        if (!NeedRescaling(width, height, maxWidth, maxHeight)) 
            return (width, height);
        
        double rescaleCoefficient = (double)maxWidth / width;
        double rescaledWidth = Math.Round(width * rescaleCoefficient);
        double rescaledHeight = Math.Round(height * rescaleCoefficient);
        
        if (rescaledHeight > maxHeight) {
            rescaleCoefficient = maxHeight / rescaledHeight;
            rescaledWidth = Math.Round(rescaledWidth * rescaleCoefficient);
            rescaledHeight = Math.Round(rescaledHeight * rescaleCoefficient);
        }
        
        return ((int)rescaledWidth, (int)rescaledHeight);
    }
    
    private static bool NeedRescaling(int width, int height, int maxWidth, int maxHeight) 
        => width > maxWidth || height > maxHeight;
}