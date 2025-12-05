using System.Collections.Generic;
using UnityEngine;

public static class TubeMesh
{
    public static Mesh Build(
        List<Vector3> centerline,
        int radialSegments,
        int longitudinalSamples,
        AnimationCurve taper,
        float baseRadius,
        bool capStart = false,
        bool capEnd = false,
        bool flipFaces = false)
    {
        var pts = Resample(centerline, longitudinalSamples);
        int rings = pts.Count;
        radialSegments = Mathf.Max(3, radialSegments);

        var verts = new List<Vector3>(rings * (radialSegments + 1) + (capStart ? radialSegments + 2 : 0) + (capEnd ? radialSegments + 2 : 0));
        var norms = new List<Vector3>(verts.Capacity);
        var uvs = new List<Vector2>(verts.Capacity);
        var tris = new List<int>(rings * radialSegments * 6 + radialSegments * 3 * 2);

        Vector3 tPrev = Vector3.forward;
        Vector3 nPrev = Vector3.right;

        for (int i = 0; i < rings; i++)
        {
            float ti = (rings <= 1) ? 0f : i / (float)(rings - 1);
            Vector3 p = pts[i];
            Vector3 tang = Tangent(pts, i);

            if (i == 0) { tPrev = tang; nPrev = Ortho(tang); }
            else { nPrev = ParallelTransport(nPrev, tPrev, tang); tPrev = tang; }

            Vector3 binorm = Vector3.Cross(tang, nPrev).normalized;
            float r = Mathf.Max(0.0005f, baseRadius * (taper != null ? taper.Evaluate(ti) : 1f));

            for (int j = 0; j <= radialSegments; j++)
            {
                float a = (j / (float)radialSegments) * Mathf.PI * 2f;
                Vector3 offset = nPrev * Mathf.Cos(a) + binorm * Mathf.Sin(a);
                verts.Add(p + offset * r);
                norms.Add(offset.normalized);
                uvs.Add(new Vector2(j / (float)radialSegments, ti));
            }
        }

        int stride = radialSegments + 1;

        for (int i = 0; i < rings - 1; i++)
        {
            for (int j = 0; j < radialSegments; j++)
            {
                int a = i * stride + j;
                int b = (i + 1) * stride + j;
                int c = (i + 1) * stride + j + 1;
                int d = i * stride + j + 1;
                tris.Add(a); tris.Add(c); tris.Add(b);
                tris.Add(a); tris.Add(d); tris.Add(c);
            }
        }

        if (capStart)
        {
            Vector3 nCap = -Tangent(pts, 0).normalized;
            AddDiscCap(pts[0], nCap, 0, radialSegments, verts, norms, uvs, tris, invert: false);
        }
        if (capEnd)
        {
            Vector3 nCap = Tangent(pts, rings - 1).normalized;
            AddDiscCap(pts[rings - 1], nCap, (rings - 1) * stride, radialSegments, verts, norms, uvs, tris, invert: false);
        }

        var mesh = new Mesh();
        mesh.indexFormat = (verts.Count > 65000)
            ? UnityEngine.Rendering.IndexFormat.UInt32
            : UnityEngine.Rendering.IndexFormat.UInt16;

        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);

        if (flipFaces)
        {
            var t = mesh.triangles;
            for (int i = 0; i < t.Length; i += 3)
            {
                int tmp = t[i];
                t[i] = t[i + 1];
                t[i + 1] = tmp;
            }
            mesh.triangles = t;
            mesh.RecalculateTangents();
        }

        mesh.RecalculateBounds();
        return mesh;
    }

    static Vector3 ParallelTransport(Vector3 n, Vector3 tPrev, Vector3 tNext)
    {
        tPrev.Normalize(); tNext.Normalize();
        Vector3 v = Vector3.Cross(tPrev, tNext);
        float c = Vector3.Dot(tPrev, tNext);
        if (v.sqrMagnitude < 1e-12f) return n;
        float k = Mathf.Acos(Mathf.Clamp(c, -1f, 1f));
        Quaternion q = Quaternion.AngleAxis(k * Mathf.Rad2Deg, v.normalized);
        return (q * n).normalized;
    }

    static Vector3 Ortho(Vector3 v)
    {
        v.Normalize();
        if (Mathf.Abs(v.y) < 0.99f) return Vector3.Cross(v, Vector3.up).normalized;
        return Vector3.Cross(v, Vector3.right).normalized;
    }

    static List<Vector3> Resample(List<Vector3> src, int samplesPerSpan)
    {
        var dst = new List<Vector3>();
        if (src == null || src.Count < 2)
        {
            if (src != null) dst.AddRange(src);
            return dst;
        }

        float total = 0f;
        for (int i = 0; i < src.Count - 1; i++)
            total += Vector3.Distance(src[i], src[i + 1]);

        int totalSamples = Mathf.Max(2, (src.Count - 1) * Mathf.Max(2, samplesPerSpan));
        float step = total / (totalSamples - 1);

        dst.Add(src[0]);
        float acc = 0f;
        int si = 0;
        float segLen = Vector3.Distance(src[0], src[1]);

        for (int s = 1; s < totalSamples - 1; s++)
        {
            float target = s * step;
            while (acc + segLen < target && si < src.Count - 2)
            {
                acc += segLen; si++;
                segLen = Vector3.Distance(src[si], src[si + 1]);
            }
            float t = Mathf.InverseLerp(acc, acc + segLen, target);
            dst.Add(Vector3.Lerp(src[si], src[si + 1], t));
        }
        dst.Add(src[src.Count - 1]);
        return dst;
    }

    static Vector3 Tangent(List<Vector3> pts, int i)
    {
        if (pts.Count < 2) return Vector3.up;
        if (i <= 0) return (pts[1] - pts[0]).normalized;
        if (i >= pts.Count - 1) return (pts[pts.Count - 1] - pts[pts.Count - 2]).normalized;
        return (pts[i + 1] - pts[i - 1]).normalized;
    }

    static void MakeBasisFromNormal(Vector3 normal, out Vector3 u, out Vector3 v)
    {
        Vector3 n = normal.normalized;
        Vector3 t = Mathf.Abs(n.y) < 0.99f ? Vector3.up : Vector3.right;
        u = Vector3.Cross(t, n).normalized;
        v = Vector3.Cross(n, u).normalized;
    }

    static void AddDiscCap(
        Vector3 center, Vector3 normal, int ringStart, int radialSegments,
        List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, List<int> tris,
        bool invert)
    {
        normal.Normalize();

        int centerIndex = verts.Count;
        verts.Add(center);
        norms.Add(normal);
        uvs.Add(new Vector2(0.5f, 0.5f));

        MakeBasisFromNormal(normal, out var U, out var V);

        int ringCopyStart = verts.Count;
        for (int j = 0; j <= radialSegments; j++)
        {
            int src = ringStart + j;
            Vector3 pos = verts[src];
            Vector3 d = pos - center;
            float du = Vector3.Dot(d, U);
            float dv = Vector3.Dot(d, V);
            float r = Mathf.Sqrt(du * du + dv * dv);
            float u = (r < 1e-6f) ? 0.5f : (du / (2f * r) + 0.5f);
            float v = (r < 1e-6f) ? 0.5f : (dv / (2f * r) + 0.5f);
            verts.Add(pos);
            norms.Add(normal);
            uvs.Add(new Vector2(u, v));
        }

        for (int j = 0; j < radialSegments; j++)
        {
            int a = ringCopyStart + j;
            int b = ringCopyStart + j + 1;
            if (!invert) { tris.Add(centerIndex); tris.Add(b); tris.Add(a); }
            else { tris.Add(centerIndex); tris.Add(a); tris.Add(b); }
        }
    }
}
