using SkiaSharp;

namespace HybridImageGenerator.Models.ImageProcessing.ShaderFactories;

public class OverlayFactory(byte startingValue = 0) : ShaderFactory(SkSlShader) {
    public byte Opacity { get; set; } = startingValue;

    public SKShader? OverlayShader { get; set; }

    private const string SkSlShader =
        """
        uniform float overlayImageOpacity;
        uniform shader baseImage;
        uniform shader overlayImage;
        
        float4 main(float2 coordinates) {
            float4 baseColor = sample(baseImage, coordinates);
            float4 overlayColor = sample(overlayImage, coordinates);
            
            return mix(baseColor, overlayColor, overlayImageOpacity);
        }
        """;

    public override SKShader? GenerateOutputShader() => InputShader is null || OverlayShader is null 
        ? null 
        : Effect.ToShader(true, new SKRuntimeEffectUniforms(Effect){ { "overlayImageOpacity", (float)Opacity / 255 } },
            new SKRuntimeEffectChildren(Effect) { { "baseImage", InputShader }, {"overlayImage", OverlayShader } });
}