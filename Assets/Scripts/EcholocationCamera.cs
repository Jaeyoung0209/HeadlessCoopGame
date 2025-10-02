using UnityEngine;
using System.Collections.Generic;

public class EcholocationCamera : MonoBehaviour
{
    [Header("Material & Scan Settings")]
    public Material echoMaterial;
    public Transform scanOrigin;
    public float expandSpeed = 5f;
    public float maxRadius = 10f;
    public float threshold = 0.03f;

    private class Sphere
    {
        public Vector3 center;
        public float radius;
        public Sphere(Vector3 c) { center = c; radius = 0; }
    }

    private List<Sphere> spheres = new List<Sphere>();
    private const int maxSpheres = 10;

    void Update()
    {
        // Start new ripple
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (spheres.Count < maxSpheres)
                spheres.Add(new Sphere(scanOrigin.position));
        }

        // Animate ripples
        for (int i = spheres.Count - 1; i >= 0; i--)
        {
            Sphere s = spheres[i];
            s.radius += expandSpeed * Time.deltaTime;
            if (s.radius > maxRadius)
                spheres.RemoveAt(i);
        }

        // Send data to shader
        Vector4[] centers = new Vector4[maxSpheres];
        float[] radii = new float[maxSpheres];

        for (int i = 0; i < spheres.Count; i++)
        {
            centers[i] = spheres[i].center;
            radii[i] = spheres[i].radius;
        }

        // Fill unused slots with zeros
        for (int i = spheres.Count; i < maxSpheres; i++)
        {
            centers[i] = Vector4.zero;
            radii[i] = 0;
        }

        echoMaterial.SetVectorArray("_SphereCenters", centers);
        echoMaterial.SetFloatArray("_SphereRadii", radii);
        echoMaterial.SetInt("_SphereCount", spheres.Count);
        echoMaterial.SetFloat("_Threshold", threshold);
    }
}
