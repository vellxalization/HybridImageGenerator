using SkiaSharp;

namespace HybridImageGenerator.Models.ShaderFactories;

public class NegativeFactory() : ShaderFactory(SkSlShader) {
    private const string SkSlShader =
        """
        uniform shader image;

        float4 main(float2 coordinates) {
            float4 pixel = sample(image, coordinates);
            
            return float4(1 - pixel.rgb, 1);
        }
        """;
    
    public override SKShader? GenerateOutputShader() => InputShader is null
        ? null
        : Effect.ToShader(true, EmptyUniforms, new SKRuntimeEffectChildren(Effect) { { "image", InputShader } });
}