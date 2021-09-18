using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(fileName = "RSMRenderAsset", menuName = "FRP/RenderPass/RSMRenderAsset")]
public class RSMRenderAsset : FRenderAssetBase
{
    public int RSM_Resolution = 1024;

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
    private const string BUFFER_NAME = "RSM";
    private const string FRP_BASE = "FRP_BASE";
    ShaderTagId baseShaderTagID = new ShaderTagId(FRP_BASE);
    CommandBuffer buffer = new CommandBuffer() { name = BUFFER_NAME };



    public override void Render()
    {
        buffer.BeginSample(BUFFER_NAME);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();

        buffer.SetRenderTarget(renderingData.ColorTarget, renderingData.DepthTarget);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();

        DrawRender();


        buffer.EndSample(BUFFER_NAME);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    private void DrawRender()
    {
        Debug.Log("fzy ???");
        SortingSettings sortingSettings = new SortingSettings(camera);
        FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
        DrawingSettings drawingSettings = new DrawingSettings(baseShaderTagID, sortingSettings);
        RenderStateBlock stateBlock = new RenderStateBlock(RenderStateMask.Nothing);
        sortingSettings.criteria = SortingCriteria.CommonOpaque;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.opaque;
        drawingSettings.perObjectData = PerObjectData.Lightmaps | PerObjectData.LightProbe | PerObjectData.LightProbeProxyVolume | PerObjectData.ReflectionProbes;
        context.DrawRenderers(renderingData.cullingResults, ref drawingSettings, ref filteringSettings, ref stateBlock);
    }

    public override void Cleanup()
    {

    }


}
