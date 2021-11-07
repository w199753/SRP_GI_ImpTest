using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

using FloatParameter = UnityEngine.Rendering.PostProcessing.FloatParameter;
using BoolParameter = UnityEngine.Rendering.PostProcessing.BoolParameter;
using IntParameter = UnityEngine.Rendering.PostProcessing.IntParameter;
using TextureParameter = UnityEngine.Rendering.PostProcessing.TextureParameter;

namespace FPostProcessing
{
    [Serializable]
    [PostProcess(typeof(HBAORender), PostProcessEvent.AfterStack, "KirkPostProcessing/HBAO")]
    public sealed class HBAO : PostProcessEffectSettings
    {
        [Range(8, 64)]
        public IntParameter SampleCount = new IntParameter() { value = 32 };
        [Range(8,32)]
        public IntParameter ThicknessStrength = new IntParameter(){value = 1};
        public BoolParameter OnlyShowAO = new BoolParameter(){value = false};
        //[Range(0.001f,4.5f)]
        //public FloatParameter  = new FloatParameter(){value = 1};
        //[Range(0.0001f,0.05f)]
        //public FloatParameter S = new FloatParameter(){value = 0.0023f};
    }

    public sealed class HBAORender : PostProcessEffectRenderer<HBAO>
    {
        private class ShaderPropertyID
        {
            public int SampleCount;
            public int OnlyShowAO;
            public int ThicknessStrength;

            public ShaderPropertyID()
            {
                SampleCount = Shader.PropertyToID("_SampleCount");
                OnlyShowAO = Shader.PropertyToID("_OnlyShowAO");
                ThicknessStrength = Shader.PropertyToID("_ThicknessStrength");
            }
        }
        private const string PROFILER_TAG = "F-HBAO";
        private System.Lazy<Shader> m_shader = new System.Lazy<Shader>(() => Shader.Find("F-PostProcessing/HBAO"));
        private readonly ShaderPropertyID m_shaderPropertyID = new ShaderPropertyID();

        public override void Init()
        {
            base.Init();
        }

        public override void Release()
        {
            base.Release();
        }
        public override void Render(PostProcessRenderContext context)
        {
            CommandBuffer buffer = context.command;
            PropertySheet sheet = context.propertySheets.Get(m_shader.Value);
            buffer.BeginSample(PROFILER_TAG);

            sheet.properties.SetInt(m_shaderPropertyID.SampleCount, settings.SampleCount);
            sheet.properties.SetInt(m_shaderPropertyID.OnlyShowAO,settings.OnlyShowAO == true ? 1: 0);
            sheet.properties.SetInt(m_shaderPropertyID.ThicknessStrength,settings.ThicknessStrength);
            context.command.SetGlobalMatrix(Shader.PropertyToID("_InvProject"),GL.GetGPUProjectionMatrix(context.camera.projectionMatrix,false).inverse);
            context.command.SetGlobalMatrix(Shader.PropertyToID("_InvView"),context.camera.worldToCameraMatrix.inverse);
            context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
            buffer.EndSample(PROFILER_TAG);
        }
    }

}
