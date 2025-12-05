 using UnityEngine;
using System.Linq;

[ExecuteAlways]
public class UFOGenerator : MonoBehaviour
{
    [Header("Textures (Shared Across All UFOs)")]
    public Texture2D[] availableTextures;

    [Header("UFO Parameters")]
    public int mainSeed = 0;
    public int ufoCount = 6;
    public GameObject ufoPrefab;

    [Header("Variation Ranges (min/max)")]
    public Vector2 scaleXZRange = new Vector2(0.75f, 1.30f);
    public Vector2 scaleYRange = new Vector2(0.60f, 1.70f);
    public Vector2 domeStretchYRange = new Vector2(0.85f, 1.90f);
    public float baseSpacing = 48f;
    public float yawJitterDeg = 12f;

    private int prevSeed;
    private int prevCount;
    private Vector2 prevXZ, prevY, prevDome;
    private float prevSpacing, prevYaw;
    private bool hasGeneratedInPlayMode = false;

    void OnEnable()
    {
        if (!Application.isPlaying)
            RefreshFleet(true);
    }

    void Update()
    {
        if (!Application.isPlaying)
        {
            if (mainSeed != prevSeed
                || ufoCount != prevCount
                || prevXZ != scaleXZRange
                || prevY != scaleYRange
                || prevDome != domeStretchYRange
                || Mathf.Abs(prevSpacing - baseSpacing) > 1e-4f
                || Mathf.Abs(prevYaw - yawJitterDeg) > 1e-4f)
            {
                RefreshFleet(true);
            }
        }
    }

    public void RefreshFleet(bool force = false)
    {
        if (Application.isPlaying) hasGeneratedInPlayMode = true;

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            Object.DestroyImmediate(child.gameObject);
        }

        System.Random rng = new System.Random(mainSeed);
        float maxXZ = Mathf.Max(Mathf.Abs(scaleXZRange.x), Mathf.Abs(scaleXZRange.y));
        float spacing = baseSpacing * Mathf.Max(1f, maxXZ);

        GameObject[] ufos = new GameObject[ufoCount];
        float[] seamHeights = new float[ufoCount];

        for (int i = 0; i < ufoCount; i++)
        {
            int localSeed = rng.Next();
            System.Random r = new System.Random(localSeed);

            Vector3 pos = new Vector3((i - (ufoCount - 1) / 2f) * spacing, 0f, 0f);
            GameObject ufo = new GameObject("UFO_" + i);
            ufo.transform.parent = transform;

            UFOFactory factory = ufo.AddComponent<UFOFactory>();
            factory.availableTextures = availableTextures;
            factory.BuildUFO(localSeed);

            float sxz = Lerp(scaleXZRange.x, scaleXZRange.y, (float)r.NextDouble());
            float sy = Lerp(scaleYRange.x, scaleYRange.y, (float)r.NextDouble());
            ufo.transform.localScale = new Vector3(sxz, sy, sxz);

            Transform dome = SafeFindDome(ufo.transform);
            if (dome != null)
            {
                float domeY = Lerp(domeStretchYRange.x, domeStretchYRange.y, (float)r.NextDouble());
                Vector3 ds = dome.localScale;
                dome.localScale = new Vector3(ds.x, ds.y * domeY, ds.z);
            }

            float yaw = (float)((r.NextDouble() * 2 - 1) * yawJitterDeg);
            ufo.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            ufo.transform.localPosition = pos;

            ufos[i] = ufo;

            float midY = EstimateSeamHeight(ufo);
            seamHeights[i] = midY;
        }

        float avgSeam = seamHeights.Average();
        for (int i = 0; i < ufoCount; i++)
        {
            Vector3 p = ufos[i].transform.localPosition;
            float delta = seamHeights[i] - avgSeam;
            p.y -= delta;
            ufos[i].transform.localPosition = p;
        }

        prevSeed = mainSeed;
        prevCount = ufoCount;
        prevXZ = scaleXZRange;
        prevY = scaleYRange;
        prevDome = domeStretchYRange;
        prevSpacing = baseSpacing;
        prevYaw = yawJitterDeg;
    }

    private static float Lerp(float a, float b, float t)
        => a + (b - a) * Mathf.Clamp01(t);

    private static Transform SafeFindDome(Transform root)
    {
        var t = root.Find("Dome");
        if (t != null) return t;
        return FindInChildrenByKeyword(root, new string[] { "dome", "cap", "top" });
    }

    private static Transform FindInChildrenByKeyword(Transform root, string[] keywords)
    {
        foreach (Transform c in root)
        {
            string n = c.name.ToLower();
            foreach (var k in keywords)
                if (n.Contains(k))
                    return c;
            var deep = FindInChildrenByKeyword(c, keywords);
            if (deep != null) return deep;
        }
        return null;
    }

    private static float EstimateSeamHeight(GameObject ufo)
    {
        MeshFilter[] filters = ufo.GetComponentsInChildren<MeshFilter>();
        if (filters == null || filters.Length == 0) return ufo.transform.position.y;

        float maxBottom = float.MinValue;
        float minTop = float.MaxValue;

        foreach (var mf in filters)
        {
            Mesh m = mf.sharedMesh;
            if (m == null) continue;

            foreach (var v in m.vertices)
            {
                Vector3 worldV = mf.transform.TransformPoint(v);
                if (worldV.y > maxBottom) maxBottom = worldV.y;
                if (worldV.y < minTop) minTop = worldV.y;
            }
        }
        return (maxBottom + minTop) / 2f;
    }
}
