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
    private class FrustumCorner
    {
        public Vector3[] Near = new Vector3[4];
        public Vector3[] Far = new Vector3[4];
        public static FrustumCorner Copy(FrustumCorner corners)
        {
            FrustumCorner temp = new FrustumCorner();
            for (int i = 0; i < 4; i++)
            {
                temp.Near[i] = new Vector3(corners.Near[i].x, corners.Near[i].y, corners.Near[i].z);
                temp.Far[i] = new Vector3(corners.Far[i].x, corners.Far[i].y, corners.Far[i].z);
            }
            return temp;
        }
    }
    private class ShaderPropertyID
    {
        public int ShadowMap_Tex;
        public int ShadowMap_VP;

        public ShaderPropertyID()
        {
            ShadowMap_Tex = Shader.PropertyToID("_ShadowMapTex");
            ShadowMap_VP = Shader.PropertyToID("_ShadowMapVP");
        }
    }
    public RSMRenderPass(RSMRenderAsset asset) : base(asset)
    {
        renderAsset = asset;
    }
    private const string BUFFER_NAME = "RSM";
    private const string FRP_BASE = "FRP_BASE";
    private RSMRenderAsset renderAsset = null;
    private ShaderTagId baseShaderTagID = new ShaderTagId(FRP_BASE);
    private CommandBuffer buffer = new CommandBuffer() { name = BUFFER_NAME };
    private FRenderSetting setting = null;
    private static readonly ShaderPropertyID shaderPropertyID = new ShaderPropertyID();
    private FrustumCorner cameraCorner = new FrustumCorner();
    private FrustumCorner lightCorner = new FrustumCorner();


    public override void Render()
    {
        setting = renderingData.settings;
        buffer.BeginSample(BUFFER_NAME);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();

        buffer.SetRenderTarget(renderingData.ColorTarget, renderingData.DepthTarget);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();

        PrePareShadowMap();
        PrePareReflectiveShadowMap();
        DrawRender();


        buffer.EndSample(BUFFER_NAME);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

//--------------sm
    private void PrePareShadowMap()
    {
        if(camera.cameraType != CameraType.Game) camera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        int lightIndex = 0;
        foreach(var light in renderingData.cullingResults.visibleLights)
        {
            if(light.lightType == LightType.Directional)
            {
                PrepareDirectionShadow(light.light,lightIndex++);
            }
        }
    }

    private void PrepareDirectionShadow(Light dirLight,int lightIndex)
    {
        renderingData.cullingResults.GetShadowCasterBounds(lightIndex,out Bounds bounds);
        var cameraLocal2World = camera.transform.localToWorldMatrix;
        
        var near = camera.nearClipPlane;
        var far = camera.farClipPlane;
        var unitHeight = Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*0.5f);
        var unitWidth = unitHeight*camera.aspect;

        cameraCorner.Near[0] = cameraLocal2World.MultiplyPoint(new Vector3(-unitWidth,-unitHeight,1)*near);
        cameraCorner.Near[1] = cameraLocal2World.MultiplyPoint(new Vector3(-unitWidth,unitHeight,1)*near);
        cameraCorner.Near[2] = cameraLocal2World.MultiplyPoint(new Vector3(unitWidth,unitHeight,1)*near);
        cameraCorner.Near[3] = cameraLocal2World.MultiplyPoint(new Vector3(unitWidth,-unitHeight,1)*near);
        cameraCorner.Far[0] = cameraLocal2World.MultiplyPoint(new Vector3(-unitWidth,-unitHeight,1)*far);
        cameraCorner.Far[1] = cameraLocal2World.MultiplyPoint(new Vector3(-unitWidth,unitHeight,1)*far);
        cameraCorner.Far[2] = cameraLocal2World.MultiplyPoint(new Vector3(unitWidth,unitHeight,1)*far);
        cameraCorner.Far[3] = cameraLocal2World.MultiplyPoint(new Vector3(unitWidth,-unitHeight,1)*far);
// DrawAABB(cameraCorner);
        var cameraBounds = GetBoundingBox(cameraCorner,dirLight.transform.worldToLocalMatrix);
        DrawAABB(cameraBounds,dirLight.transform.localToWorldMatrix);
        // cameraCorner.Near[0] = dirLight.transform.worldToLocalMatrix.MultiplyPoint(cameraCorner.Near[0]);
        // cameraCorner.Near[1] = dirLight.transform.worldToLocalMatrix.MultiplyPoint(cameraCorner.Near[1]);
        // cameraCorner.Near[2] = dirLight.transform.worldToLocalMatrix.MultiplyPoint(cameraCorner.Near[2]);
        // cameraCorner.Near[3] = dirLight.transform.worldToLocalMatrix.MultiplyPoint(cameraCorner.Near[3]);
        // cameraCorner.Far[0] = dirLight.transform.worldToLocalMatrix.MultiplyPoint(cameraCorner.Far[0]);
        // cameraCorner.Far[1] = dirLight.transform.worldToLocalMatrix.MultiplyPoint(cameraCorner.Far[1]);
        // cameraCorner.Far[2] = dirLight.transform.worldToLocalMatrix.MultiplyPoint(cameraCorner.Far[2]);
        // cameraCorner.Far[3] = dirLight.transform.worldToLocalMatrix.MultiplyPoint(cameraCorner.Far[3]);
        //DrawAABB(cameraCorner,dirLight.transform.localToWorldMatrix);
        //Utility.DrawBound(bounds,Color.red);
    }

    private FrustumCorner GetBoundingBox(FrustumCorner c,Matrix4x4 mat)
    {
        FrustumCorner res = new FrustumCorner();
        res.Near[0] = new Vector3(999,999,999);
        res.Near[1] = new Vector3(999,-999,999);
        res.Near[2] = new Vector3(-999,-999,999);
        res.Near[3] = new Vector3(-999,999,999);
        res.Far[0] = new Vector3(999,999,-999);
        res.Far[1] = new Vector3(999,-999,-999);
        res.Far[2] = new Vector3(-999,-999,-999);
        res.Far[3] = new Vector3(-999,999,-999);

        float minX = 999;
        float minY = 999;
        float minZ = 999;
        float maxX = -999;
        float maxY = -999;
        float maxZ = -999;
        for(int i=0;i<4;i++)
        {
            minX = Mathf.Min(minX,mat.MultiplyPoint(c.Near[i]).x);
            minY = Mathf.Min(minY,mat.MultiplyPoint(c.Near[i]).y);
            minZ = Mathf.Min(minZ,mat.MultiplyPoint(c.Near[i]).z);

            maxX = Mathf.Max(maxX,mat.MultiplyPoint(c.Near[i]).x);
            maxY = Mathf.Max(maxY,mat.MultiplyPoint(c.Near[i]).y);
            maxZ = Mathf.Max(maxZ,mat.MultiplyPoint(c.Near[i]).z);
        }
        for(int i=0;i<4;i++)
        {
            minX = Mathf.Min(minX,mat.MultiplyPoint(c.Far[i]).x);
            minY = Mathf.Min(minY,mat.MultiplyPoint(c.Far[i]).y);
            minZ = Mathf.Min(minZ,mat.MultiplyPoint(c.Far[i]).z);

            maxX = Mathf.Max(maxX,mat.MultiplyPoint(c.Far[i]).x);
            maxY = Mathf.Max(maxY,mat.MultiplyPoint(c.Far[i]).y);
            maxZ = Mathf.Max(maxZ,mat.MultiplyPoint(c.Far[i]).z);
        }
        res.Near[0] =new Vector3(minX,minY,minZ);
        res.Near[1] =new Vector3(minX,maxY,minZ);
        res.Near[2] =new Vector3(maxX,maxY,minZ);
        res.Near[3] =new Vector3(maxX,minY,minZ);
        res.Far[0] =new Vector3(minX,minY,maxZ);
        res.Far[1] =new Vector3(minX,maxY,maxZ);
        res.Far[2] =new Vector3(maxX,maxY,maxZ);
        res.Far[3] =new Vector3(maxX,minY,maxZ);
        return res;
    }
        private void DrawAABB(FrustumCorner debugCor)
        {
            Debug.DrawLine(debugCor.Near[0], debugCor.Near[1], Color.magenta);
            Debug.DrawLine(debugCor.Near[1], debugCor.Near[2], Color.magenta);
            Debug.DrawLine(debugCor.Near[2], debugCor.Near[3], Color.magenta);
            Debug.DrawLine(debugCor.Near[3], debugCor.Near[0], Color.magenta);

            Debug.DrawLine(debugCor.Far[0], debugCor.Far[1], Color.blue);
            Debug.DrawLine(debugCor.Far[1], debugCor.Far[2], Color.blue);
            Debug.DrawLine(debugCor.Far[2], debugCor.Far[3], Color.blue);
            Debug.DrawLine(debugCor.Far[3], debugCor.Far[0], Color.blue);

            Debug.DrawLine(debugCor.Far[0], debugCor.Near[0], Color.blue);
            Debug.DrawLine(debugCor.Far[1], debugCor.Near[1], Color.blue);
            Debug.DrawLine(debugCor.Far[2], debugCor.Near[2], Color.blue);
            Debug.DrawLine(debugCor.Far[3], debugCor.Near[3], Color.blue);
        }
        private void DrawAABB(FrustumCorner debugCor,Matrix4x4 mat)
        {
            Debug.DrawLine(mat.MultiplyPoint(debugCor.Near[0]), mat.MultiplyPoint(debugCor.Near[1]), Color.magenta);
            Debug.DrawLine(mat.MultiplyPoint(debugCor.Near[1]), mat.MultiplyPoint(debugCor.Near[2]), Color.magenta);
            Debug.DrawLine(mat.MultiplyPoint(debugCor.Near[2]), mat.MultiplyPoint(debugCor.Near[3]), Color.magenta);
            Debug.DrawLine(mat.MultiplyPoint(debugCor.Near[3]), mat.MultiplyPoint(debugCor.Near[0]), Color.magenta);

            Debug.DrawLine(mat.MultiplyPoint(debugCor.Far[0]), mat.MultiplyPoint(debugCor.Far[1]), Color.blue);
            Debug.DrawLine(mat.MultiplyPoint(debugCor.Far[1]), mat.MultiplyPoint(debugCor.Far[2]), Color.blue);
            Debug.DrawLine(mat.MultiplyPoint(debugCor.Far[2]), mat.MultiplyPoint(debugCor.Far[3]), Color.blue);
            Debug.DrawLine(mat.MultiplyPoint(debugCor.Far[3]), mat.MultiplyPoint(debugCor.Far[0]), Color.blue);

            Debug.DrawLine(mat.MultiplyPoint(debugCor.Far[0]), mat.MultiplyPoint(debugCor.Near[0]), Color.blue);
            Debug.DrawLine(mat.MultiplyPoint(debugCor.Far[1]), mat.MultiplyPoint(debugCor.Near[1]), Color.blue);
            Debug.DrawLine(mat.MultiplyPoint(debugCor.Far[2]), mat.MultiplyPoint(debugCor.Near[2]), Color.blue);
            Debug.DrawLine(mat.MultiplyPoint(debugCor.Far[3]), mat.MultiplyPoint(debugCor.Near[3]), Color.blue);
        }

    void DrawRTMap()
    {

    }

//----------------rsm
    private void PrePareReflectiveShadowMap()
    {

    }

    private void DrawRender()
    {
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
