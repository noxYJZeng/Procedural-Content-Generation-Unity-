using UnityEngine;

public class UnderseaRockGenerator : MonoBehaviour
{
    public int rockCount = 6;
    public Vector2 minMaxScale = new Vector2(1f, 6f);
    public int seed = 123;

    private System.Random rand;

    void Start()
    {
        rand = new System.Random(seed);
        GenerateRocks();
    }

    void GenerateRocks()
    {
        if (FlockManager.Instance == null)
        {
            Debug.LogError("FlockManager missing — cannot generate rocks.");
            return;
        }

        Vector3 half = FlockManager.Instance.worldSize * 0.5f;

        int obstacleLayer = LayerMask.NameToLayer("Obstacle");
        if (obstacleLayer < 0)
        {
            Debug.LogWarning("Layer 'Obstacle' not found, using Default layer.");
            obstacleLayer = 0;
        }

        for (int i = 0; i < rockCount; i++)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = "Rock_" + i;
            g.transform.SetParent(transform);

            Vector3 pos = new Vector3(
                RandomRange(-half.x * 0.7f, half.x * 0.7f),
                RandomRange(-half.y * 0.7f, half.y * 0.7f),
                RandomRange(-half.z * 0.7f, half.z * 0.7f)
            );
            g.transform.position = pos;

            float baseScale = RandomRange(minMaxScale.x, minMaxScale.y);

            bool makeBeam = rand.NextDouble() < 0.25;

            if (makeBeam)
            {
                int axis = rand.Next(0, 3);
                float longSize = baseScale * RandomRange(1.5f, 3.0f);
                float mid = baseScale * RandomRange(0.6f, 1.0f);
                float shortSize = baseScale * RandomRange(0.5f, 0.8f);

                if (axis == 0)
                    g.transform.localScale = new Vector3(longSize, mid, shortSize);
                else if (axis == 1)
                    g.transform.localScale = new Vector3(mid, longSize, shortSize);
                else
                    g.transform.localScale = new Vector3(mid, shortSize, longSize);
            }
            else
            {
                g.transform.localScale = new Vector3(
                    baseScale * RandomRange(0.7f, 1.3f),
                    baseScale * RandomRange(0.5f, 1.2f),
                    baseScale * RandomRange(0.7f, 1.3f)
                );
            }

            Renderer r = g.GetComponent<Renderer>();
            r.material.color = new Color(0.8f, 0.4f, 0.25f);

            BoxCollider bc = g.GetComponent<BoxCollider>();
            if (bc == null) bc = g.AddComponent<BoxCollider>();
            bc.isTrigger = false;

            Rigidbody rb = g.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            g.layer = obstacleLayer;
        }
    }

    float RandomRange(float a, float b)
    {
        return a + (float)rand.NextDouble() * (b - a);
    }
}
