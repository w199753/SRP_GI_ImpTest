using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using FloatParameter = UnityEngine.Rendering.PostProcessing.FloatParameter;
using BoolParameter = UnityEngine.Rendering.PostProcessing.BoolParameter;
using IntParameter = UnityEngine.Rendering.PostProcessing.IntParameter;
using TextureParameter = UnityEngine.Rendering.PostProcessing.TextureParameter;
using ColorParameter = UnityEngine.Rendering.PostProcessing.ColorParameter;
namespace KPostProcessing
{
    [Serializable]
    [PostProcess(typeof(ReliefRenderer), PostProcessEvent.AfterStack, "KirkPostProcessing/Relief")]
    public sealed class Relief : PostProcessEffectSettings
    {
        public ColorParameter Color = new ColorParameter { value = new Color(1.0f, 1.0f, 1.0f) };
    }
    public sealed class ReliefRenderer : PostProcessEffectRenderer<Relief>
    {
        private const string PROFILER_TAG = "K-Relief";
        private Shader shader;

        #region Init
        public override void Init()
        {
            shader = Shader.Find("Hidden/K-PostProcessing/Relief");
        }
        #endregion

        #region Release
        public override void Release()
        {
            base.Release();
        }
        #endregion


        public override void Render(PostProcessRenderContext context)
        {
            CommandBuffer cmd = context.command;
            PropertySheet sheet = context.propertySheets.Get(shader);
            cmd.BeginSample(PROFILER_TAG);
            sheet.properties.SetColor("_SetColor", settings.Color);
            cmd.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
            cmd.EndSample(PROFILER_TAG);
        }
    }
}
