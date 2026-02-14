using System;
using SkiaSharp;

namespace HybridImageGenerator.Models.ImageProcessing.ShaderFactories;

public class GammaFactory : ShaderFactory {
    public float Gamma { 
        get => _gamma;
        set {
            if (value is < 0 or > (float)2.2)
                throw new ArgumentOutOfRangeException(nameof(Gamma), "Value must be between 0 and 2.2");
            
            _gamma = value;
        } 
    }
    private float _gamma;

    public GammaFactory(float startingValue = 0) : base(SkSlShader) => Gamma = startingValue;

    private const string SkSlShader =
        """
        uniform float gamma;
        uniform shader image;
        
        float4 main(float2 coordinates) {
            float exponent = 1.0 / (gamma * 2.2);
            float4 pixel = sample(image, coordinates);
            
            return float4(pow(pixel.rgb, float3(exponent)), 1);
        }
        """;

    public override SKShader? GenerateOutputShader() => InputShader is null
        ? null
        : Effect.ToShader(true, new SKRuntimeEffectUniforms(Effect) { { "gamma", _gamma } },
            new SKRuntimeEffectChildren(Effect) { { "image", InputShader } });
}