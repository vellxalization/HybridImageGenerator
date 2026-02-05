using SkiaSharp;

namespace HybridImageGenerator.Models.ShaderFactories;

public class CombinedFactory(byte outputLow = 0, byte opacity = 0) : ShaderFactory(SkSlShader) {
    // a shader that combines all the other shaders because otherwise saving would be very clunky
    public SKShader? OverlayShader { get; set; }
    public byte OutputLow { get; set; } = outputLow;
    public byte Opacity { get; set; } = opacity;

    private const string SkSlShader =
        """
        uniform float outputLow;
        uniform float opacity;
        
        uniform shader mainImage;
        uniform shader hiddenImage;
        
        float4 main(float2 coordinates) {
            float4 hiddenPixel = sample(hiddenImage, coordinates);
            float4 outputLowPixel = float4(hiddenPixel.rgb * (1.0 - outputLow) + outputLow, hiddenPixel.a);
            
            float yMod = coordinates.y - 2 * floor(coordinates.y / 2); 
            float xMod = coordinates.x - 2 * floor(coordinates.x / 2);
            if (xMod >= 1 && yMod >= 1) {
                float4 mainPixel = sample(mainImage, coordinates);
                float4 negative = float4(1 - outputLowPixel.rgb, 1);
                
                return mix(mainPixel, negative, opacity);
            } else {
                return outputLowPixel;
            }
        }
        """;
    
    public override SKShader? GenerateOutputShader() => InputShader is null || OverlayShader is null 
        ? null 
        : Effect.ToShader(true, 
            new SKRuntimeEffectUniforms(Effect) { { "outputLow", (float)OutputLow / 255 }, { "opacity", (float)Opacity / 255 }, }, 
            new SKRuntimeEffectChildren(Effect) { { "mainImage", InputShader }, { "hiddenImage", OverlayShader } });
}