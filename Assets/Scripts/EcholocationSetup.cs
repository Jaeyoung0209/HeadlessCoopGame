using UnityEngine;
using UnityEngine.Rendering.Universal;

public class EcholocationSetup : MonoBehaviour
{
    public UniversalRendererData rendererData; // your URP renderer asset
    public Renderer movingTarget;

    void Start()
    {
        foreach (var feature in rendererData.rendererFeatures)
        {
            if (feature is DrawTargetObjectFeature targetFeature)
            {
                targetFeature.targetRenderer = movingTarget; // assign at runtime
            }
        }
    }
}
