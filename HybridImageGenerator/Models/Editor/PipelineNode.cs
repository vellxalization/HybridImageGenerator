using System;
using HybridImageGenerator.Models.Editor.ShaderFactories;
using SkiaSharp;

namespace HybridImageGenerator.Models.Editor;

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
