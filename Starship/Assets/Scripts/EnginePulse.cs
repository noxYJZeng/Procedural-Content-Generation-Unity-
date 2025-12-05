using UnityEngine;

public class EnginePulse : MonoBehaviour
{
    public int variant = 0;
    private Renderer[] renderers;
    private float baseIntensity = 5f;
    private float pulseSpeed = 2f;

    void Start()
    {
        renderers = GetComponentsInChildren<Renderer>(true);

        if (variant == 0) baseIntensity = 5f;
        else if (variant == 1) baseIntensity = 2.5f;
        else baseIntensity = 8f;
    }

    void Update()
    {
        float t = Time.time;
        float intensity = baseIntensity + Mathf.Sin(t * pulseSpeed) * (baseIntensity * 0.4f);

        foreach (var r in renderers)
        {
            if (r == null) continue;
            var mat = r.sharedMaterial;
            if (mat != null && mat.HasProperty("_EmissionColor"))
            {
                Color c = mat.color;
                mat.SetColor("_EmissionColor", c * intensity);
            }
        }
    }
}
