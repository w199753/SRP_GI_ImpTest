using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Experimental.Rendering;

namespace FPostProcessing
{
    public class PostEffectRenderPass
    {
        private PostEffectRenderPass()
        {
            postProcessContext = new PostProcessRenderContext();
        }
        private static PostEffectRenderPass _instance;
        public static PostEffectRenderPass Instance
        {
            get
            {
                return _instance == null ? new PostEffectRenderPass() : _instance;
            }
        }
        private class ShaderPropertyID
        {
            public int dest;
            public int source;
            public ShaderPropertyID()
            {
                dest = Shader.PropertyToID("_DestImage");
                source = Shader.PropertyToID("_SrcImage");
            }
        }
        private const string BUFFER_NAME = "PostEffectPass";
        private static readonly ShaderPropertyID shaderPropertyID = new ShaderPropertyID();
        private CommandBuffer buffer = new CommandBuffer() { name = BUFFER_NAME };
        ScriptableRenderContext context;
        Camera camera;
        RenderingData renderingData;
        PostProcessRenderContext postProcessContext;

        readonly RenderTargetIdentifier screenSrcImageID = new RenderTargetIdentifier(shaderPropertyID.source);
        readonly RenderTargetIdentifier screenDestImageID = new RenderTargetIdentifier(shaderPropertyID.dest);


        public void ExecuteRender(ref ScriptableRenderContext context, Camera camera, ref RenderingData renderingData)
        {
            this.context = context;
            this.camera = camera;
            this.renderingData = renderingData;
            Render();
        }

        public void Render()
        {
            buffer.BeginSample(BUFFER_NAME);
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();

            CopyCameraTargetToFrameBuffer();

            RenderPostEffect();

            buffer.Blit(screenDestImageID, renderingData.ColorTarget);

            buffer.ReleaseTemporaryRT(shaderPropertyID.dest);

            buffer.EndSample(BUFFER_NAME);
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        void CopyCameraTargetToFrameBuffer()
        {
            // buffer.GetTemporaryRT(ShaderIDs.Dummy, Screen.width, Screen.height, 0, FilterMode.Point, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            // buffer.SetGlobalVector(ShaderIDs.BlitViewport, new Vector4(camera.rect.width, camera.rect.height, camera.rect.xMin, camera.rect.yMin));
            // buffer.Blit(ShaderIDs._CameraDepthTexture, BuiltinRenderTextureType.CameraTarget, UtilityShader.material, (int)UtilityShader.Pass.DepthCopyViewport);
            // buffer.Blit(BuiltinRenderTextureType.CameraTarget, ShaderIDs.Dummy);
            // buffer.Blit(ShaderIDs.Dummy, ShaderIDs.FrameBuffer, UtilityShader.material, (int)UtilityShader.Pass.GrabCopy);
            // buffer.ReleaseTemporaryRT(ShaderIDs.Dummy);
            // renderContext.ExecuteCommandBuffer(_command);
            // buffer.Clear();
            buffer.GetTemporaryRT(shaderPropertyID.dest, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Point, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            buffer.GetTemporaryRT(shaderPropertyID.source, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Point, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);

            buffer.Blit(renderingData.ColorTarget, screenSrcImageID);
            buffer.Blit(screenSrcImageID, screenDestImageID, MaterialPool.GetMaterial("FRP/CopyRT"), 0);


            buffer.ReleaseTemporaryRT(shaderPropertyID.source);

            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }
        public void RenderPostEffect()
        {
            // var layer = camera.GetComponent<PostProcessLayer>();

            // if (layer == null || !layer.isActiveAndEnabled) return;

            // _postProcessRenderContext.Reset();
            // _postProcessRenderContext.camera = camera;
            // _postProcessRenderContext.command = _command;
            // _postProcessRenderContext.destination = ShaderIDs.FrameBuffer;
            // _postProcessRenderContext.source = ShaderIDs.Dummy;
            // _postProcessRenderContext.sourceFormat = RenderTextureFormat.ARGBHalf;

            // _command.BeginSample(_samplePostProcessRender);
            // _command.GetTemporaryRT(ShaderIDs.Dummy, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Point, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            // _command.Blit(ShaderIDs.FrameBuffer, ShaderIDs.Dummy);
            // layer.Render(_postProcessRenderContext);
            // _command.ReleaseTemporaryRT(ShaderIDs.Dummy);
            // _command.EndSample(_samplePostProcessRender);
            // renderContext.ExecuteCommandBuffer(_command);
            // _command.Clear();

            var postLayer = camera.GetComponent<PostProcessLayer>();
            if (postLayer == null || !postLayer.isActiveAndEnabled) return;
            postProcessContext.Reset();
            postProcessContext.camera = camera;
            postProcessContext.command = buffer;
            postProcessContext.destination = shaderPropertyID.dest;
            postProcessContext.source = shaderPropertyID.source;
            postProcessContext.sourceFormat = RenderTextureFormat.ARGBHalf;

            // _command.BeginSample(_samplePostProcessRender);
            buffer.GetTemporaryRT(shaderPropertyID.source, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Point, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            buffer.Blit(shaderPropertyID.dest, shaderPropertyID.source);
            postLayer.Render(postProcessContext);
            buffer.ReleaseTemporaryRT(shaderPropertyID.source);
            // _command.EndSample(_samplePostProcessRender);
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }
    }

}
