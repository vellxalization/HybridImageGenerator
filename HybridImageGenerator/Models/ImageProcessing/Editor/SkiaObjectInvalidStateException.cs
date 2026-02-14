using System;

namespace HybridImageGenerator.Models.ImageProcessing.Editor;

public class SkiaObjectInvalidStateException(string message) : Exception(message);