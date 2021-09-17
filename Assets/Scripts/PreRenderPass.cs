using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class PreRenderPass
{
    private PreRenderPass() { }
    private static PreRenderPass _instance;
    public static PreRenderPass Instance
    {
        get
        {
            return _instance == null ? new PreRenderPass() : _instance;
        }
    }
    private class ShaderPropertyID
    {
        public int lightData;
        public int lightCount;

        public int smShadowMap;
        public int smVPArray;
        public int smSplitNears;
        public int smSplitFars;
        public int smType;
        public int smTempDepth;

        public int depthNormalTex;
        public ShaderPropertyID()
        {
            lightData = Shader.PropertyToID("_LightData");
            lightCount = Shader.PropertyToID("_LightCount");

            smType = Shader.PropertyToID("_ShadowType");
            smShadowMap = Shader.PropertyToID("_SMShadowMap");
            smVPArray = Shader.PropertyToID("_LightVPArray");
            smSplitNears = Shader.PropertyToID("_LightSplitNear");
            smSplitFars = Shader.PropertyToID("_LightSplitFar");
            smTempDepth = Shader.PropertyToID("_TempDepth");

            depthNormalTex = Shader.PropertyToID("_DepthNormal");
        }
    }
    private const string BUFFER_NAME = "PreRender";
    FRenderSetting setting;
    CommandBuffer buffer = new CommandBuffer() { name = BUFFER_NAME };
    ScriptableRenderContext context;
    Camera camera;
    RenderingData renderingData;
    private static readonly ShaderPropertyID shaderPropertyID = new ShaderPropertyID();

    ComputeBuffer lightDataBuffer;
    List<LightingData> lightDataList;
    private int prevLightCount = -1;
    public void ExecuteRender(ref ScriptableRenderContext context, Camera camera, ref RenderingData renderingData)
    {
        this.context = context;
        this.camera = camera;
        this.renderingData = renderingData;
        Render();
    }

    public void Render()
    {
        buffer.BeginSample(BUFFER_NAME);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();

        setting = renderingData.settings;
        renderingData.lightingData.Clear();
        renderingData.shadowData.Clear();

        RenderPrepareLight();
        RenderPrepareShadow();
        RenderPrepareDepthNormal();

        buffer.EndSample(BUFFER_NAME);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    private void RenderPrepareLight()
    {
        int idx = 0;
        NativeArray<VisibleLight> visibleLights = renderingData.cullingResults.visibleLights;
        int lightCount = visibleLights.Length;
        if (lightCount == 0) return;
        if (lightCount != prevLightCount)
        {
            if (lightDataBuffer != null) lightDataBuffer.Release();
            lightDataBuffer = new ComputeBuffer(lightCount, Marshal.SizeOf(typeof(LightingData)));
            lightDataList = new List<LightingData>(lightCount);
        }
        foreach (var light in visibleLights)
        {
            Matrix4x4 local2world = light.localToWorldMatrix;
            LightingData data = new LightingData();
            if (light.lightType == LightType.Directional)
            {
                data.geometry = local2world.GetColumn(2).normalized;
                data.pos_type = -data.geometry;
                data.geometry.w = float.MaxValue;
                data.pos_type.w = 1;
                data.color = light.finalColor;
            }
            else if (light.lightType == LightType.Point)
            {
                data.geometry = local2world.GetColumn(2).normalized;
                data.geometry.w = light.range;
                data.pos_type = new Vector4(local2world.m03, local2world.m13, local2world.m23, 2);
                data.color = light.finalColor;
            }
            else if (light.lightType == LightType.Spot)
            {

            }
            renderingData.lightingData.Add(idx++, data);
        }
        lightDataBuffer.SetData<LightingData>(renderingData.lightingData.Values.GetValueList<int, LightingData>());
        buffer.SetGlobalInt(shaderPropertyID.lightCount, lightCount);
        buffer.SetGlobalBuffer(shaderPropertyID.lightData, lightDataBuffer);

        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
        prevLightCount = lightCount;
    }

    private void RenderPrepareShadow()
    {

    }

    private void RenderPrepareDepthNormal()
    {

    }
}
