using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class FRP : RenderPipeline
{
    private FRenderSetting renderSettings;
    private CullingResults m_cullingResults;
    int ColorTarget;
    int DepthTarget;
    public FRP(FRenderSetting setting)
    {
        renderSettings = setting;
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        BeginFrameRendering(context, cameras);
        foreach (var camera in cameras)
        {
            BeginCameraRendering(context, camera);

            if (!Cull(context, camera)) break;
            CameraRender(context, camera);

            context.Submit();
            EndCameraRendering(context, camera);
        }
        EndFrameRendering(context, cameras);
    }

    private bool Cull(ScriptableRenderContext context, Camera camera)
    {
        if (camera.TryGetCullingParameters(out ScriptableCullingParameters cullParam))
        {
            m_cullingResults = context.Cull(ref cullParam);
            return true;
        }
        return false;
    }

    private void CameraRender(ScriptableRenderContext context, Camera camera)
    {
        if (camera.cameraType == CameraType.SceneView)
        {
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
        }

        RenderingData renderingData = new RenderingData();
        renderingData.settings = renderSettings;
        renderingData.cullingResults = m_cullingResults;
        renderingData.lightingData = new Dictionary<int, LightingData>();
        renderingData.shadowData = new Dictionary<int, ShadowData>();
        renderingData.sourceViewMatrix = camera.worldToCameraMatrix;
        renderingData.sourceProjectionMatrix = camera.projectionMatrix;
        renderingData.ColorTarget = BuiltinRenderTextureType.CameraTarget;
        renderingData.DepthTarget = BuiltinRenderTextureType.CameraTarget;

        Setup(context, camera, ref renderingData);

        if(renderSettings.renderAssetBase)
        {
            var pass = renderSettings.renderAssetBase.GetRenderPass(camera);
            pass.Setup(context,camera,renderingData);
            pass.Render();
        }

        var cmd = CommandBufferPool.Get(camera.name);
        cmd.Clear();
        cmd.SetRenderTarget(ColorTarget, DepthTarget);
        cmd.ClearRenderTarget(true, true, Color.black, 1);
        cmd.SetRenderTarget(DepthTarget, DepthTarget);
        cmd.ClearRenderTarget(true, true, Color.black, 1);
        cmd.SetRenderTarget(ColorTarget, DepthTarget);
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();

        context.DrawSkybox(camera);

        PreRenderPass.Instance.ExecuteRender(ref context,camera,ref renderingData);
        //PreRenderPass.Instance.Render();
        // foreach (var pass in m_renderPassQueue)
        // {
        //     pass.Setup(context, camera, renderingData);
        //     pass.Render();
        // }

        if (UnityEditor.Handles.ShouldRenderGizmos())
        {
            context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
        }

        var resetBuffer = CommandBufferPool.Get("ResetBuffer");
        //resetBuffer.Blit(renderingData.ColorTarget, BuiltinRenderTextureType.CameraTarget);
        resetBuffer.SetViewProjectionMatrices(renderingData.sourceViewMatrix, renderingData.sourceProjectionMatrix);
        resetBuffer.Blit(renderingData.ColorTarget, BuiltinRenderTextureType.CameraTarget);
        context.ExecuteCommandBuffer(resetBuffer);
        resetBuffer.Clear();
        CommandBufferPool.Release(resetBuffer);
        CommandBufferPool.Release(cmd);


        Cleanup(context);
        context.Submit();
    }

    private void Setup(ScriptableRenderContext context, Camera camera, ref RenderingData renderingData)
    {
        context.SetupCameraProperties(camera);
        //setup之后需要清一次
        var buffer = CommandBufferPool.Get();
        this.ColorTarget = Shader.PropertyToID("_ColorTarget");
        this.DepthTarget = Shader.PropertyToID("_DepthTarget");
        buffer.GetTemporaryRT(ColorTarget, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Point, RenderTextureFormat.Default);
        buffer.GetTemporaryRT(DepthTarget, camera.pixelWidth, camera.pixelHeight, 24, FilterMode.Point, RenderTextureFormat.Depth);
        //buffer.ClearRenderTarget(true, true, Color.clear);

        //buffer.SetRenderTarget(renderingData.ColorTarget,renderingData.DepthTarget);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
        CommandBufferPool.Release(buffer);
        renderingData.ColorTarget = ColorTarget;
        renderingData.DepthTarget = DepthTarget;

    }

    private void Cleanup(ScriptableRenderContext context)
    {
        var buffer = CommandBufferPool.Get();
        //ColorTarget.Release(cmd);
        //DepthTarget.Release(cmd);
        buffer.ReleaseTemporaryRT(ColorTarget);
        buffer.ReleaseTemporaryRT(DepthTarget);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
        CommandBufferPool.Release(buffer);
    }
}
