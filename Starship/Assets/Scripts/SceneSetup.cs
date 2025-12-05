using UnityEngine;

public class SceneSetup : MonoBehaviour
{
    void Start()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(0f, 27.8675f, -112.814f);
            cam.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.05f, 0.2f);
            cam.fieldOfView = 60f;
        }

        Light dirLight = FindObjectOfType<Light>();
        if (dirLight != null)
        {
            dirLight.transform.rotation = Quaternion.Euler(50f, 50f, 0f);
            dirLight.intensity = 1.4f;
            dirLight.color = Color.white;
        }

        RenderSettings.ambientLight = new Color(0.1f, 0.15f, 0.25f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
    }
}
