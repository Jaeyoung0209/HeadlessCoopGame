using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DrawTargetObjectFeature : ScriptableRendererFeature
{
    class DrawTargetObjectPass : ScriptableRenderPass
    {
        public Renderer targetRenderer;
        public Material overrideMaterial;

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (targetRenderer == null || overrideMaterial == null) return;

            CommandBuffer cmd = CommandBufferPool.Get("DrawTargetObject");

            var drawSettings = new DrawingSettings(new ShaderTagId("UniversalForward"), new SortingSettings(renderingData.cameraData.camera))
            {
                perObjectData = PerObjectData.None,
            };
            drawSettings.overrideMaterial = overrideMaterial;

            var filterSettings = new FilteringSettings(RenderQueueRange.all);

            // Draw only the target renderer
            cmd.DrawRenderer(targetRenderer, overrideMaterial);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    public Renderer targetRenderer;
    public Material overrideMaterial;

    DrawTargetObjectPass _pass;

    public override void Create()
    {
        _pass = new DrawTargetObjectPass
        {
            targetRenderer = targetRenderer,
            overrideMaterial = overrideMaterial,
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(_pass);
    }
}
