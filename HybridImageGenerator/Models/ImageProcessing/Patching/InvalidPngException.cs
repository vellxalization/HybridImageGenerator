using System;

namespace HybridImageGenerator.Models.ImageProcessing.Patching;

public class InvalidPngException(string message) : Exception(message);