using SkiaSharp;

namespace HybridImageGenerator.Models.ShaderFactories;

public class StitchingFactory() : ShaderFactory(SkSlShader) {
    public SKShader? OverlayShader { get; set; }

    private const string SkSlShader =
        """
        uniform shader baseImage;
        uniform shader hiddenImage;
        
        float4 main(float2 coordinates) {
            float yMod = coordinates.y - 2 * floor(coordinates.y / 2); 
            if (yMod >= 1)
                return sample(baseImage, coordinates);
                
            float xMod = coordinates.x - 2 * floor(coordinates.x / 2);
            if (xMod >= 1)
                return sample(baseImage, coordinates);
            
            return sample(hiddenImage, coordinates);
        }
        """;

    public override SKShader? GenerateOutputShader() => InputShader is null || OverlayShader is null 
        ? null 
        : Effect.ToShader(true, EmptyUniforms,
            new SKRuntimeEffectChildren(Effect) { { "baseImage", InputShader }, {"hiddenImage", OverlayShader } });
}