using UnityEngine;

public class DynamicBeamGradient : MonoBehaviour
{
    public Renderer renderer;
    public MeshFilter meshFilter;

    public float baseFrequency = 2.5f;
    public float hueScrollSpeed = 0.25f;
    public float brightnessAmplitude = 2.0f;
    public float brightnessBase = 1.5f;

    private float localFrequency;
    private float phaseOffset;
    private float hueSeed;
    private float noiseOffset;
    private float timeOffset;
    private float hueShift = 0f;

    void Start()
    {
        phaseOffset = Random.Range(0f, Mathf.PI * 2f);
        hueSeed = Random.value;
        noiseOffset = Random.Range(0f, 1000f);
        timeOffset = Random.Range(0f, 20f);
        localFrequency = baseFrequency * Random.Range(0.5f, 1.6f);
        hueShift = Random.Range(0f, 1f);
    }

    void Update()
    {
        if (meshFilter == null || meshFilter.mesh == null) return;

        float time = Time.time + timeOffset;
        float noise = Mathf.PerlinNoise(noiseOffset, time * 0.5f);
        float dynamicFreq = localFrequency * (0.8f + noise * 0.6f);
        float pulse = Mathf.Sin(time * dynamicFreq + phaseOffset) * 0.5f + 0.5f;

        hueShift += Time.deltaTime * hueScrollSpeed * Random.Range(0.8f, 1.2f);

        var mesh = meshFilter.mesh;
        var verts = mesh.vertices;
        var colors = new Color[verts.Length];

        float topY = verts[0].y;
        float bottomY = verts[verts.Length - 1].y;

        for (int i = 0; i < verts.Length; i++)
        {
            float normalizedY = Mathf.InverseLerp(bottomY, topY, verts[i].y);
            float hue = (hueShift + hueSeed * 0.7f + normalizedY * 0.35f + noise * 0.2f) % 1f;
            float sat = 0.8f + 0.2f * Mathf.Sin(time * 0.7f + normalizedY * Mathf.PI * 2f);
            Color gradColor = Color.HSVToRGB(hue, sat, 1f);
            gradColor.a = Mathf.Lerp(0.2f, 1.0f, normalizedY);
            colors[i] = gradColor;
        }

        mesh.colors = colors;

        if (renderer != null && renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_EmissionColor"))
        {
            float brightness = brightnessBase + pulse * brightnessAmplitude;
            Color emissionColor = Color.HSVToRGB((hueShift + noise * 0.3f) % 1f, 0.9f, 1f) * brightness;
            renderer.sharedMaterial.SetColor("_EmissionColor", emissionColor);
        }
    }
}
