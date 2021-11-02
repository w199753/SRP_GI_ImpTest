using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

using FloatParameter = UnityEngine.Rendering.PostProcessing.FloatParameter;
using BoolParameter = UnityEngine.Rendering.PostProcessing.BoolParameter;
using IntParameter = UnityEngine.Rendering.PostProcessing.IntParameter;
using TextureParameter = UnityEngine.Rendering.PostProcessing.TextureParameter;
using System;

namespace FPostProcessing
{
    [Serializable]
    [PostProcess(typeof(SSAORender), PostProcessEvent.AfterStack, "KirkPostProcessing/SSAO")]
    public sealed class SSAO : PostProcessEffectSettings
    {
        [Range(8, 64)]
        public IntParameter SampleCount = new IntParameter() { value = 32 };
    }

    public sealed class SSAORender : PostProcessEffectRenderer<SSAO>
    {
        private class ShaderPropertyID
        {
            public int SampleCount;

            public ShaderPropertyID()
            {

            }
        }
        private const string PROFILER_TAG = "F-SSAO";
        private System.Lazy<Shader> m_shader = new System.Lazy<Shader>(() => Shader.Find("F-PostProcessing/SSAO"));
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

            context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
            buffer.EndSample(PROFILER_TAG);
        }
    }

}
