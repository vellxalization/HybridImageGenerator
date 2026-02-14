using System;
using HybridImageGenerator.Models.ImageProcessing.ShaderFactories;
using SkiaSharp;

namespace HybridImageGenerator.Models.ImageProcessing.Editor;

public class PipelineNode<T>(T factory) where T : ShaderFactory {
    public T Factory => factory;

    public void SendUpdate() => ShaderUpdated?.Invoke(this, Factory.GenerateOutputShader());

    public void Link<TChild>(PipelineNode<TChild> node, Action<SKShader?, PipelineNode<TChild>> setter) where TChild : ShaderFactory {
        ShaderUpdated += (_, shader) => {
            setter(shader, node);
            node.SendUpdate();
        };
    }
    
    public event EventHandler<SKShader?>? ShaderUpdated; 
}
