using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SeaFloorGenerator : MonoBehaviour
{
    public int resolution = 60;
    public float noiseScale = 4f;
    public float heightMultiplier = 1.5f;
    public float offset = 0f;

    private Mesh mesh;

    void Start()
    {
        GenerateMesh();
    }

    void GenerateMesh()
    {
        mesh = new Mesh();
        mesh.name = "SeaFloor";

        GetComponent<MeshFilter>().mesh = mesh;

        Vector3 world = FlockManager.Instance.worldSize;

        int xVerts = resolution;
        int zVerts = resolution;

        Vector3[] vertices = new Vector3[xVerts * zVerts];
        int[] triangles = new int[(xVerts - 1) * (zVerts - 1) * 6];
        Vector2[] uvs = new Vector2[vertices.Length];

        float yBase = -world.y / 2f;

        for (int z = 0; z < zVerts; z++)
        {
            for (int x = 0; x < xVerts; x++)
            {
                float normX = (float)x / (xVerts - 1);
                float normZ = (float)z / (zVerts - 1);

                float worldX = Mathf.Lerp(-world.x / 2f, world.x / 2f, normX);
                float worldZ = Mathf.Lerp(-world.z / 2f, world.z / 2f, normZ);

                float noise = Mathf.PerlinNoise(
                    normX * noiseScale + offset,
                    normZ * noiseScale + offset
                );

                float height = (noise - 0.5f) * heightMultiplier;

                vertices[z * xVerts + x] = new Vector3(worldX, yBase + height, worldZ);
                uvs[z * xVerts + x] = new Vector2(normX, normZ);
            }
        }

        int t = 0;
        for (int z = 0; z < zVerts - 1; z++)
        {
            for (int x = 0; x < xVerts - 1; x++)
            {
                int i = z * xVerts + x;

                triangles[t++] = i;
                triangles[t++] = i + xVerts;
                triangles[t++] = i + 1;

                triangles[t++] = i + 1;
                triangles[t++] = i + xVerts;
                triangles[t++] = i + xVerts + 1;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        var renderer = GetComponent<MeshRenderer>();
        Material mat = new Material(Shader.Find("Standard"));

        mat.color = new Color(0.8f, 0.4f, 0.25f);

        renderer.material = mat;
    }
}
