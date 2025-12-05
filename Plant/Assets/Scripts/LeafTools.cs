using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

public static class PrettyLeafMesh
{
    public static Mesh BuildLeaf(
        float length = 0.12f,
        float maxWidth = 0.06f,
        int segAlong = 12,
        int segAcrossHalf = 6,
        float camber = 0.15f,
        float twistDeg = 12f,
        float tipSharpness = 1.25f,
        float baseTaper = 0.35f,
        float midribThickness = 0.003f
    )
    {
        segAlong = Mathf.Max(2, segAlong);
        segAcrossHalf = Mathf.Max(2, segAcrossHalf);

        int across = segAcrossHalf * 2 + 1;
        int rings = segAlong + 1;

        var verts = new List<Vector3>(across * rings * 2);
        var norms = new List<Vector3>(verts.Capacity);
        var uvs = new List<Vector2>(verts.Capacity);
        var tris = new List<int>((rings - 1) * (across - 1) * 6 * 2);

        float TwistRad(float t) => Mathf.Deg2Rad * twistDeg * Mathf.Pow(t, 1.1f);
        float HalfWidth(float t)
        {
            float arch = Mathf.Pow(4f * t * (1f - t), tipSharpness);
            float baseShrink = Mathf.Lerp(baseTaper, 1f, t);
            return 0.5f * maxWidth * arch * baseShrink;
        }
        float Camber(float xNorm, float t)
        {
            float edgeFalloff = 1f - Mathf.Pow(Mathf.Abs(xNorm), 1.2f);
            float tipFall = Mathf.Lerp(1f, 0.4f, Mathf.Pow(t, 1.1f));
            return camber * edgeFalloff * tipFall;
        }

        for (int i = 0; i < rings; i++)
        {
            float t = i / (float)(rings - 1);
            float z = t * length;
            float hw = HalfWidth(t);
            float twist = TwistRad(t);
            float cs = Mathf.Cos(twist);
            float sn = Mathf.Sin(twist);

            for (int j = 0; j < across; j++)
            {
                float xn = Mathf.Lerp(-1f, 1f, j / (float)(across - 1));
                float x = xn * hw;
                float rx = cs * x;
                float ry = sn * x;
                float y = Camber(xn, t) * length;
                float midWeight = Mathf.Exp(-Mathf.Pow(xn * 2f, 2f) * 3f);
                y += midWeight * midribThickness;
                verts.Add(new Vector3(rx, y, z));
                uvs.Add(new Vector2(Mathf.InverseLerp(-hw, hw, x), t));
                Vector3 n = new Vector3(-0.15f * xn, 1f, -0.25f).normalized;
                n = new Vector3(cs * n.x - sn * n.y, sn * n.x + cs * n.y, n.z).normalized;
                norms.Add(n);
            }
        }

        int stride = across;
        for (int i = 0; i < rings - 1; i++)
        {
            for (int j = 0; j < across - 1; j++)
            {
                int a = i * stride + j;
                int b = (i + 1) * stride + j;
                int c = (i + 1) * stride + j + 1;
                int d = i * stride + j + 1;
                tris.Add(a); tris.Add(c); tris.Add(b);
                tris.Add(a); tris.Add(d); tris.Add(c);
            }
        }

        int frontCount = verts.Count;
        for (int i = 0; i < frontCount; i++)
        {
            verts.Add(verts[i]);
            uvs.Add(uvs[i]);
            norms.Add(-norms[i]);
        }
        int baseIndex = frontCount;
        for (int i = 0; i < rings - 1; i++)
        {
            for (int j = 0; j < across - 1; j++)
            {
                int a = baseIndex + i * stride + j;
                int b = baseIndex + (i + 1) * stride + j;
                int c = baseIndex + (i + 1) * stride + j + 1;
                int d = baseIndex + i * stride + j + 1;
                tris.Add(a); tris.Add(b); tris.Add(c);
                tris.Add(a); tris.Add(c); tris.Add(d);
            }
        }

        var m = new Mesh();
        m.indexFormat = (verts.Count > 65000)
            ? UnityEngine.Rendering.IndexFormat.UInt32
            : UnityEngine.Rendering.IndexFormat.UInt16;
        m.SetVertices(verts);
        m.SetNormals(norms);
        m.SetUVs(0, uvs);
        m.SetTriangles(tris, 0);
        m.RecalculateBounds();
        return m;
    }
}

public class LeafCanopyDecorator : MonoBehaviour
{
    [Header("Materials")]
    public Material leafMaterial;

    [Header("Distribution")]
    [Range(0f, 1f)] public float vMin = 0.50f;
    [Range(0f, 1f)] public float vMax = 0.95f;
    public int leavesPerTipRef = 20;

    [Header("Order Coverage")]
    public bool coverAllOrders = true;
    [Range(0, 6)] public int minCoveredOrder = 1;
    [Range(1, 6)] public int includeOrdersBack = 3;
    public AnimationCurve orderWeight = new AnimationCurve(
        new Keyframe(0.00f, 1.00f),
        new Keyframe(0.50f, 0.65f),
        new Keyframe(1.00f, 0.40f)
    );
    [Range(0f, 0.3f)] public float relaxVMinForInner = 0.15f;

    [Header("Per-Branch Guarantee")]
    public bool ensureEveryBranch = true;
    [Range(1, 12)] public int perBranchGuarantee = 3;
    [Range(0f, 1f)] public float perBranchFallbackVMin = 0.30f;
    [Range(0f, 1f)] public float perBranchFallbackVMax = 0.98f;
    public bool shuffleBranches = true;

    [Header("Global Limits")]
    public int maxLeavesTotal = 4000;
    public int minLeavesTotal = 1600;
    [Range(0f, 1f)] public float fallbackVMin = 0.35f;
    [Range(0f, 1f)] public float fallbackVMax = 0.98f;

    [Header("Leaf Shape")]
    public Vector2 leafSizeRange = new Vector2(0.25f, 0.40f);
    [Range(0f, 0.5f)] public float camber = 0.16f;
    [Range(0f, 25f)] public float twistDeg = 9f;
    [Range(0.8f, 1.8f)] public float tipSharpness = 1.25f;
    [Range(0.1f, 0.8f)] public float baseTaper = 0.35f;
    [Range(0f, 0.006f)] public float midribThickness = 0.0025f;

    [Header("Size Modulation")]
    public AnimationCurve sizeAlongV = new AnimationCurve(
        new Keyframe(0.00f, 0.95f),
        new Keyframe(0.45f, 1.15f),
        new Keyframe(1.00f, 0.85f)
    );
    public AnimationCurve sizeByOrder = new AnimationCurve(
        new Keyframe(0.00f, 1.05f),
        new Keyframe(1.00f, 0.92f)
    );

    [Header("Color Modulation")]
    public Gradient colorAlongV = DefaultAlongV();
    public Gradient colorByOrder = DefaultByOrder();
    [Range(0f, 1f)] public float colorBlend = 0.6f;

    [Header("Base→Tip Gradient")]
    public bool useBaseTipGradient = true;
    public Gradient baseTipGradient = DefaultBaseTip();
    [Range(0f, 1f)] public float baseTipWeight = 0.85f;

    [Range(0f, 0.08f)] public float hueJitter = 0.02f;
    [Range(0f, 0.25f)] public float satJitter = 0.10f;
    [Range(0f, 0.25f)] public float valJitter = 0.10f;

    [Header("Orientation / Jitter")]
    [Range(0f, 1f)] public float upFacing = 0.35f;
    [Range(0f, 30f)] public float randPitch = 10f;
    [Range(0f, 120f)] public float randYaw = 80f;
    [Range(0f, 12f)] public float randRoll = 8f;

    [Header("Global Scale")]
    public float globalSize = 1.2f;

    const string CONTAINER = "__LeavesContainer";

    [ContextMenu("Decorate Leaves")]
    public void DecorateNow()
    {
        if (leafMaterial == null) { Debug.LogWarning("LeafCanopyDecorator: please assign leafMaterial."); return; }

        var old = transform.Find(CONTAINER);
        if (old != null) DestroyImmediate(old.gameObject);
        var root = new GameObject(CONTAINER).transform;
        root.SetParent(transform, false);

        var branches = new List<(int order, Transform tf, Mesh mesh)>();
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            if (!t.name.StartsWith("Branch_o")) continue;
            if (t.TryGetComponent<MeshFilter>(out var mf) && mf.sharedMesh != null)
                branches.Add((ParseOrder(t.name), t, mf.sharedMesh));
        }
        if (branches.Count == 0) { Debug.LogWarning("LeafCanopyDecorator: no Branch_oX found."); return; }

        int maxOrder = branches.Max(b => b.order);
        int minOrderIncluded = Mathf.Max(useBaseTipGradient ? 1 : 0,
                                         coverAllOrders ? minCoveredOrder : maxOrder - (includeOrdersBack - 1));

        branches = branches.Where(b => b.order >= minOrderIncluded).ToList();

        const uint GOLDEN = 0x9E3779B9u;
        System.Random rng = new System.Random(unchecked(transform.GetInstanceID() ^ (int)GOLDEN));
        if (shuffleBranches) branches = branches.OrderBy(_ => rng.Next()).ToList();

        const float GOLD = 137.50776405f * Mathf.Deg2Rad;
        int total = 0;
        var placedPerBranch = new Dictionary<Transform, int>();

        if (ensureEveryBranch)
        {
            foreach (var (order, tf, mesh) in branches)
            {
                if (total >= maxLeavesTotal) break;
                if (order == 0) continue;
                var poolLoose = FilterByV(mesh.uv, perBranchFallbackVMin, perBranchFallbackVMax);
                if (poolLoose.Count == 0) continue;
                int can = Mathf.Min(perBranchGuarantee, maxLeavesTotal - total);
                total += PlaceLeavesOnBranch(root, tf, mesh, poolLoose, can, order, maxOrder, rng, GOLD, placedPerBranch, true);
            }
        }

        var perBranchTarget = new Dictionary<Transform, int>();
        foreach (var (order, tf, mesh) in branches)
        {
            float tInner = (minOrderIncluded == maxOrder) ? 0f : Mathf.InverseLerp(maxOrder, minOrderIncluded, order);
            float wOrder = Mathf.Clamp01(orderWeight.Evaluate(tInner));
            int want = Mathf.Clamp(Mathf.RoundToInt(leavesPerTipRef * wOrder), 0, 64);
            perBranchTarget[tf] = want;
        }

        bool placedInRound = true;
        while (placedInRound && total < maxLeavesTotal)
        {
            placedInRound = false;
            foreach (var (order, tf, mesh) in branches)
            {
                if (total >= maxLeavesTotal) break;
                float tInner = (minOrderIncluded == maxOrder) ? 0f : Mathf.InverseLerp(maxOrder, minOrderIncluded, order);
                float thisVMin = Mathf.Clamp01(vMin - relaxVMinForInner * tInner);

                var pool = FilterByV(mesh.uv, thisVMin, vMax);
                if (pool.Count == 0) continue;

                int already = placedPerBranch.TryGetValue(tf, out var a) ? a : 0;
                int want = perBranchTarget[tf];
                if (already >= Mathf.Max(perBranchGuarantee, want)) continue;

                int made = PlaceLeavesOnBranch(root, tf, mesh, pool, 1, order, maxOrder, rng, GOLD, placedPerBranch, true);
                if (made > 0) { total += made; placedInRound = true; }
            }
        }

        if (total < minLeavesTotal)
        {
            int target = Mathf.Min(maxLeavesTotal, minLeavesTotal);
            foreach (var (order, tf, mesh) in branches)
            {
                if (total >= target) break;
                var pool = FilterByV(mesh.uv, fallbackVMin, fallbackVMax);
                if (pool.Count == 0) continue;
                int made = PlaceLeavesOnBranch(root, tf, mesh, pool, 2, order, maxOrder, rng, GOLD, placedPerBranch, false);
                total += made;
            }
        }

        Debug.Log($"LeafCanopyDecorator: generated {total} leaves.");
    }

    int PlaceLeavesOnBranch(
        Transform root, Transform tf, Mesh mesh, List<int> pool,
        int count, int order, int maxOrder,
        System.Random rng, float GOLD,
        Dictionary<Transform, int> placedPerBranch,
        bool biasToTip)
    {
        var verts = mesh.vertices; var norms = mesh.normals; var uvs = mesh.uv;
        int made = 0;
        placedPerBranch.TryGetValue(tf, out int already);
        float vLo = pool.Min(i => uvs[i].y);
        float vHi = pool.Max(i => uvs[i].y);

        for (int k = 0; k < count; k++)
        {
            float r = (float)rng.NextDouble();
            float tBias = biasToTip ? Mathf.Pow(r, 0.6f) : r;
            float vTarget = Mathf.Lerp(vLo, vHi, tBias);
            float ang = (already + k) * GOLD;
            float uTarget = Mathf.Repeat(ang / (2f * Mathf.PI), 1f);
            int best = FindClosestUV(pool, uvs, uTarget, vTarget);
            if (best < 0) break;
            SpawnLeaf(root, tf, verts[best], norms[best], uvs[best].y, maxOrder, order, rng, out bool ok);
            if (ok) made++;
        }
        if (made > 0) placedPerBranch[tf] = already + made;
        return made;
    }

    void SpawnLeaf(Transform root, Transform tf, Vector3 vLocal, Vector3 nLocal, float vUV,
                   int maxOrder, int order, System.Random rng, out bool success)
    {
        success = false;
        Vector3 n = (nLocal.sqrMagnitude > 1e-6f) ? nLocal.normalized : Vector3.up;
        Vector3 upN = (Vector3.Slerp(n, tf.InverseTransformDirection(Vector3.up), upFacing)).normalized;
        Vector3 tan = Vector3.Cross(n, Vector3.up);
        if (tan.sqrMagnitude < 1e-6f) tan = Vector3.right;
        tan.Normalize();

        float sRand = (float)rng.NextDouble();
        Vector3 posLocal = vLocal + upN * (0.01f * sRand);

        float orderNorm = (maxOrder == 0) ? 1f : Mathf.InverseLerp(Mathf.Max(0, maxOrder - 5), maxOrder, order);
        float sizeBase = Mathf.Lerp(leafSizeRange.x, leafSizeRange.y, (float)rng.NextDouble());
        float sizeV = SafeCurve(sizeAlongV, Mathf.Clamp01(vUV), 1f);
        float sizeO = SafeCurve(sizeByOrder, orderNorm, 1f);
        float leafSize = sizeBase * sizeV * sizeO * Mathf.Max(0.01f, globalSize);

        Quaternion face = Quaternion.LookRotation(Vector3.Cross(upN, tan), upN);
        Quaternion jitter = Quaternion.Euler(
            RandRange(rng, -randPitch, randPitch),
            RandRange(rng, -randYaw, randYaw),
            RandRange(rng, -randRoll, randRoll));

        var go = new GameObject("Leaf");
        go.transform.SetParent(root, false);
        go.transform.position = tf.TransformPoint(posLocal);
        go.transform.rotation = tf.rotation * (jitter * face);
        go.transform.localScale = Vector3.one * leafSize;

        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        mf.sharedMesh = PrettyLeafMesh.BuildLeaf(
            length: leafSize * 1.0f,
            maxWidth: leafSize * 0.55f,
            segAlong: 10,
            segAcrossHalf: 6,
            camber: camber,
            twistDeg: twistDeg,
            tipSharpness: tipSharpness,
            baseTaper: baseTaper,
            midribThickness: midribThickness
        );
        mr.sharedMaterial = leafMaterial;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        Color cV = colorAlongV.Evaluate(Mathf.Clamp01(vUV));
        Color cO = colorByOrder.Evaluate(orderNorm);
        Color baseMix = Color.Lerp(cO, cV, colorBlend);
        Color cBT = useBaseTipGradient ? baseTipGradient.Evaluate(Mathf.Clamp01(vUV)) : baseMix;
        Color c = Color.Lerp(baseMix, cBT, Mathf.Clamp01(baseTipWeight));

        Color.RGBToHSV(c, out float H, out float S, out float V);
        H = Mathf.Repeat(H + RandRange(rng, -hueJitter, hueJitter), 1f);
        S = Mathf.Clamp01(S * (1f + RandRange(rng, -satJitter, satJitter)));
        V = Mathf.Clamp01(V * (1f + RandRange(rng, -valJitter, valJitter)));
        Color outColor = Color.HSVToRGB(H, S, V, true);

        var mpb = new MaterialPropertyBlock();
        if (mr.sharedMaterial.HasProperty("_BaseColor")) mpb.SetColor("_BaseColor", outColor);
        if (mr.sharedMaterial.HasProperty("_Color")) mpb.SetColor("_Color", outColor);
        if (mr.sharedMaterial.HasProperty("_TintColor")) mpb.SetColor("_TintColor", outColor);
        mr.SetPropertyBlock(mpb);
        success = true;
    }

    static List<int> FilterByV(Vector2[] uv, float vLo, float vHi)
    {
        var list = new List<int>(uv.Length / 4);
        for (int i = 0; i < uv.Length; i++)
        {
            float v = uv[i].y;
            if (v >= vLo && v <= vHi) list.Add(i);
        }
        return list;
    }

    static int FindClosestUV(List<int> pool, Vector2[] uv, float u, float v)
    {
        int best = -1; float bestD = float.MaxValue;
        for (int i = 0; i < pool.Count; i++)
        {
            int idx = pool[i];
            float du = Mathf.Abs(uv[idx].x - u); du = Mathf.Min(du, 1f - du);
            float dv = Mathf.Abs(uv[idx].y - v);
            float d = du * du * 0.6f + dv * dv;
            if (d < bestD) { bestD = d; best = idx; }
        }
        return best;
    }

    static int ParseOrder(string name)
    {
        int i = name.IndexOf('o');
        if (i >= 0 && i + 1 < name.Length)
        {
            string s = new string(name.Skip(i + 1).TakeWhile(char.IsDigit).ToArray());
            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
                return v;
        }
        return -1;
    }

    static float RandRange(System.Random r, float a, float b) => a + (float)r.NextDouble() * (b - a);
    static float SafeCurve(AnimationCurve c, float t, float fallback)
        => (c == null) ? fallback : c.Evaluate(Mathf.Clamp01(t));

    static Gradient DefaultAlongV()
    {
        var g = new Gradient();
        g.SetKeys(
            new[]{
                new GradientColorKey(new Color(0.13f,0.42f,0.18f), 0f),
                new GradientColorKey(new Color(0.18f,0.55f,0.22f), 0.5f),
                new GradientColorKey(new Color(0.28f,0.70f,0.30f), 1f),
            },
            new[]{ new GradientAlphaKey(1f,0f), new GradientAlphaKey(1f,1f) }
        );
        return g;
    }
    static Gradient DefaultByOrder()
    {
        var g = new Gradient();
        g.SetKeys(
            new[]{
                new GradientColorKey(new Color(0.16f,0.50f,0.22f), 0f),
                new GradientColorKey(new Color(0.20f,0.68f,0.28f), 1f),
            },
            new[]{ new GradientAlphaKey(1f,0f), new GradientAlphaKey(1f,1f) }
        );
        return g;
    }
    static Gradient DefaultBaseTip()
    {
        var g = new Gradient();
        g.SetKeys(
            new[]{
                new GradientColorKey(new Color(0.10f,0.35f,0.15f), 0f),
                new GradientColorKey(new Color(0.22f,0.65f,0.25f), 0.6f),
                new GradientColorKey(new Color(0.32f,0.78f,0.28f), 1f),
            },
            new[]{ new GradientAlphaKey(1f,0f), new GradientAlphaKey(1f,1f) }
        );
        return g;
    }
}
