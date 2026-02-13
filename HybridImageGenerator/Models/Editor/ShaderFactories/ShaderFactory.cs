using System;
using SkiaSharp;

namespace HybridImageGenerator.Models.Editor.ShaderFactories;

public abstract class ShaderFactory : IDisposable {
    protected readonly SKRuntimeEffect Effect;
    protected readonly SKRuntimeEffectUniforms EmptyUniforms;

    public SKShader? InputShader { get; set; }
    
    protected ShaderFactory(string skSlShader) {
        Effect = SKRuntimeEffect.Create(skSlShader, out var errors) 
                 ?? throw new ArgumentException($"Error while creating effect: {errors}");
        
        EmptyUniforms =  new SKRuntimeEffectUniforms(Effect);
    }
    
    public virtual void Dispose() {
        Effect.Dispose();
    }
    
    public abstract SKShader? GenerateOutputShader();
}