using System.Runtime.InteropServices.JavaScript;
using HybridImageGenerator.Models.ErrorHandling;

namespace HybridImageGenerator.ViewModels.ErrorHandling;

public sealed partial class WebErrorViewModel(ErrorDispatcher errorDispatcher) : ErrorViewModel(errorDispatcher, "Reload app") {
    [JSImport("globalThis.location.reload")]
    private static partial void ReloadPage();
    
    protected override void Critical() {
        ReloadPage();
    }
}