using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(fileName = "RSMRenderAsset", menuName = "FRP/RenderPass/RSMRenderAsset")]
public class RSMRenderAsset : FRenderAssetBase
{
    public override FRenderPass CreateRenderPass()
    {
        return new RSMRenderPass(this);
    }
}


public class RSMRenderPass : FRenderPassRender
{
    public RSMRenderPass(FRenderAssetBase asset) : base(asset)
    {
        
    }
    const string BUFFER_NAME = "RSM";
    CommandBuffer buffer = new CommandBuffer(){name = BUFFER_NAME};


    public override void Render()
    {
        buffer.BeginSample(BUFFER_NAME);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();

        

        buffer.EndSample(BUFFER_NAME);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }
    
    public override void Cleanup()
    {
        
    }


}
