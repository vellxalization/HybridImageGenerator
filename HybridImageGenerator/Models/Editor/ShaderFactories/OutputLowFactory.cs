using SkiaSharp;

namespace HybridImageGenerator.Models.Editor.ShaderFactories;

public class OutputLowFactory(byte startingValue = 0) : ShaderFactory(SkSlShader) {
    public byte OutputLow { get; set; } = startingValue;

    private const string SkSlShader =
        """
        uniform float outputLow;
        uniform shader image;
        
        float4 main(float2 coordinates) {
            float4 pixel = sample(image, coordinates);
            return float4(pixel.rgb * (1.0 - outputLow) + outputLow, pixel.a);
        }
        """;

    public override SKShader? GenerateOutputShader() => InputShader is null
        ? null
        : Effect.ToShader(true, new SKRuntimeEffectUniforms(Effect) { { "outputLow", (float)OutputLow / 255 } },
            new SKRuntimeEffectChildren(Effect) { { "image", InputShader } });
}