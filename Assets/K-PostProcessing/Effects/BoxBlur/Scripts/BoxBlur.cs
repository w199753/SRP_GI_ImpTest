using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using FloatParameter = UnityEngine.Rendering.PostProcessing.FloatParameter;
using BoolParameter = UnityEngine.Rendering.PostProcessing.BoolParameter;
using IntParameter = UnityEngine.Rendering.PostProcessing.IntParameter;
namespace KPostProcessing
{
    [Serializable]
    [PostProcess(typeof(BoxBlurRenderer), PostProcessEvent.AfterStack, "KirkPostProcessing/BoxBlur")]
    public sealed class BoxBlur : PostProcessEffectSettings
    {
        #region Public Properties
        [Range(0f, 5f)]
        public FloatParameter BlurRadius = new FloatParameter { value = 3f };

        [Range(1, 20)]
        public IntParameter Iteration = new IntParameter { value = 6 };

        [Range(1, 8)]
        public FloatParameter RTDownScaling = new FloatParameter { value = 2 };
        #endregion
    }

    public sealed class BoxBlurRenderer : PostProcessEffectRenderer<BoxBlur>
    {

        private const string PROFILER_TAG = "K-BoxBlur";
        private Shader shader;

        public override void Init()
        {
            shader = Shader.Find("Hidden/K-PostProcessing/BoxBlur");
        }

        public override void Release()
        {
            base.Release();
        }

        static class ShaderIDs
        {
            internal static readonly int BlurRadius = Shader.PropertyToID("_BlurOffset");
            internal static readonly int BufferRT1 = Shader.PropertyToID("_BufferRT1");
            internal static readonly int BufferRT2 = Shader.PropertyToID("_BufferRT2");
        }

        public override void Render(PostProcessRenderContext context)
        {
            CommandBuffer cmd = context.command;
            PropertySheet sheet = context.propertySheets.Get(shader);

            cmd.BeginSample(PROFILER_TAG);

            int RTWidth = (int)(context.screenWidth / settings.RTDownScaling);
            int RTHeight = (int)(context.screenHeight / settings.RTDownScaling);
            cmd.GetTemporaryRT(ShaderIDs.BufferRT1, RTWidth, RTHeight, 0, FilterMode.Bilinear);
            cmd.GetTemporaryRT(ShaderIDs.BufferRT2, RTWidth, RTHeight, 0, FilterMode.Bilinear);

            // downsample screen copy into smaller RT
            context.command.BlitFullscreenTriangle(context.source, ShaderIDs.BufferRT1);


            for (int i = 0; i < settings.Iteration; i++)
            {
                if (settings.Iteration > 20)
                {
                    return;
                }

                Vector4 BlurRadius = new Vector4(settings.BlurRadius / (float)context.screenWidth, settings.BlurRadius / (float)context.screenHeight, 0, 0);
                // RT1 -> RT2
                sheet.properties.SetVector(ShaderIDs.BlurRadius, BlurRadius);
                context.command.BlitFullscreenTriangle(ShaderIDs.BufferRT1, ShaderIDs.BufferRT2, sheet, 0);

                // RT2 -> RT1
                sheet.properties.SetVector(ShaderIDs.BlurRadius, BlurRadius);
                context.command.BlitFullscreenTriangle(ShaderIDs.BufferRT2, ShaderIDs.BufferRT1, sheet, 0);
            }

            // Render blurred texture in blend pass
            cmd.BlitFullscreenTriangle(ShaderIDs.BufferRT1, context.destination, sheet, 1);

            // release
            cmd.ReleaseTemporaryRT(ShaderIDs.BufferRT1);
            cmd.ReleaseTemporaryRT(ShaderIDs.BufferRT2);
            cmd.EndSample(PROFILER_TAG);
        }
    }
}