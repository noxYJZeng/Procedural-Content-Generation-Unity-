using UnityEngine;

public static class BezierPatchGenerator
{
    public static Mesh GenerateBody(System.Random rand)
    {
        float radius = 5f + (float)rand.NextDouble() * 2.5f;
        float height = 1.5f + (float)rand.NextDouble() * 0.8f;

        float edgeShrinkTop = Mathf.Lerp(0.12f, 0.25f, (float)rand.NextDouble());
        float edgeShrinkBot = Mathf.Lerp(0.12f, 0.25f, (float)rand.NextDouble());
        float bulge = Mathf.Lerp(0.9f, 1.12f, (float)rand.NextDouble());
        float bulgeBias = Mathf.Lerp(0.1f, 0.22f, (float)rand.NextDouble());

        float h2 = height * 0.5f;
        Vector2 p0 = new Vector2(radius * edgeShrinkBot, -h2);
        Vector2 p1 = new Vector2(radius * bulge, -height * bulgeBias);
        Vector2 p2 = new Vector2(radius * bulge, height * bulgeBias);
        Vector2 p3 = new Vector2(radius * edgeShrinkTop * 0.98f, +h2 * 1.8f);

        int vSegments = 64;
        int uSegments = 96;

        var r = new float[vSegments + 1];
        var y = new float[vSegments + 1];
        var dr = new float[vSegments + 1];
        var dy = new float[vSegments + 1];

        for (int i = 0; i <= vSegments; i++)
        {
            float t = i / (float)vSegments;
            Vector2 pos = CubicBezier(p0, p1, p2, p3, t);
            Vector2 der = CubicBezierDerivative(p0, p1, p2, p3, t);
            r[i] = Mathf.Max(0f, pos.x);
            y[i] = pos.y;
            dr[i] = der.x;
            dy[i] = der.y;
        }

        int vertCount = (vSegments + 1) * (uSegments + 1);
        Vector3[] verts = new Vector3[vertCount];
        Vector3[] norms = new Vector3[vertCount];
        Vector2[] uv = new Vector2[vertCount];

        for (int i = 0; i <= vSegments; i++)
        {
            float v = i / (float)vSegments;
            float ri = r[i];
            float yi = y[i];
            float dri = dr[i];
            float dyi = dy[i];

            for (int j = 0; j <= uSegments; j++)
            {
                float u = j / (float)uSegments;
                float theta = u * Mathf.PI * 2f;
                float c = Mathf.Cos(theta);
                float s = Mathf.Sin(theta);
                int idx = i * (uSegments + 1) + j;

                verts[idx] = new Vector3(ri * c, yi, ri * s);

                Vector3 dTheta = new Vector3(-ri * s, 0f, ri * c);
                Vector3 dT = new Vector3(dri * c, dyi, dri * s);
                norms[idx] = Vector3.Cross(dTheta, dT).normalized;

                uv[idx] = new Vector2(u, v);
            }
        }

        int[] tris = new int[vSegments * uSegments * 6];
        int tPtr = 0;
        for (int i = 0; i < vSegments; i++)
        {
            for (int j = 0; j < uSegments; j++)
            {
                int i0 = i * (uSegments + 1) + j;
                int i1 = i0 + 1;
                int i2 = (i + 1) * (uSegments + 1) + j;
                int i3 = i2 + 1;

                tris[tPtr++] = i0;
                tris[tPtr++] = i2;
                tris[tPtr++] = i1;
                tris[tPtr++] = i1;
                tris[tPtr++] = i2;
                tris[tPtr++] = i3;
            }
        }

        Mesh mesh = new Mesh();
        mesh.vertices = verts;
        mesh.normals = norms;
        mesh.uv = uv;
        mesh.triangles = tris;
        mesh.RecalculateBounds();
        return mesh;
    }

    static Vector2 CubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float it = 1f - t;
        float b0 = it * it * it;
        float b1 = 3f * it * it * t;
        float b2 = 3f * it * t * t;
        float b3 = t * t * t;
        return new Vector2(
            b0 * p0.x + b1 * p1.x + b2 * p2.x + b3 * p3.x,
            b0 * p0.y + b1 * p1.y + b2 * p2.y + b3 * p3.y
        );
    }

    static Vector2 CubicBezierDerivative(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float it = 1f - t;
        Vector2 term1 = it * it * (p1 - p0);
        Vector2 term2 = 2f * it * t * (p2 - p1);
        Vector2 term3 = t * t * (p3 - p2);
        return 3f * (term1 + term2 + term3);
    }


    public static Mesh GenerateDome(System.Random rand, int variant = 0)
    {
        int steps = 40;
        float radius = 2f + (float)rand.NextDouble() * 0.5f;
        float height = 2.2f + (float)rand.NextDouble() * 0.3f;

        switch (variant)
        {
            case 0: return GenerateHemisphere(radius, height, steps, true, true);
            case 1: return GenerateConeLikeDome(radius, height * 1.5f, steps);
            case 2: return GenerateFlattenedDome(radius, height * 0.6f, steps);
            default: return GenerateHemisphere(radius, height, steps, true, true);
        }
    }

    static Mesh GenerateHemisphere(float radius, float height, int steps, bool upward, bool isDome = false)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[(steps + 1) * (steps + 1)];
        int[] triangles = new int[steps * steps * 6];
        Vector2[] uvs = new Vector2[vertices.Length];
        int t = 0;

        for (int i = 0; i <= steps; i++)
        {
            float u = i / (float)steps;
            float theta = u * Mathf.PI * 2f;
            for (int j = 0; j <= steps; j++)
            {
                float v = j / (float)steps;
                float r = radius * (1 - Mathf.Pow(v, 2f));
                float y = (upward ? 1 : -1) * Mathf.Sin(v * Mathf.PI / 2f) * height;

                Vector3 pos = new Vector3(r * Mathf.Cos(theta), y, r * Mathf.Sin(theta));
                if (isDome) pos.y *= 1.2f;

                int index = i * (steps + 1) + j;
                vertices[index] = pos;


                float uMap = v;
                float vMap = 0.5f + 0.5f * Mathf.Sin(theta);
                uvs[index] = new Vector2(uMap, vMap);

                if (i < steps && j < steps)
                {
                    int a = i * (steps + 1) + j;
                    int b = a + 1;
                    int c = a + (steps + 1);
                    int d = c + 1;

                    if (upward)
                    {
                        triangles[t++] = a; triangles[t++] = b; triangles[t++] = c;
                        triangles[t++] = b; triangles[t++] = d; triangles[t++] = c;
                    }
                    else
                    {
                        triangles[t++] = a; triangles[t++] = c; triangles[t++] = b;
                        triangles[t++] = b; triangles[t++] = c; triangles[t++] = d;
                    }
                }
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        return mesh;
    }



    static Mesh GenerateConeLikeDome(float radius, float height, int steps)
    {
        Mesh mesh = new Mesh();
        Vector3[] verts = new Vector3[(steps + 1) * (steps + 1)];
        int[] tris = new int[steps * steps * 6];
        int t = 0;

        for (int i = 0; i <= steps; i++)
        {
            float u = i / (float)steps;
            float theta = u * Mathf.PI * 2f;
            for (int j = 0; j <= steps; j++)
            {
                float v = j / (float)steps;
                float r = radius * (1 - v);
                float y = Mathf.Pow(v, 1.2f) * height;
                verts[i * (steps + 1) + j] = new Vector3(r * Mathf.Cos(theta), y, r * Mathf.Sin(theta));

                if (i < steps && j < steps)
                {
                    int a = i * (steps + 1) + j;
                    int b = a + 1;
                    int c = a + (steps + 1);
                    int d = c + 1;
                    tris[t++] = a; tris[t++] = b; tris[t++] = c;
                    tris[t++] = b; tris[t++] = d; tris[t++] = c;
                }
            }
        }
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        return mesh;
    }

    static Mesh GenerateFlattenedDome(float radius, float height, int steps)
    {
        Mesh mesh = GenerateHemisphere(radius, height, steps, true);
        Vector3[] v = mesh.vertices;
        for (int i = 0; i < v.Length; i++)
            if (v[i].y > height * 0.7f)
                v[i].y = height * 0.7f;
        mesh.vertices = v;
        mesh.RecalculateNormals();
        return mesh;
    }

    public static Mesh GenerateLowerPod(System.Random rand, int variant)
    {
        int steps = 36;
        switch (variant)
        {
            case 0:
                float r0 = 2.4f + (float)rand.NextDouble() * 0.9f;
                float h0 = 1.0f + (float)rand.NextDouble() * 0.3f;
                return GenerateHemisphere(r0, h0, steps, false);
            case 1:
                float r1 = 2.2f + (float)rand.NextDouble() * 0.6f;
                float h1 = 1.5f + (float)rand.NextDouble() * 0.3f;
                return GenerateConePod(r1, h1, steps);
            case 2:
            default:
                float r2 = 2.3f + (float)rand.NextDouble() * 0.9f;
                float h2 = 1.0f + (float)rand.NextDouble() * 0.2f;
                return GenerateBulgePod(r2, h2, steps);
        }
    }

    static Mesh GenerateConePod(float radius, float height, int steps)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[steps + 2];
        int[] triangles = new int[steps * 3];
        vertices[0] = new Vector3(0, -height, 0);
        for (int i = 0; i <= steps; i++)
        {
            float theta = i / (float)steps * Mathf.PI * 2f;
            vertices[i + 1] = new Vector3(radius * Mathf.Cos(theta), 0, radius * Mathf.Sin(theta));
        }
        int t = 0;
        for (int i = 0; i < steps; i++)
        {
            triangles[t++] = 0;
            triangles[t++] = i + 1;
            triangles[t++] = (i + 2) <= steps ? (i + 2) : 1;
        }
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        return mesh;
    }

    static Mesh GenerateBulgePod(float radius, float height, int steps)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[(steps + 1) * (steps + 1)];
        int[] triangles = new int[steps * steps * 6];
        int t = 0;

        for (int i = 0; i <= steps; i++)
        {
            float u = i / (float)steps;
            float theta = u * Mathf.PI * 2f;
            for (int j = 0; j <= steps; j++)
            {
                float v = j / (float)steps;
                float r = radius * (1 - 0.3f * Mathf.Pow(v - 0.5f, 2f));
                float y = -Mathf.Cos(v * Mathf.PI) * height * 0.45f;
                vertices[i * (steps + 1) + j] = new Vector3(r * Mathf.Cos(theta), y, r * Mathf.Sin(theta));
                if (i < steps && j < steps)
                {
                    int a = i * (steps + 1) + j;
                    int b = a + 1;
                    int c = a + (steps + 1);
                    int d = c + 1;
                    triangles[t++] = a; triangles[t++] = b; triangles[t++] = c;
                    triangles[t++] = b; triangles[t++] = d; triangles[t++] = c;
                }
            }
        }
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        return mesh;
    }

    static Mesh CombineMeshes(Mesh a, Mesh b)
    {
        Mesh mesh = new Mesh();

        int vCount = a.vertexCount + b.vertexCount;

        Vector3[] verts = new Vector3[vCount];
        a.vertices.CopyTo(verts, 0);
        b.vertices.CopyTo(verts, a.vertexCount);

        int[] tris = new int[a.triangles.Length + b.triangles.Length];
        a.triangles.CopyTo(tris, 0);
        int offset = a.vertexCount;
        for (int i = 0; i < b.triangles.Length; i++)
            tris[a.triangles.Length + i] = b.triangles[i] + offset;

        Vector2[] uvs = new Vector2[vCount];
        var auv = (a.uv != null && a.uv.Length == a.vertexCount) ? a.uv : new Vector2[a.vertexCount];
        var buv = (b.uv != null && b.uv.Length == b.vertexCount) ? b.uv : new Vector2[b.vertexCount];
        auv.CopyTo(uvs, 0);
        buv.CopyTo(uvs, offset);

        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.uv = uvs;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    public static GameObject GenerateEngineCore(System.Random rand, Transform parent)
    {
        int variant = rand.Next(0, 3);
        GameObject core = new GameObject("EngineCore_" + variant);
        core.transform.parent = parent;

        float offsetY = 0.0f;
        core.transform.localPosition = new Vector3(0, offsetY, 0);

        switch (variant)
        {
            case 0: GenerateMultiRingEmitter(core, offsetY); break;
            case 1: GenerateParticleEmitter(core, offsetY); break;
            case 2: GenerateTrapezoidBeam(core, offsetY, rand); break;
        }

        core.AddComponent<EnginePulse>().variant = variant;
        return core;
    }

    static void GenerateMultiRingEmitter(GameObject core, float offsetY)
    {
        GameObject group = new GameObject("MultiRingEmitter");
        group.transform.parent = core.transform;
        group.transform.localPosition = new Vector3(0, offsetY, 0);

        int ringCount = 6;
        float baseY = 0.0f;
        float ringSpacingBase = 0.6f;
        float ringSpacingGrowth = 0.25f;
        float radiusTop = 4.6f;
        float radiusStep = 0.7f;
        float thicknessTop = 0.18f;
        float thicknessStep = 0.07f;

        Color topColor = RandomColorBright();
        Color bottomColor = RandomColorDark();

        float currentY = baseY;

        for (int i = 0; i < ringCount; i++)
        {
            float radius = radiusTop + i * radiusStep;
            float thickness = thicknessTop + i * thicknessStep;
            float t = i / (float)(ringCount - 1);
            Color c = Color.Lerp(topColor, bottomColor, t);

            GameObject ring = new GameObject("EnergyRing_" + i);
            ring.transform.parent = group.transform;
            ring.transform.localPosition = new Vector3(0, currentY, 0);
            ring.transform.localScale = Vector3.one;

            var mf = ring.AddComponent<MeshFilter>();
            var mr = ring.AddComponent<MeshRenderer>();
            mr.sharedMaterial = new Material(Shader.Find("Standard"));
            mr.sharedMaterial.EnableKeyword("_EMISSION");

            mf.mesh = GenerateTorus(radius, thickness, 64, 32);

            mr.sharedMaterial.color = c;
            mr.sharedMaterial.SetColor("_EmissionColor", c * (6f + i * 2f));

            float spacing = ringSpacingBase + i * ringSpacingGrowth;
            currentY -= spacing + thickness * 1.2f;
        }
    }

    static void GenerateParticleEmitter(GameObject core, float offsetY)
    {
        string[] oldNames = { "EngineParticles", "Cylinder", "MainBeam", "Beam", "EngineCone", "Flame", "Emitter" };

        foreach (Transform t in core.GetComponentsInChildren<Transform>(true))
        {
            foreach (string name in oldNames)
            {
                if (t.name.ToLower().Contains(name.ToLower()))
                {
                    Object.DestroyImmediate(t.gameObject);
                    break;
                }
            }
        }

        if (core.transform.parent != null)
        {
            foreach (Transform t in core.transform.parent.GetComponentsInChildren<Transform>(true))
            {
                foreach (string name in oldNames)
                {
                    if (t.name.ToLower().Contains(name.ToLower()))
                    {
                        Object.DestroyImmediate(t.gameObject);
                        break;
                    }
                }
            }
        }

        Renderer bodyRenderer = null;
        Transform body = null;

        Transform check = core.transform;
        while (check != null && body == null)
        {
            if (check.parent != null)
            {
                foreach (Transform child in check.parent)
                {
                    if (child.name.ToLower().Contains("body"))
                    {
                        body = child;
                        break;
                    }
                }
            }
            check = check.parent;
        }

        if (body != null)
            bodyRenderer = body.GetComponent<Renderer>();

        float bottomY = core.transform.position.y;
        float spanXZ = 5f;

        if (bodyRenderer != null)
        {
            var b = bodyRenderer.bounds;
            bottomY = b.center.y - b.extents.y;
            spanXZ = Mathf.Max(b.size.x, b.size.z);
        }

        float gap = Mathf.Max(0.1f * spanXZ, 0.6f);
        Vector3 emitWorldPos = new Vector3(core.transform.position.x,
                                           bottomY - gap + offsetY,
                                           core.transform.position.z);

        var group = new GameObject("EngineParticles");
        group.transform.SetParent(core.transform, worldPositionStays: true);
        group.transform.position = emitWorldPos;
        group.transform.rotation = Quaternion.identity;

        var go = new GameObject("MainBeam");
        go.transform.SetParent(group.transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.duration = 6f;

        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 20000;
        ps.Play();

        main.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 3.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.0f, 3.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
        main.startRotation3D = true;

        var emission = ps.emission;
        emission.rateOverTime = 3000f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.ConeVolume;
        shape.radius = spanXZ * 0.2f;
        shape.angle = 50f;
        shape.length = Mathf.Max(0.4f * spanXZ, 4f);
        shape.radiusThickness = 1.0f;
        shape.arc = 360f;
        shape.alignToDirection = true;

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.y = new ParticleSystem.MinMaxCurve(0f);

        Color RandomBrightColor()
        {
            float hue = UnityEngine.Random.value;
            return Color.HSVToRGB(hue, 0.9f, 1f);
        }

        Color c1 = RandomBrightColor();
        Color c2 = RandomBrightColor();
        Color c3 = RandomBrightColor();

        var gradient = new Gradient();
        gradient.SetKeys(
            new[] {
                new GradientColorKey(c1, 0f),
                new GradientColorKey(c2, 0.5f),
                new GradientColorKey(c3, 1f)
            },
            new[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = new ParticleSystem.MinMaxGradient(gradient);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        var sCurve = new AnimationCurve(
            new Keyframe(0f, 0.85f),
            new Keyframe(0.3f, 1f),
            new Keyframe(1f, 0.3f)
        );
        size.size = new ParticleSystem.MinMaxCurve(1f, sCurve);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.35f;
        noise.frequency = 0.6f;
        noise.scrollSpeed = 0.25f;

        var psr = ps.GetComponent<ParticleSystemRenderer>();
        Shader sh = Shader.Find("Particles/Standard Unlit");
        if (sh == null) sh = Shader.Find("Particles/Additive");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");

        var mat = new Material(sh);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", c1);
        if (mat.HasProperty("_EmissionColor"))
            mat.SetColor("_EmissionColor", c2 * 3.0f);
        mat.EnableKeyword("_EMISSION");

        psr.sharedMaterial = mat;
        psr.renderMode = ParticleSystemRenderMode.Billboard;
        psr.alignment = ParticleSystemRenderSpace.World;
        psr.sortMode = ParticleSystemSortMode.Distance;
    }

    static void GenerateTrapezoidBeam(GameObject core, float offsetY, System.Random rand)
    {
        foreach (Transform t in core.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.ToLower().Contains("beam"))
                Object.DestroyImmediate(t.gameObject);
        }

        Renderer bodyRenderer = null;
        Transform body = null;
        Transform p = core.transform;
        while (p != null && body == null)
        {
            if (p.parent != null)
            {
                foreach (Transform c in p.parent)
                {
                    if (c.name.ToLower().Contains("body"))
                    {
                        body = c;
                        break;
                    }
                }
            }
            p = p.parent;
        }

        if (body != null)
            bodyRenderer = body.GetComponent<Renderer>();

        float bodyRadius = 5f;
        float bodyBottomY = core.transform.position.y;

        if (bodyRenderer != null)
        {
            var b = bodyRenderer.bounds;
            bodyBottomY = b.center.y - b.extents.y;
            bodyRadius = Mathf.Max(b.extents.x, b.extents.z);
        }

        float beamHeight = Random.Range(bodyRadius * 0.8f, bodyRadius * 1.5f);
        float beamBottomRadius = bodyRadius * Random.Range(0.4f, 0.6f);
        float beamTopRadius = beamBottomRadius * Random.Range(0.25f, 0.5f);

        float verticalOffset = -beamHeight * Random.Range(0.6f, 0.8f);

        Vector3 beamWorldPos = new Vector3(
            core.transform.position.x,
            bodyBottomY + offsetY + verticalOffset,
            core.transform.position.z
        );

        GameObject beamGO = new GameObject("DynamicBeam");
        beamGO.transform.SetParent(core.transform, worldPositionStays: true);
        beamGO.transform.position = beamWorldPos;
        beamGO.transform.rotation = Quaternion.identity;

        var mf = beamGO.AddComponent<MeshFilter>();
        var mr = beamGO.AddComponent<MeshRenderer>();

        GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Mesh mesh = Object.Instantiate(temp.GetComponent<MeshFilter>().sharedMesh);
        Object.DestroyImmediate(temp);

        Vector3[] verts = mesh.vertices;
        Color[] colors = new Color[verts.Length];

        float bottomY = verts[0].y;
        float topY = verts[verts.Length - 1].y;

        float h1 = (float)rand.NextDouble();
        float h2 = (float)rand.NextDouble();
        Color topColor = Color.HSVToRGB(h1, 0.8f, 1f);
        Color bottomColor = Color.HSVToRGB(h2, 0.8f, 1f);


        for (int i = 0; i < verts.Length; i++)
        {
            float t = Mathf.InverseLerp(topY, bottomY, verts[i].y);

            float localRadius = Mathf.Lerp(beamTopRadius, beamBottomRadius, t);
            verts[i].x *= localRadius;
            verts[i].z *= localRadius;
            verts[i].y *= beamHeight * 0.5f;

            colors[i] = Color.Lerp(topColor, bottomColor, t);
        }

        mesh.vertices = verts;
        mesh.colors = colors;
        mesh.RecalculateNormals();
        mf.mesh = mesh;

        Shader shader = Shader.Find("Unlit/VertexColor");
        if (shader == null)
            shader = Shader.Find("Legacy Shaders/Transparent/Diffuse");

        mr.sharedMaterial = new Material(shader);
        mr.sharedMaterial.renderQueue = 3000;
        mr.sharedMaterial.EnableKeyword("_EMISSION");
        mr.sharedMaterial.SetColor("_Color", Color.white);
        mr.sharedMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

        var dyn = beamGO.AddComponent<DynamicBeamGradient>();
        dyn.renderer = mr;
        dyn.meshFilter = mf;
    }


    static Color RandomColorBright()
    {
        return Color.HSVToRGB(Random.Range(0f, 1f), Random.Range(0.6f, 1f), 1f);
    }

    static Color RandomColorDark()
    {
        return Color.HSVToRGB(Random.Range(0f, 1f), Random.Range(0.4f, 0.8f), 0.5f + Random.Range(0f, 0.3f));
    }

    static Mesh GenerateTorus(float majorRadius, float minorRadius, int majorSegs, int minorSegs)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[(majorSegs + 1) * (minorSegs + 1)];
        int[] triangles = new int[majorSegs * minorSegs * 6];
        int t = 0;

        for (int i = 0; i <= majorSegs; i++)
        {
            float theta = i / (float)majorSegs * Mathf.PI * 2f;
            Vector3 center = new Vector3(Mathf.Cos(theta) * majorRadius, 0, Mathf.Sin(theta) * majorRadius);

            for (int j = 0; j <= minorSegs; j++)
            {
                float phi = j / (float)minorSegs * Mathf.PI * 2f;
                Vector3 normal = new Vector3(Mathf.Cos(theta) * Mathf.Cos(phi), Mathf.Sin(phi), Mathf.Sin(theta) * Mathf.Cos(phi));
                vertices[i * (minorSegs + 1) + j] = center + normal * minorRadius;

                if (i < majorSegs && j < minorSegs)
                {
                    int a = i * (minorSegs + 1) + j;
                    int b = a + 1;
                    int c = a + (minorSegs + 1);
                    int d = c + 1;
                    triangles[t++] = a;
                    triangles[t++] = b;
                    triangles[t++] = c;
                    triangles[t++] = b;
                    triangles[t++] = d;
                    triangles[t++] = c;
                }
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        return mesh;
    }

}