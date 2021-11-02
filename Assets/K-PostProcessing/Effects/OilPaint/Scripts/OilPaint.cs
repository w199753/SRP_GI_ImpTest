using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using FloatParameter = UnityEngine.Rendering.PostProcessing.FloatParameter;
using BoolParameter = UnityEngine.Rendering.PostProcessing.BoolParameter;
using IntParameter = UnityEngine.Rendering.PostProcessing.IntParameter;
using TextureParameter = UnityEngine.Rendering.PostProcessing.TextureParameter;
namespace KPostProcessing
{
    [Serializable]
    [PostProcess(typeof(OilPaintRenderer), PostProcessEvent.AfterStack, "KirkPostProcessing/OilPaint")]
    public sealed class OilPaint : PostProcessEffectSettings
    {
        [Range(0.0f, 5.0f)]
        public FloatParameter Radius = new FloatParameter { value = 2f };

        [Range(0.0f, 5.0f)]
        public FloatParameter ResolutionValue = new FloatParameter { value = 1f };
    }
    public sealed class OilPaintRenderer : PostProcessEffectRenderer<OilPaint>
    {
        private const string PROFILER_TAG = "K-OilPaint";
        private Shader shader;

        #region Init
        public override void Init()
        {
            shader = Shader.Find("Hidden/K-PostProcessing/OilPaint");
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
            sheet.properties.SetFloat("_Radius", settings.Radius);
            sheet.properties.SetFloat("_ResolutionValue", settings.ResolutionValue);
            sheet.properties.SetFloat("_Width", context.screenWidth);
            sheet.properties.SetFloat("_Height", context.screenHeight);
            cmd.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
            cmd.EndSample(PROFILER_TAG);
        }
    }
}
