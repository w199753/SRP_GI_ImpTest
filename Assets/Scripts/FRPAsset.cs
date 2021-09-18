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
    private Lazy<Shader> m_defaultShader = new Lazy<Shader>(()=>Shader.Find("FRP/Default"));
    private Material m_defualtMaterial;
    protected override RenderPipeline CreatePipeline()
    {
        return new FRP(renderSetting);
    }

    public override Material defaultMaterial
    {
        get{
            if(m_defualtMaterial == null)
            {
                m_defualtMaterial = new Material(m_defaultShader.Value);
            }
            return m_defualtMaterial;
        }
    }
}
