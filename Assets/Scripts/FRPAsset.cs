using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public enum GIType 
{
    NONE,
    RSM ,
    LPV,
    VXGI
}

[Serializable]
public class FRenderSetting 
{
    public GIType GI_Type;
    public FRenderAssetBase renderAssetBase;
}

[CreateAssetMenu(menuName = "FRP/Create new asset")]
public class FRPAsset : RenderPipelineAsset
{
    [SerializeField]
    public FRenderSetting renderSetting;
    protected override RenderPipeline CreatePipeline()
    {
        return new FRP(renderSetting);
    }
}
