using UnityEngine;

public class FloatMotion : MonoBehaviour
{
    public float amplitude = 0.25f;
    public float frequency = 0.6f;
    public float phase = 0f;

    private Vector3 startLocalPos;

    void Start()
    {
        startLocalPos = transform.localPosition;
    }

    void Update()
    {
        float y = Mathf.Sin((Time.time + phase) * Mathf.PI * 2f * frequency) * amplitude;
        transform.localPosition = startLocalPos + Vector3.up * y;
    }
}
