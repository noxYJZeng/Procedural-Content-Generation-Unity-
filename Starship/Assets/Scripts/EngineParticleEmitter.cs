using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class EngineParticleEmitter : MonoBehaviour
{
    public float pulseSpeed = 2.0f;
    public float minRate = 400f;
    public float maxRate = 1200f;
    public float minIntensity = 1f;
    public float maxIntensity = 3.5f;

    private ParticleSystem ps;
    private ParticleSystemRenderer pr;
    private Material mat;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        pr = GetComponent<ParticleSystemRenderer>();
        if (pr != null)
            mat = pr.sharedMaterial;
    }

    void Update()
    {
        if (ps == null) return;

        float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;

        var emission = ps.emission;
        emission.rateOverTime = Mathf.Lerp(minRate, maxRate, t);

        if (mat != null && mat.HasProperty("_EmissionColor"))
        {
            Color baseColor = mat.GetColor("_EmissionColor");
            mat.SetColor("_EmissionColor", baseColor * Mathf.Lerp(minIntensity, maxIntensity, t));
        }
    }
}
