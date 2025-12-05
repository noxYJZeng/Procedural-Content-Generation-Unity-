using UnityEngine;
using System.Collections.Generic;

public class PlantSpawner : MonoBehaviour
{
    [Header("Random")]
    public int masterSeed = 42;

    [Header("Layout")]
    public int plantCount = 3;
    public float xSpacing = 8f;

    [Header("Appearance / Geometry")]
    public Material barkMaterial;
    public Gradient barkColorGradient;
    public int radialSegments = 12;
    public int curveSamples = 16;
    public float trunkHeight = 8f;
    public float trunkRadius = 0.14f;
    public float trunkCurviness = 0.12f;

    [Header("Branching (Depth & Breadth)")]
    public int branchOrders = 4;
    public int branchesPerOrder = 8;
    public float branchesPerOrderDecay = 0.95f;
    public Vector2 childLengthScale = new Vector2(0.65f, 0.9f);
    public float lengthDecayPerOrder = 0.88f;
    public float childRadiusScale = 0.65f;
    public float tipBranchChance = 0.8f;

    [Header("Orientation")]
    public Vector2 pitchRangeDeg = new Vector2(15f, 40f);
    public Vector2 yawJitterDeg = new Vector2(-20f, 20f);
    public AnimationCurve orthotropicBias = AnimationCurve.EaseInOut(0, 0.6f, 1, 1.0f);
    public AnimationCurve plagiotropicBias = AnimationCurve.EaseInOut(0, 0.2f, 1, 0.6f);

    [Header("Taper / Noise / Visibility")]
    public AnimationCurve taper = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.2f));
    public float noiseAmp = 0.06f;
    public float minBranchRadius = 0.008f;
    public float minBranchLength = 0.28f;

    [Header("Bark Textures (optional)")]
    public Texture2D barkAlbedo;
    public Texture2D barkNormal;
    public Texture2D barkHeight;
    public Texture2D barkMaskMap;

    [Header("Bark Tiling & PBR")]
    public Vector2 barkTiling = new Vector2(3f, 1.6f);
    [Range(0f, 1f)] public float barkSmoothness = 0.32f;
    [Range(0f, 1f)] public float barkMetallic = 0.05f;
    public float barkNormalScale = 1.0f;
    public float barkHeightScale = 0.03f;

    [Header("Leaf Material (optional)")]
    public Material leafMaterial;

    [Header("Ground (Auto)")]
    public bool generateGround = true;
    public int groundResolution = 80;
    public float groundPadding = 6f;
    public float groundNoiseScale = 0.25f;
    public float groundHeightAmplitude = 0.6f;
    public Color grassTint = new Color(0.25f, 0.6f, 0.25f);
    public Texture2D grassAlbedo;
    public Texture2D grassNormal;

    [Header("Debug")]
    public bool debugOrderTint = true;

    private GameObject groundObj;

    private void Start() => Generate();

    private Vector3 GroundAnchor() => new Vector3(transform.position.x, 0f, transform.position.z);

    [ContextMenu("Generate")]
    public void Generate()
    {
        for (int i = transform.childCount - 1; i >= 0; --i)
            DestroyImmediate(transform.GetChild(i).gameObject);

        Random.InitState(masterSeed);
        var anchor = GroundAnchor();

        if (generateGround)
            GenerateGround(anchor);

        for (int i = 0; i < plantCount; i++)
        {
            var go = new GameObject($"Plant_{i}");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(
                anchor.x + (i - (plantCount - 1) * 0.5f) * xSpacing,
                0f,
                anchor.z
            );

            var mat = new Material(barkMaterial != null ? barkMaterial : new Material(Shader.Find("Standard")));
            float t = (plantCount <= 1) ? 0f : Mathf.InverseLerp(0, plantCount - 1, i);
            var tint = barkColorGradient.Evaluate(t);
            TrySetColor(mat, tint);
            ApplyBarkMaps(mat, barkAlbedo, barkNormal, barkHeight, barkMaskMap,
                barkTiling, barkSmoothness, barkMetallic, barkNormalScale, barkHeightScale);

            bool orthotropic = (i % 2 == 0);
            var bias = orthotropic ? orthotropicBias : plagiotropicBias;

            var plant = go.AddComponent<ProceduralPlant>();
            plant.Build(new ProceduralPlant.Settings
            {
                seed = masterSeed + 1000 * (i + 1),
                radialSegments = radialSegments,
                curveSamples = curveSamples,
                trunkHeight = trunkHeight,
                trunkRadius = trunkRadius,
                trunkCurviness = trunkCurviness,
                branchOrders = Mathf.Max(3, branchOrders),
                branchesPerOrder = branchesPerOrder,
                branchesPerOrderDecay = branchesPerOrderDecay,
                childRadiusScale = childRadiusScale,
                childLengthScale = childLengthScale,
                lengthDecayPerOrder = lengthDecayPerOrder,
                tipBranchChance = tipBranchChance,
                pitchRangeDeg = pitchRangeDeg,
                yawJitterDeg = yawJitterDeg,
                bias = bias,
                taper = taper,
                noiseAmp = noiseAmp,
                minRadius = minBranchRadius,
                minLength = minBranchLength,
                material = mat,
                debugOrderTint = debugOrderTint
            });

            var leaf = go.AddComponent<LeafCanopyDecorator>();
            leaf.leafMaterial = leafMaterial != null
                ? leafMaterial
                : new Material(Shader.Find("Unlit/Color")) { color = new Color(0.18f, 0.65f, 0.25f) };
            leaf.DecorateNow();
        }
    }

    void GenerateGround(Vector3 anchor)
    {
        if (groundObj != null)
            DestroyImmediate(groundObj);

        float width = (plantCount - 1) * xSpacing + groundPadding;
        Mesh mesh = BuildGrassMesh(width, width * 0.6f, groundResolution,
            groundHeightAmplitude, groundNoiseScale);

        groundObj = new GameObject("GrassGround");
        groundObj.transform.SetParent(transform, false);
        groundObj.transform.position = anchor;

        var mf = groundObj.AddComponent<MeshFilter>();
        var mr = groundObj.AddComponent<MeshRenderer>();
        mf.sharedMesh = mesh;

        var mat = new Material(Shader.Find("Standard"));
        mat.color = grassTint;
        mat.SetFloat("_Metallic", 0f);
        mat.SetFloat("_Glossiness", 0.15f);
        if (grassAlbedo) mat.mainTexture = grassAlbedo;
        if (grassNormal)
        {
            mat.EnableKeyword("_NORMALMAP");
            mat.SetTexture("_BumpMap", grassNormal);
            mat.SetFloat("_BumpScale", 1.2f);
        }

        mr.sharedMaterial = mat;
    }

    Mesh BuildGrassMesh(float sizeX, float sizeZ, int res, float amp, float scale)
    {
        int vertsX = res + 1;
        int vertsZ = res + 1;
        var verts = new List<Vector3>(vertsX * vertsZ);
        var uvs = new List<Vector2>(vertsX * vertsZ);
        var tris = new List<int>(res * res * 6);

        for (int z = 0; z < vertsZ; z++)
        {
            for (int x = 0; x < vertsX; x++)
            {
                float xf = (x / (float)res - 0.5f) * sizeX;
                float zf = (z / (float)res - 0.5f) * sizeZ;
                float nx = xf * scale + 100f;
                float nz = zf * scale + 100f;

                float baseNoise = Mathf.PerlinNoise(nx, nz);
                float detail1 = Mathf.PerlinNoise(nx * 2f, nz * 2f) * 0.5f;
                float detail2 = Mathf.PerlinNoise(nx * 4f, nz * 4f) * 0.25f;
                float y = (baseNoise + detail1 + detail2) * amp * 2.5f - amp;

                verts.Add(new Vector3(xf, y, zf));
                uvs.Add(new Vector2(x / (float)res, z / (float)res));
            }
        }

        for (int z = 0; z < res; z++)
        {
            for (int x = 0; x < res; x++)
            {
                int i = z * (res + 1) + x;
                tris.Add(i);
                tris.Add(i + res + 1);
                tris.Add(i + 1);
                tris.Add(i + 1);
                tris.Add(i + res + 1);
                tris.Add(i + res + 2);
            }
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static void TrySetColor(Material m, Color c)
    {
        if (!m) return;
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
    }

    static void ApplyBarkMaps(Material m,
        Texture2D albedo, Texture2D normal, Texture2D height, Texture2D mask,
        Vector2 tiling, float smoothness, float metallic,
        float normalScale, float heightScale)
    {
        if (!m) return;
        if (albedo) m.SetTexture("_MainTex", albedo);
        if (normal)
        {
            m.EnableKeyword("_NORMALMAP");
            m.SetTexture("_BumpMap", normal);
            m.SetFloat("_BumpScale", normalScale);
        }
        if (height)
        {
            m.EnableKeyword("_PARALLAXMAP");
            m.SetTexture("_ParallaxMap", height);
            m.SetFloat("_Parallax", heightScale);
        }
        if (mask) m.SetTexture("_MaskMap", mask);

        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
    }
}
