using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
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

        public int Rsm_Flux;
        public int Rsm_WorldPos;
        public int Rsm_WorldNormal;


        public ShaderPropertyID()
        {
            ShadowMap_Tex = Shader.PropertyToID("_ShadowMapTex");
            ShadowMap_VP = Shader.PropertyToID("_ShadowMapVP");
            Rsm_Flux = Shader.PropertyToID("_RsmFlux");
            Rsm_WorldPos = Shader.PropertyToID("_RsmWorldPos");
            Rsm_WorldNormal = Shader.PropertyToID("_RsmWorldNormal");
        }
    }

    Vector3 SplitPosition = new Vector3();
    Quaternion SplitRotate = new Quaternion();
    Matrix4x4 SplitMatrix = new Matrix4x4();
    public RSMRenderPass(RSMRenderAsset asset) : base(asset)
    {
        renderAsset = asset;
        SplitMatrix = Matrix4x4.identity;
        SplitRotate = Quaternion.identity;
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
    private FrustumCorner casterCorner = new FrustumCorner();

    RenderTextureDescriptor shadowMapDesc = new RenderTextureDescriptor();
    RenderTextureDescriptor fluxDesc = new RenderTextureDescriptor();
    RenderTextureDescriptor normalDesc = new RenderTextureDescriptor();
    RenderTextureDescriptor worldPosDesc = new RenderTextureDescriptor();


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
        if (camera.cameraType != CameraType.Game) camera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        int lightIndex = 0;
        foreach (var light in renderingData.cullingResults.visibleLights)
        {
            if (light.lightType == LightType.Directional)
            {
                PrepareDirectionShadow(light.light, lightIndex++);
            }
        }
    }

    Vector2[] drawOrder = new Vector2[4]{
                new Vector2(-1,-1),
                new Vector2(-1,1),
                new Vector2(1,1),
                new Vector2(1,-1)
            };
    private void PrepareDirectionShadow(Light dirLight, int lightIndex)
    {
        float near = camera.nearClipPlane;
        float far = (camera.farClipPlane);
        float height = Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
        float width = height * camera.aspect;

        Matrix4x4 cameraLocal2World = camera.transform.localToWorldMatrix;

        var hasShadow = renderingData.cullingResults.GetShadowCasterBounds(lightIndex, out Bounds bounds);
        //四个点顺序：左下，左上，右上，右下(下面的都同理)
        //var ttBounds = new Vector3[8];
        //for (int x = -1, i = 0; x <= 1; x += 2)
        //    for (int y = -1; y <= 1; y += 2)
        //        for (int z = -1; z <= 1; z += 2)
        //            ttBounds[i++] = bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z));
        if (hasShadow == false) return;
        //变换包围盒
        var casterBoundVerts = new FrustumCorner();
        casterBoundVerts.Near = new Vector3[4];
        casterBoundVerts.Far = new Vector3[4];
        casterBoundVerts.Near[0] = (bounds.center + Vector3.Scale(bounds.extents, new Vector3(-1, -1, -1)));
        casterBoundVerts.Near[1] = (bounds.center + Vector3.Scale(bounds.extents, new Vector3(-1, 1, -1)));
        casterBoundVerts.Near[2] = (bounds.center + Vector3.Scale(bounds.extents, new Vector3(1, 1, -1)));
        casterBoundVerts.Near[3] = (bounds.center + Vector3.Scale(bounds.extents, new Vector3(1, -1, -1)));
        casterBoundVerts.Far[0] = (bounds.center + Vector3.Scale(bounds.extents, new Vector3(-1, -1, 1)));
        casterBoundVerts.Far[1] = (bounds.center + Vector3.Scale(bounds.extents, new Vector3(-1, 1, 1)));
        casterBoundVerts.Far[2] = (bounds.center + Vector3.Scale(bounds.extents, new Vector3(1, 1, 1)));
        casterBoundVerts.Far[3] = (bounds.center + Vector3.Scale(bounds.extents, new Vector3(1, -1, 1)));
        // for (int idx = 0; idx < 4; idx++)
        // {
        //     casterBoundVerts.Near[idx] = dirLight.transform.worldToLocalMatrix.MultiplyPoint(casterBoundVerts.Near[idx]);
        //     casterBoundVerts.Far[idx] = dirLight.transform.worldToLocalMatrix.MultiplyPoint(casterBoundVerts.Far[idx]);
        // }
        // //计算出包围盒在在灯光空间下的样子
        Vector3[] nn = new Vector3[4];
        Vector3[] ff = new Vector3[4];

        casterBoundVerts = GetBoundingBox(casterBoundVerts, dirLight.transform.worldToLocalMatrix);
        for (int idx = 0; idx < 4; idx++)
        {
            casterBoundVerts.Near[idx] = dirLight.transform.localToWorldMatrix.MultiplyPoint(casterBoundVerts.Near[idx]);
            casterBoundVerts.Far[idx] = dirLight.transform.localToWorldMatrix.MultiplyPoint(casterBoundVerts.Near[idx]);
        }
        //DrawAABB(casterBoundVerts);


        for (int j = 0; j < 4; j++)
        {
            var pNear = new Vector3(width * drawOrder[j].x, height * drawOrder[j].y, 1) * near;
            var pFar = new Vector3(width * drawOrder[j].x, height * drawOrder[j].y, 1) * far;
            cameraCorner.Near[j] = cameraLocal2World.MultiplyPoint(pNear);
            cameraCorner.Far[j] = cameraLocal2World.MultiplyPoint(pFar);
        }
        shadowMapDesc = ConfigRTDescriptor(shadowMapDesc,renderAsset.RSM_Resolution,renderAsset.RSM_Resolution,GraphicsFormat.R32G32B32A32_SFloat,32);
        buffer.GetTemporaryRT(shaderPropertyID.ShadowMap_Tex, shadowMapDesc, FilterMode.Point);
        
        fluxDesc = ConfigRTDescriptor(fluxDesc,renderAsset.RSM_Resolution,renderAsset.RSM_Resolution,GraphicsFormat.R32G32B32A32_SFloat,32);
        buffer.GetTemporaryRT(shaderPropertyID.Rsm_Flux, fluxDesc, FilterMode.Bilinear);

        normalDesc = ConfigRTDescriptor(normalDesc,renderAsset.RSM_Resolution,renderAsset.RSM_Resolution,GraphicsFormat.R32G32B32A32_SFloat,32);
        buffer.GetTemporaryRT(shaderPropertyID.Rsm_WorldNormal,normalDesc,FilterMode.Bilinear);

        worldPosDesc = ConfigRTDescriptor(worldPosDesc,renderAsset.RSM_Resolution,renderAsset.RSM_Resolution,GraphicsFormat.R32G32B32A32_SFloat,32);
        buffer.GetTemporaryRT(shaderPropertyID.Rsm_WorldPos,worldPosDesc,FilterMode.Bilinear);

        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();


        for (int j = 0; j < 4; j++)
        {
            var mainNearCor = cameraCorner.Near[j];
            var mainFarCor = cameraCorner.Far[j];
            lightCorner.Near[j] = SplitMatrix.inverse.MultiplyPoint(mainNearCor);
            lightCorner.Far[j] = SplitMatrix.inverse.MultiplyPoint(mainFarCor);
        }

        var farDist = Vector3.Distance(lightCorner.Far[0], lightCorner.Far[2]);
        var crossDist = Vector3.Distance(lightCorner.Near[0], lightCorner.Far[2]);
        var maxDist = Mathf.Max(farDist, crossDist);

        //更新灯光视椎的八个顶点为boundingbox的点
        nn = new Vector3[4];
        ff = new Vector3[4];
        for (int idx = 0; idx < 4; idx++)
        {
            var pNear = lightCorner.Near[idx];
            var pFar = lightCorner.Far[idx];
            nn[0] = new Vector3(Mathf.Min(nn[0].x, pNear.x, pFar.x), Mathf.Min(nn[0].y, pNear.y, pFar.y), Mathf.Min(nn[0].z, pNear.z, pFar.z));
            nn[1] = new Vector3(Mathf.Min(nn[1].x, pNear.x, pFar.x), Mathf.Max(nn[1].y, pNear.y, pFar.y), Mathf.Min(nn[1].z, pNear.z, pFar.z));
            nn[2] = new Vector3(Mathf.Max(nn[2].x, pNear.x, pFar.x), Mathf.Max(nn[2].y, pNear.y, pFar.y), Mathf.Min(nn[2].z, pNear.z, pFar.z));
            nn[3] = new Vector3(Mathf.Max(nn[3].x, pNear.x, pFar.x), Mathf.Min(nn[3].y, pNear.y, pFar.y), Mathf.Min(nn[3].z, pNear.z, pFar.z));
            //
            ff[0] = new Vector3(Mathf.Min(ff[0].x, pNear.x, pFar.x), Mathf.Min(ff[0].y, pNear.y, pFar.y), Mathf.Max(ff[0].z, pNear.z, pFar.z));
            ff[1] = new Vector3(Mathf.Min(ff[1].x, pNear.x, pFar.x), Mathf.Max(ff[1].y, pNear.y, pFar.y), Mathf.Max(ff[1].z, pNear.z, pFar.z));
            ff[2] = new Vector3(Mathf.Max(ff[2].x, pNear.x, pFar.x), Mathf.Max(ff[2].y, pNear.y, pFar.y), Mathf.Max(ff[2].z, pNear.z, pFar.z));
            ff[3] = new Vector3(Mathf.Max(ff[3].x, pNear.x, pFar.x), Mathf.Min(ff[3].y, pNear.y, pFar.y), Mathf.Max(ff[3].z, pNear.z, pFar.z));
        }
        for (int idx = 0; idx < 4; idx++)
        {
            lightCorner.Near[idx] = nn[idx];
            lightCorner.Far[idx] = ff[idx];
        }

        for (int idx = 0; idx < 4; idx++)
        {
            lightCorner.Near[idx] = nn[idx];
            lightCorner.Near[idx] = SplitMatrix.inverse.MultiplyPoint(casterBoundVerts.Near[idx]);
            //  if (D2 - D1 > 0)
            //  {
            //      lightCorner.Near[idx].z = nn[idx].z - dis;
            //  }
            //  else
            //  {
            //      lightCorner.Near[idx].z = nn[idx].z + dis;
            //  }
        }
        //lightCorner.Near=casterBoundVerts.Near;
        //Debug.Log("d:"+D2+" "+D1+" "+dis);
        //DrawAABB(lightCorner);
        var center = PixelAlignment(maxDist, lightCorner);
        SplitRotate = dirLight.transform.rotation;
        SplitPosition = SplitMatrix.MultiplyPoint(center);
        SplitMatrix = GetModelMatrix(SplitPosition, SplitRotate);
        var viewMatrix = Matrix4x4.TRS(SplitPosition, SplitRotate, Vector3.one).inverse;
        var t = Matrix4x4.identity;
        t.m22 = -1;
        viewMatrix = t * viewMatrix;

        //Debug.Log(maxDist + " " + lightCorner.Near[0].z + " " + lightCorner.Far[0].z + " " + SplitPosition + " " + center);
        var project = Matrix4x4.Ortho(-maxDist * 0.5f, maxDist * 0.5f, -maxDist * 0.5f, maxDist * 0.5f, lightCorner.Near[0].z, lightCorner.Far[0].z);
        buffer.SetViewProjectionMatrices(viewMatrix, project);

        CullingResults m_cullingResults = new CullingResults();
        if (camera.TryGetCullingParameters(out ScriptableCullingParameters cullParam))
        {
            cullParam.isOrthographic = true;
            for (int i = 0; i < cullParam.cullingPlaneCount; i++)
            {
                cullParam.SetCullingPlane(i, GetCullingPlane(i));
                //cullParam.SetCullingPlane(i, new Plane(new Vector3(0, 1, 0), new Vector3()));
            }
            m_cullingResults = context.Cull(ref cullParam);
        }

        DrawShadows(project, viewMatrix,m_cullingResults);
        DrawFlux(m_cullingResults);
        DrawNormal(m_cullingResults);
        DrawWorldPos(m_cullingResults);
        //DD();
        //buffer.SetGlobalInt(shaderPropertyID.smType, (int)settings.shadowSetting.shadowType);
        //buffer.SetGlobalVector(shaderPropertyID.smSplitNears, new Vector4(nears[0], nears[1], nears[2], nears[3]));
        //buffer.SetGlobalVector(shaderPropertyID.smSplitFars, new Vector4(fars[0], fars[1], fars[2], fars[3]));
        //buffer.SetGlobalMatrixArray(shaderPropertyID.smVPArray, vpArray);
        //buffer.SetGlobalTexture(shaderPropertyID.smShadowMap, smid);
        buffer.ReleaseTemporaryRT(shaderPropertyID.ShadowMap_Tex);
        buffer.ReleaseTemporaryRT(shaderPropertyID.Rsm_Flux);
        buffer.ReleaseTemporaryRT(shaderPropertyID.Rsm_WorldNormal);
        buffer.ReleaseTemporaryRT(shaderPropertyID.Rsm_WorldPos);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();

        //reset camera params
        buffer.SetViewProjectionMatrices(renderingData.sourceViewMatrix, renderingData.sourceProjectionMatrix);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    private void DrawShadows(Matrix4x4 project, Matrix4x4 viewMatrix,CullingResults m_cullingResults)
    {

        buffer.SetRenderTarget(shaderPropertyID.ShadowMap_Tex, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        buffer.ClearRenderTarget(true, true, Color.clear);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();

        SortingSettings sortingSettings = new SortingSettings(camera);
        FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all);
        //filteringSettings.sortingLayerRange = SortingLayerRange.all;
        DrawingSettings drawingSettings = new DrawingSettings() { sortingSettings = sortingSettings };
        RenderStateBlock stateBlock = new RenderStateBlock(RenderStateMask.Nothing);

        // drawingSettings.SetShaderPassName(0, new ShaderTagId("FRP_ShadowCaster_SM"));
        drawingSettings.SetShaderPassName(0, new ShaderTagId("FRP_Caster_Shadow"));

        sortingSettings.criteria = SortingCriteria.RenderQueue;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.all;
        drawingSettings.perObjectData = PerObjectData.Lightmaps | PerObjectData.LightProbe | PerObjectData.LightProbeProxyVolume | PerObjectData.ReflectionProbes;
        context.DrawRenderers(m_cullingResults, ref drawingSettings, ref filteringSettings, ref stateBlock);


        var proj = GL.GetGPUProjectionMatrix(project, false);
        // vpArray[cascadeIndex] = proj * viewMatrix;
        buffer.SetRenderTarget(cameraTargetID, cameraTargetID);
        //buffer.ClearRenderTarget(true, true, Color.clear);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();

    }

    private void DrawFlux(CullingResults m_cullingResults)
    {

    }

    private void DrawNormal(CullingResults m_cullingResults)
    {

    }

    private void DrawWorldPos(CullingResults m_cullingResults)
    {

    }



    private RenderTextureDescriptor ConfigRTDescriptor(RenderTextureDescriptor descriptor,int width,int height,GraphicsFormat format,int depthBits,TextureDimension dimension = TextureDimension.Tex2D)
    {
        descriptor.width = width;
        descriptor.height = height;
        descriptor.graphicsFormat = format;
        descriptor.depthBufferBits = depthBits;
        descriptor.useMipMap = false;
        descriptor.autoGenerateMips = false;
        descriptor.msaaSamples = 1;
        descriptor.dimension = dimension;
        return descriptor;
    }

    private Vector3 GetPlaneNormal(Vector3 p0, Vector3 p1, Vector3 p2)
    {
        return Vector3.Cross(p0 - p1, p2 - p1).normalized;
    }

    //前后，上下，左右
    private Plane GetCullingPlane(int planeIndex)
    {
        var tLight = lightCorner;

        var normal = new Vector3();
        var point = new Vector3();
        var mat = SplitMatrix;
        var nears = new Vector3[4];
        var fars = new Vector3[4];
        //return new Plane(Vector3.one,Vector3.one);
        for (int i = 0; i < 4; i++)
        {
            var near = mat.MultiplyPoint(tLight.Near[i]);
            nears[i] = new Vector3(near.x, near.y, near.z);

            var far = mat.MultiplyPoint(tLight.Far[i]);
            fars[i] = new Vector3(far.x, far.y, far.z);
        }

        if (planeIndex == 0)
        {
            normal = GetPlaneNormal(nears[0], nears[1], nears[2]);
            point = nears[0];
        }
        else if (planeIndex == 1)
        {
            normal = -GetPlaneNormal(fars[0], fars[1], fars[2]);
            point = fars[0];
        }
        else if (planeIndex == 2)
        {
            normal = GetPlaneNormal(nears[1], fars[1], fars[2]);
            point = nears[1];
        }
        else if (planeIndex == 3)
        {
            normal = -GetPlaneNormal(nears[0], fars[0], fars[3]);
            point = nears[0];
        }
        else if (planeIndex == 4)
        {
            normal = GetPlaneNormal(fars[0], fars[1], nears[1]);
            point = fars[0];
        }
        else if (planeIndex == 5)
        {
            normal = -GetPlaneNormal(fars[3], fars[2], nears[2]);
            point = fars[3];
        }
        return new Plane(normal, point);
    }

    private Matrix4x4 GetModelMatrix(Vector3 position, Quaternion rotate)
    {
        float x = rotate.x;
        float y = rotate.y;
        float z = rotate.z;
        float w = rotate.w;
        var q00 = 1 - 2 * y * y - 2 * z * z;
        var q01 = 2 * x * y - 2 * z * w;
        var q02 = 2 * x * z + 2 * y * w;
        var q10 = 2 * x * y + 2 * z * w;
        var q11 = 1 - 2 * x * x - 2 * z * z;
        var q12 = 2 * y * z - 2 * x * w;
        var q20 = 2 * x * z - 2 * y * w;
        var q21 = 2 * y * z + 2 * x * w;
        var q22 = 1 - 2 * x * x - 2 * y * y;
        var modelMatrix =
            new Matrix4x4(
            new Vector4(q00, q10, q20, 0),
            new Vector4(q01, q11, q21, 0),
            new Vector4(q02, q12, q22, 0),
            new Vector4(position.x, position.y, position.z, 1)
            );
        return modelMatrix;
    }
    RenderTargetIdentifier cameraTargetID = new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);
    private FrustumCorner GetBoundingBox(FrustumCorner c, Matrix4x4 mat)
    {
        FrustumCorner res = new FrustumCorner();
        res.Near[0] = new Vector3(999, 999, 999);
        res.Near[1] = new Vector3(999, -999, 999);
        res.Near[2] = new Vector3(-999, -999, 999);
        res.Near[3] = new Vector3(-999, 999, 999);
        res.Far[0] = new Vector3(999, 999, -999);
        res.Far[1] = new Vector3(999, -999, -999);
        res.Far[2] = new Vector3(-999, -999, -999);
        res.Far[3] = new Vector3(-999, 999, -999);

        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float minZ = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;
        float maxZ = float.MinValue;
        for (int i = 0; i < 4; i++)
        {
            minX = Mathf.Min(minX, mat.MultiplyPoint(c.Near[i]).x);
            minY = Mathf.Min(minY, mat.MultiplyPoint(c.Near[i]).y);
            minZ = Mathf.Min(minZ, mat.MultiplyPoint(c.Near[i]).z);

            maxX = Mathf.Max(maxX, mat.MultiplyPoint(c.Near[i]).x);
            maxY = Mathf.Max(maxY, mat.MultiplyPoint(c.Near[i]).y);
            maxZ = Mathf.Max(maxZ, mat.MultiplyPoint(c.Near[i]).z);
        }
        for (int i = 0; i < 4; i++)
        {
            minX = Mathf.Min(minX, mat.MultiplyPoint(c.Far[i]).x);
            minY = Mathf.Min(minY, mat.MultiplyPoint(c.Far[i]).y);
            minZ = Mathf.Min(minZ, mat.MultiplyPoint(c.Far[i]).z);

            maxX = Mathf.Max(maxX, mat.MultiplyPoint(c.Far[i]).x);
            maxY = Mathf.Max(maxY, mat.MultiplyPoint(c.Far[i]).y);
            maxZ = Mathf.Max(maxZ, mat.MultiplyPoint(c.Far[i]).z);
        }
        res.Near[0] = new Vector3(minX, minY, minZ);
        res.Near[1] = new Vector3(minX, maxY, minZ);
        res.Near[2] = new Vector3(maxX, maxY, minZ);
        res.Near[3] = new Vector3(maxX, minY, minZ);
        res.Far[0] = new Vector3(minX, minY, maxZ);
        res.Far[1] = new Vector3(minX, maxY, maxZ);
        res.Far[2] = new Vector3(maxX, maxY, maxZ);
        res.Far[3] = new Vector3(maxX, minY, maxZ);
        return res;
    }

    private FrustumCorner GetMaxBoundingBox(FrustumCorner c1, FrustumCorner c2)
    {
        var result = new FrustumCorner();
        result.Near[0] = new Vector3(Mathf.Min(c1.Near[0].x, c2.Near[0].x), Mathf.Min(c1.Near[0].y, c2.Near[0].y), Mathf.Min(c1.Near[0].z, c2.Near[0].z));
        result.Near[1] = new Vector3(Mathf.Min(c1.Near[1].x, c2.Near[1].x), Mathf.Max(c1.Near[1].y, c2.Near[1].y), Mathf.Min(c1.Near[1].z, c2.Near[1].z));
        result.Near[2] = new Vector3(Mathf.Max(c1.Near[2].x, c2.Near[2].x), Mathf.Max(c1.Near[2].y, c2.Near[2].y), Mathf.Min(c1.Near[2].z, c2.Near[2].z));
        result.Near[3] = new Vector3(Mathf.Max(c1.Near[3].x, c2.Near[3].x), Mathf.Min(c1.Near[3].y, c2.Near[3].y), Mathf.Min(c1.Near[3].z, c2.Near[3].z));
        result.Far[0] = new Vector3(Mathf.Min(c1.Far[0].x, c2.Far[0].x), Mathf.Min(c1.Far[0].y, c2.Far[0].y), Mathf.Max(c1.Far[0].z, c2.Far[0].z));
        result.Far[1] = new Vector3(Mathf.Min(c1.Far[1].x, c2.Far[1].x), Mathf.Max(c1.Far[1].y, c2.Far[1].y), Mathf.Max(c1.Far[1].z, c2.Far[1].z));
        result.Far[2] = new Vector3(Mathf.Max(c1.Far[2].x, c2.Far[2].x), Mathf.Max(c1.Far[2].y, c2.Far[2].y), Mathf.Max(c1.Far[2].z, c2.Far[2].z));
        result.Far[3] = new Vector3(Mathf.Max(c1.Far[3].x, c2.Far[3].x), Mathf.Min(c1.Far[3].y, c2.Far[3].y), Mathf.Max(c1.Far[3].z, c2.Far[3].z));
        return result;
    }

    private Vector3 PixelAlignment(float maxDist, FrustumCorner cameraBounds)
    {
        //防止边缘抖动
        float minX = cameraBounds.Near[0].x;
        float maxX = cameraBounds.Near[2].x;
        float minY = cameraBounds.Near[0].y;
        float maxY = cameraBounds.Near[2].y;
        float minZ = cameraBounds.Near[0].z;
        float unitPerTex = maxDist / (float)renderAsset.RSM_Resolution;
        var posx = (minX + maxX) * 0.5f;
        posx /= unitPerTex;
        posx = Mathf.FloorToInt(posx);
        posx *= unitPerTex;

        var posy = (minY + maxY) * 0.5f;
        posy /= unitPerTex;
        posy = Mathf.FloorToInt(posy);
        posy *= unitPerTex;

        var posz = minZ;
        posz /= unitPerTex;
        posz = Mathf.FloorToInt(posz);
        posz *= unitPerTex;
        return new Vector3(posx, posy, posz);
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
    private void DrawAABB(FrustumCorner debugCor, Matrix4x4 mat)
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
        buffer.BeginSample("DrawOpaque");
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();

        buffer.SetRenderTarget(renderingData.ColorTarget, renderingData.DepthTarget);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
        SortingSettings sortingSettings = new SortingSettings(camera);
        FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
        DrawingSettings drawingSettings = new DrawingSettings(baseShaderTagID, sortingSettings);
        RenderStateBlock stateBlock = new RenderStateBlock(RenderStateMask.Nothing);
        sortingSettings.criteria = SortingCriteria.CommonOpaque;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.opaque;
        drawingSettings.perObjectData = PerObjectData.Lightmaps | PerObjectData.LightProbe | PerObjectData.LightProbeProxyVolume | PerObjectData.ReflectionProbes;
        context.DrawRenderers(renderingData.cullingResults, ref drawingSettings, ref filteringSettings, ref stateBlock);

        buffer.EndSample("DrawOpaque");
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    public override void Cleanup()
    {

    }


}
