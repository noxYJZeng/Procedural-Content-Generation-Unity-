using System.Collections.Generic;
using UnityEngine;

public class ProceduralPlant : MonoBehaviour
{
    public struct Settings
    {
        public int seed;
        public int radialSegments;
        public int curveSamples;
        public float trunkHeight;
        public float trunkRadius;
        public float trunkCurviness;

        public int branchOrders;
        public int branchesPerOrder;
        public float branchesPerOrderDecay;
        public float childRadiusScale;
        public Vector2 childLengthScale;
        public float lengthDecayPerOrder;
        public float tipBranchChance;

        public Vector2 pitchRangeDeg;
        public Vector2 yawJitterDeg;
        public AnimationCurve bias;
        public AnimationCurve taper;
        public float noiseAmp;

        public float minRadius;
        public float minLength;

        public Material material;
        public bool debugOrderTint;

        public float maxChildToParentRatio;
        public float tipThinPower;
        public float absMinTwigRadius;
        public float stopBranchRadius;

        public float upwardBranchChance;
        public float upwardLift;

        public float plagiotropicChance;
        public float canopyPlagiotropicBoost;
        public Vector2 horizontalPitchRangeDeg;
        public float reduceUpBiasInCurve;

        public float lastOrderLenFactor;
        public float nearLastOrderLenFactor;
    }

    Settings s;
    System.Random rng;

    public void Build(Settings settings)
    {
        s = settings;
        rng = new System.Random(s.seed);

        // core ratios
        if (s.maxChildToParentRatio <= 0f || s.maxChildToParentRatio >= 1f) s.maxChildToParentRatio = 0.6f;
        if (s.tipThinPower <= 0f) s.tipThinPower = 1.2f;
        if (s.absMinTwigRadius <= 0f) s.absMinTwigRadius = Mathf.Max(0.0005f, s.minRadius * 0.25f);
        if (s.stopBranchRadius <= 0f) s.stopBranchRadius = Mathf.Max(s.absMinTwigRadius * 1.5f, s.minRadius * 0.9f);

        // ↓↓↓ make branches grow more horizontally ↓↓↓
        s.upwardBranchChance = Mathf.Clamp01(settings.upwardBranchChance <= 0f ? 0.15f : settings.upwardBranchChance);
        s.upwardLift         = Mathf.Clamp01(settings.upwardLift <= 0f ? 0.06f : settings.upwardLift);
        s.plagiotropicChance = Mathf.Clamp01(settings.plagiotropicChance <= 0f ? 0.85f : settings.plagiotropicChance);
        s.canopyPlagiotropicBoost = Mathf.Clamp01(settings.canopyPlagiotropicBoost <= 0f ? 0.45f : settings.canopyPlagiotropicBoost);
        s.horizontalPitchRangeDeg = (settings.horizontalPitchRangeDeg == Vector2.zero) ? new Vector2(5f, 35f) : settings.horizontalPitchRangeDeg;
        s.reduceUpBiasInCurve = Mathf.Clamp01(settings.reduceUpBiasInCurve <= 0f ? 0.3f : settings.reduceUpBiasInCurve);

        // ↓↓↓ reduce vertical dominance ↓↓↓
        s.pitchRangeDeg = new Vector2(5f, 25f);
        s.yawJitterDeg  = new Vector2(-45f, 45f);

        if (s.lastOrderLenFactor <= 0f || s.lastOrderLenFactor >= 1.0f) s.lastOrderLenFactor = 0.65f;
        if (s.nearLastOrderLenFactor <= 0f || s.nearLastOrderLenFactor >= 1.0f) s.nearLastOrderLenFactor = 0.85f;

        if (s.lengthDecayPerOrder <= 0f || s.lengthDecayPerOrder > 1f) s.lengthDecayPerOrder = 0.8f;

        var trunk = new BranchNode(null, 0, transform.position, Vector3.up, s.trunkHeight, s.trunkRadius);
        GenerateBranchCurve(trunk, s.trunkCurviness);
        SpawnChildren(trunk);
        DrawBranchRecursive(trunk);
    }

    class BranchNode
    {
        public BranchNode parent;
        public int order;
        public Vector3 basePos;
        public Vector3 dir;
        public float length;
        public float radius;
        public List<Vector3> curve;
        public readonly List<BranchNode> children = new();

        public BranchNode(BranchNode p, int o, Vector3 b, Vector3 d, float len, float r)
        { parent = p; order = o; basePos = b; dir = d.normalized; length = len; radius = r; }
    }

    float RadiusAt(BranchNode parent, float t)
    {
        t = Mathf.Clamp01(t);
        float taperScale = (parent == null || s.taper == null) ? 1f : s.taper.Evaluate(t);
        return Mathf.Max(s.minRadius, parent.radius * taperScale);
    }

    void SpawnChildren(BranchNode parent)
    {
        int nextOrder = parent.order + 1;
        if (nextOrder >= s.branchOrders) return;

        float ideal = s.branchesPerOrder * Mathf.Pow(Mathf.Clamp01(s.branchesPerOrderDecay), nextOrder - 1);
        int count = Mathf.Max(1, Mathf.RoundToInt(ideal + NormalAround(0f, 0.35f)));

        float margin = 0.10f;
        float step = (1f - 2f * margin) / (count + 1);

        for (int i = 1; i <= count; i++)
        {
            float t = margin + i * step;
            if (RadiusAt(parent, t) < s.stopBranchRadius) continue;
            CreateOneChild(parent, nextOrder, t);
        }

        if (Rand01() < Mathf.Clamp01(s.tipBranchChance))
        {
            float tTip = RandRange(0.84f, 0.95f);
            if (RadiusAt(parent, tTip) >= s.stopBranchRadius)
                CreateOneChild(parent, nextOrder, tTip, tip: true);
        }

        foreach (var c in parent.children)
            SpawnChildren(c);
    }

    void CreateOneChild(BranchNode parent, int order, float tOnParent, bool tip = false)
    {
        Vector3 spawnPos = SampleCurve(parent.curve, tOnParent);
        Vector3 parentDir = TangentCurve(parent.curve, tOnParent);

        float height01 = Mathf.Clamp01(parent.basePos.y / Mathf.Max(0.0001f, s.trunkHeight));
        float outwardChance = Mathf.Clamp01(s.plagiotropicChance + s.canopyPlagiotropicBoost * (1f - height01));
        outwardChance = Mathf.Clamp01(outwardChance + 0.15f * (1f - tOnParent));

        float biasV = Mathf.Clamp01(s.bias.Evaluate(tOnParent));
        float pitchDegUp = Mathf.Lerp(s.pitchRangeDeg.x, s.pitchRangeDeg.y, Rand01() * biasV + (1 - biasV) * 0.5f);
        float yawDeg = RandRange(s.yawJitterDeg.x, s.yawJitterDeg.y);

        Vector3 side = Vector3.Cross(parentDir, Vector3.up);
        if (side.sqrMagnitude < 1e-4f) side = Vector3.right;
        side.Normalize();

        Quaternion qYaw = Quaternion.AngleAxis(yawDeg, parentDir);

        bool plagiotropic = Rand01() < outwardChance;
        float pitchSign, pitchDeg;

        if (plagiotropic)
        {
            pitchDeg = RandRange(s.horizontalPitchRangeDeg.x, s.horizontalPitchRangeDeg.y);
            pitchSign = (Rand01() < 0.65f) ? -1f : +1f;
        }
        else
        {
            pitchDeg = pitchDegUp;
            pitchSign = (Rand01() < s.upwardBranchChance) ? +1f : -1f;
        }

        Quaternion qPitch = Quaternion.AngleAxis(pitchSign * pitchDeg, side);
        Vector3 childDir = (qYaw * qPitch) * parentDir;

        float lift = plagiotropic ? s.upwardLift * 0.25f : s.upwardLift;
        if (lift > 0f)
        {
            float liftAmt = lift * Mathf.Lerp(0.6f, 1.0f, Mathf.Clamp01(tOnParent));
            childDir = Vector3.Slerp(childDir, Vector3.up, Mathf.Clamp01(liftAmt));
        }
        childDir.Normalize();

        float parentRHere = RadiusAt(parent, tOnParent);
        float baseLen = parent.length * RandRange(s.childLengthScale.x, s.childLengthScale.y);
        float orderScale = Mathf.Pow(Mathf.Clamp01(s.lengthDecayPerOrder), Mathf.Max(1, order));
        baseLen *= orderScale;
        if (s.branchOrders >= 2 && order == s.branchOrders - 1) baseLen *= Mathf.Clamp01(s.lastOrderLenFactor);
        else if (s.branchOrders >= 3 && order == s.branchOrders - 2) baseLen *= Mathf.Clamp01(s.nearLastOrderLenFactor);
        float len = Mathf.Max(s.minLength, tip ? baseLen * 0.6f : baseLen);

        float baseRad = parentRHere * Mathf.Max(0.0001f, s.childRadiusScale);
        float tipThin = Mathf.Pow(1f - Mathf.Clamp01(tOnParent), s.tipThinPower);
        baseRad *= Mathf.Lerp(0.75f, 1f, 0.5f) * Mathf.Max(0.15f, tipThin);
        if (tip) baseRad *= 0.85f;

        float maxAllowedByParent = Mathf.Max(s.absMinTwigRadius, parentRHere * s.maxChildToParentRatio);
        float rad = Mathf.Clamp(baseRad, s.absMinTwigRadius, maxAllowedByParent);
        if (rad >= parentRHere * 0.98f) return;

        float insetMax = Mathf.Max(0f, parentRHere - rad * 0.25f);
        float inset = Mathf.Clamp(rad * 0.6f, 0f, insetMax);
        Vector3 childBase = spawnPos - childDir * inset;

        var child = new BranchNode(parent, order, childBase, childDir, len, rad);
        GenerateBranchCurve(child, 0.25f);
        parent.children.Add(child);
    }

    void DrawBranchRecursive(BranchNode b)
    {
        var mesh = TubeMesh.Build(b.curve, s.radialSegments, s.curveSamples, s.taper, b.radius,
                                  capStart: (b.order == 0), capEnd: true);

        var go = new GameObject($"Branch_o{b.order}");
        go.transform.SetParent(transform, true);
        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        mf.sharedMesh = mesh;
        mr.sharedMaterial = s.material;

        if (s.debugOrderTint && mr.sharedMaterial.HasProperty("_Color"))
        {
            var pb = new MaterialPropertyBlock();
            pb.SetColor("_Color", OrderColor(b.order));
            mr.SetPropertyBlock(pb);
        }

        foreach (var c in b.children)
            DrawBranchRecursive(c);
    }

    Color OrderColor(int order)
    {
        Color[] palette = {
            new Color(0.45f,0.30f,0.12f),
            new Color(0.90f,0.55f,0.15f),
            new Color(0.95f,0.90f,0.20f),
            new Color(0.20f,0.75f,0.35f),
            new Color(0.20f,0.80f,0.80f),
            new Color(0.25f,0.45f,0.95f),
            new Color(0.70f,0.40f,0.90f)
        };
        return palette[Mathf.Clamp(order, 0, palette.Length - 1)];
    }

    void GenerateBranchCurve(BranchNode b, float curviness)
    {
        int ctrl = 3 + Mathf.RoundToInt(curviness * 2f);
        var points = new List<Vector3> { b.basePos };

        Vector3 p = b.basePos;
        Vector3 d = b.dir;
        float remain = b.length;

        for (int i = 1; i < ctrl; i++)
        {
            float seg = remain / (ctrl - i + 1);
            Vector3 n1 = Ortho(d);
            Vector3 n2 = Vector3.Cross(d, n1).normalized;
            float amp = s.noiseAmp * seg;
            Vector3 wobble = n1 * RandRange(-amp, amp) + n2 * RandRange(-amp, amp);

            float upFactor = Mathf.Lerp(0.2f, 0.2f * (1f - s.reduceUpBiasInCurve), curviness);
            Vector3 upBias = Vector3.up * (upFactor * seg);

            p = p + d * seg + wobble + upBias;
            d = Vector3.Slerp(d, (d + Vector3.up * 0.35f * (1f - s.reduceUpBiasInCurve)).normalized, 0.35f);
            points.Add(p);
            remain -= seg;
        }
        points.Add(b.basePos + b.dir * b.length);

        b.curve = CatmullRom(points, s.curveSamples);
    }

    Vector3 Ortho(Vector3 v)
    {
        v.Normalize();
        if (Mathf.Abs(v.y) < 0.99f) return Vector3.Cross(v, Vector3.up).normalized;
        return Vector3.Cross(v, Vector3.right).normalized;
    }

    float Rand01() => (float)rng.NextDouble();
    float RandRange(float a, float b) => a + (float)rng.NextDouble() * (b - a);

    float NormalAround(float mean, float sigma)
    {
        double u1 = 1.0 - rng.NextDouble();
        double u2 = 1.0 - rng.NextDouble();
        double r = Mathf.Sqrt(-2.0f * Mathf.Log((float)u1)) * Mathf.Sin(2.0f * Mathf.PI * (float)u2);
        return mean + (float)r * sigma;
    }

    static Vector3 SampleCurve(List<Vector3> c, float t)
    {
        if (c == null || c.Count == 0) return Vector3.zero;
        if (c.Count == 1) return c[0];
        float f = Mathf.Clamp01(t) * (c.Count - 1);
        int i = Mathf.Clamp(Mathf.FloorToInt(f), 0, c.Count - 2);
        float lt = f - i;
        return Vector3.Lerp(c[i], c[i + 1], lt);
    }

    static Vector3 TangentCurve(List<Vector3> c, float t)
    {
        float eps = 1f / Mathf.Max(8, c.Count);
        Vector3 a = SampleCurve(c, Mathf.Clamp01(t - eps));
        Vector3 b = SampleCurve(c, Mathf.Clamp01(t + eps));
        Vector3 d = (b - a);
        if (d.sqrMagnitude < 1e-6f) d = Vector3.up;
        return d.normalized;
    }

    static List<Vector3> CatmullRom(List<Vector3> ctrl, int segmentsPerSpan)
    {
        var pts = new List<Vector3>();
        if (ctrl.Count < 2) { pts.AddRange(ctrl); return pts; }

        for (int i = -1; i < ctrl.Count - 2; i++)
        {
            Vector3 p0 = ctrl[Mathf.Clamp(i, 0, ctrl.Count - 1)];
            Vector3 p1 = ctrl[Mathf.Clamp(i + 1, 0, ctrl.Count - 1)];
            Vector3 p2 = ctrl[Mathf.Clamp(i + 2, 0, ctrl.Count - 1)];
            Vector3 p3 = ctrl[Mathf.Clamp(i + 3, 0, ctrl.Count - 1)];

            for (int j = 0; j < segmentsPerSpan; j++)
            {
                float t = j / (float)segmentsPerSpan;
                pts.Add(Catmull(p0, p1, p2, p3, t));
            }
        }
        pts.Add(ctrl[^1]);
        return pts;
    }

    static Vector3 Catmull(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t, t3 = t2 * t;
        return 0.5f * ((2f * p1) +
                       (-p0 + p2) * t +
                       (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                       (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }
}
