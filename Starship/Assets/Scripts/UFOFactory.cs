using UnityEngine;

public class UFOFactory : MonoBehaviour
{
    [Header("Available Body Textures (drag in Unity)")]
    public Texture2D[] availableTextures;

    public void BuildUFO(int seed)
    {
        System.Random rand = new System.Random(seed);

        GameObject body = new GameObject("Body");
        body.transform.parent = transform;
        MeshFilter bodyFilter = body.AddComponent<MeshFilter>();
        MeshRenderer bodyRenderer = body.AddComponent<MeshRenderer>();
        bodyFilter.mesh = BezierPatchGenerator.GenerateBody(rand);

        Material bodyMat = new Material(Shader.Find("Standard"));
        bodyMat.name = "UFO_Body_Instance";
        ForceOpaque(bodyMat);
        bodyMat.color = Color.white;
        bodyMat.SetFloat("_Metallic", 0.6f);
        bodyMat.SetFloat("_Glossiness", 0.7f);

        if (availableTextures != null && availableTextures.Length > 0)
        {
            Texture2D tex = availableTextures[rand.Next(0, availableTextures.Length)];
            if (tex != null)
            {
                bodyMat.mainTexture = tex;
                bodyMat.SetTexture("_MainTex", tex);

                float scale = 0.3f + (float)rand.NextDouble() * 0.4f;
                bodyMat.mainTextureScale = new Vector2(scale, scale);
                bodyMat.mainTextureOffset = Vector2.zero;

                bodyMat.DisableKeyword("_EMISSION");
                bodyMat.SetColor("_EmissionColor", Color.black);
                bodyMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
        }

        bodyRenderer.material = bodyMat;
        body.transform.localPosition = new Vector3(0, 10f, 0);
        body.transform.localScale = new Vector3(3f, 2f, 3f);

        GameObject domeObj = new GameObject("Dome");
        domeObj.transform.parent = transform;
        MeshFilter domeFilter = domeObj.AddComponent<MeshFilter>();
        MeshRenderer domeRenderer = domeObj.AddComponent<MeshRenderer>();

        int domeType = rand.Next(0, 3);
        domeFilter.mesh = BezierPatchGenerator.GenerateDome(rand, domeType);

        Color randomColor = new Color(
            (float)rand.NextDouble(),
            (float)rand.NextDouble(),
            (float)rand.NextDouble(),
            0.25f + (float)rand.NextDouble() * 0.35f
        );

        Material domeMat = new Material(Shader.Find("Standard"));
        domeMat.name = "UFO_Dome_Instance";
        domeMat.color = randomColor;
        domeMat.SetFloat("_Metallic", 0.05f);
        domeMat.SetFloat("_Glossiness", 0.95f);
        SetMaterialTransparent(domeMat);
        domeMat.EnableKeyword("_EMISSION");
        domeMat.SetColor("_EmissionColor", randomColor * 0.6f);

        domeRenderer.material = domeMat;

        domeObj.transform.localPosition = new Vector3(0, 13f, 0);
        float domeStretch = (domeType == 1) ? 3.2f : (domeType == 2 ? 2.0f : 2.6f);
        domeObj.transform.localScale = new Vector3(2.5f, domeStretch, 2.5f);

        var floatMotion = gameObject.AddComponent<FloatMotion>();
        floatMotion.amplitude = 0.35f + 0.1f * (float)rand.NextDouble();
        floatMotion.frequency = 0.5f + 0.3f * (float)rand.NextDouble();
        floatMotion.phase = (float)rand.NextDouble();

        bool hasPod = rand.NextDouble() > 0.4;
        float engineY;
        if (hasPod)
        {
            GameObject pod = new GameObject("LowerPod");
            pod.transform.parent = transform;
            MeshFilter podFilter = pod.AddComponent<MeshFilter>();
            MeshRenderer podRenderer = pod.AddComponent<MeshRenderer>();

            int podVariant = rand.Next(0, 3);
            podFilter.mesh = BezierPatchGenerator.GenerateLowerPod(rand, podVariant);

            Color podColor = RandomColorCool(rand);
            Material podMat = new Material(Shader.Find("Standard"));
            podMat.name = "UFO_Pod_Instance";
            ForceOpaque(podMat);
            podMat.color = podColor;
            podMat.SetFloat("_Metallic", 0.6f);
            podMat.SetFloat("_Glossiness", 0.7f);
            podRenderer.material = podMat;

            float offsetY = (podVariant == 0) ? 8.4f : (podVariant == 1 ? 8.0f : 8.6f);
            pod.transform.localPosition = new Vector3(0, offsetY, 0);
            pod.transform.localScale = new Vector3(2.2f, 1.4f, 2.2f);

            engineY = offsetY - 1.3f;
        }
        else
        {
            engineY = 8.5f;
        }

        GameObject engine = BezierPatchGenerator.GenerateEngineCore(rand, transform);
        var pulse = engine.GetComponent<EnginePulse>();
        int v = (pulse != null) ? pulse.variant : 0;
        float extraDown = (v == 0) ? -2.6f : (v == 2) ? -3.2f : -2.0f;
        engine.transform.localPosition = new Vector3(0, engineY + extraDown, 0);

        var rotator = engine.AddComponent<RotatePart>();
        rotator.localAxis = Vector3.up;
        rotator.speedDegPerSec = 45f + 30f * (float)rand.NextDouble();
    }

    private static void ForceOpaque(Material mat)
    {
        mat.SetFloat("_Mode", 0);
        mat.SetOverrideTag("RenderType", "");
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        mat.SetInt("_ZWrite", 1);

        mat.DisableKeyword("_ALPHATEST_ON");
        mat.DisableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.DisableKeyword("_EMISSION");
        mat.renderQueue = -1;

        if (mat.HasProperty("_EmissionColor"))
            mat.SetColor("_EmissionColor", Color.black);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
    }

    private static void SetMaterialTransparent(Material mat)
    {
        mat.SetFloat("_Mode", 3);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
    }

    private static Color RandomColorCool(System.Random rand)
    {
        float h = 0.5f + 0.25f * (float)rand.NextDouble();
        float s = 0.4f + 0.3f * (float)rand.NextDouble();
        float v = 0.8f + 0.2f * (float)rand.NextDouble();
        return Color.HSVToRGB(h, s, v);
    }
}
