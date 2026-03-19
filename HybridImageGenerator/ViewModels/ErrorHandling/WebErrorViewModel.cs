using System.Runtime.InteropServices.JavaScript;
using HybridImageGenerator.Models;

namespace HybridImageGenerator.ViewModels.ErrorHandling;

public sealed partial class WebErrorViewModel(ErrorDetails details) : ErrorViewModel("Reload app", details) {
    [JSImport("globalThis.location.reload")]
    private static partial void ReloadPage();
    
    protected override void Critical() {
        ReloadPage();
    }
}