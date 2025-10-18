using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class DrawTargetObjectFeature : ScriptableRendererFeature
{
    class DrawTargetObjectPass : ScriptableRenderPass
    {
        public Renderer targetRenderer;
        public Material overrideMaterial;

        private class PassData
        {
            public Renderer renderer;
            public Material material;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (targetRenderer == null || overrideMaterial == null)
                return;

            using (
                var builder = renderGraph.AddRasterRenderPass<PassData>(
                    "Draw Target Object",
                    out var passData
                )
            )
            {
                passData.renderer = targetRenderer;
                passData.material = overrideMaterial;

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture);

                builder.AllowPassCulling(false);

                builder.SetRenderFunc(
                    (PassData data, RasterGraphContext context) =>
                    {
                        if (data.renderer == null || data.material == null)
                            return;

                        context.cmd.DrawRenderer(data.renderer, data.material);
                    }
                );
            }
        }
    }

    public Renderer targetRenderer;
    public Material overrideMaterial;
    private DrawTargetObjectPass _pass;

    public override void Create()
    {
        _pass = new DrawTargetObjectPass
        {
            targetRenderer = targetRenderer,
            overrideMaterial = overrideMaterial,
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents,
        };
    }

    public override void AddRenderPasses(
        ScriptableRenderer renderer,
        ref RenderingData renderingData
    )
    {
        renderer.EnqueuePass(_pass);
    }
}
