using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public abstract class FRenderAssetBase : ScriptableObject
{
    private Dictionary<Camera, FRenderPass> perCameraPass = new Dictionary<Camera, FRenderPass>();

    public abstract FRenderPass CreateRenderPass();
    public FRenderPass GetRenderPass(Camera camera)
    {
        if (!perCameraPass.ContainsKey(camera))
            perCameraPass[camera] = CreateRenderPass();
        return perCameraPass[camera];
    }
}

public abstract class FRenderPass
{
    [NonSerialized]
    protected ScriptableRenderContext context;
    [NonSerialized]
    protected Camera camera;
    [NonSerialized]
    protected RenderingData renderingData;
    public virtual void Setup(ScriptableRenderContext context, Camera camera, RenderingData renderingData)
    {
        this.context = context;
        this.camera = camera;
        this.renderingData = renderingData;
    }

    public virtual void Render()
    {

    }

    public virtual void Cleanup()
    {

    }
}

public abstract class FRenderPassRender : FRenderPass
{
    protected FRenderAssetBase asset { get; private set; }
    public FRenderPassRender(FRenderAssetBase asset)
    {
        this.asset = asset;
    }
}
