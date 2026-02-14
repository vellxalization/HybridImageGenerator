using System;

namespace HybridImageGenerator.Models.ImageProcessing.Saving;

public class InvalidImageFormatException(string message) : Exception(message);