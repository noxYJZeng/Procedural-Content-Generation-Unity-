using UnityEngine;

public class PlantSpawnerUI : MonoBehaviour
{
    [Header("Target")]
    public PlantSpawner spawner;

    [Header("Panel")]
    public bool showUI = true;
    public Vector2 anchor = new Vector2(10, 10);
    public float width = 260f;

    [Header("Options")]
    public bool autoApplyOnEnter = true;
    public bool rememberSeed = true;

    string seedStr;
    int seedCache;
    string prefsKey => $"{Application.productName}_plant_seed";

    void Awake()
    {
        if (spawner == null) spawner = GetComponent<PlantSpawner>();
        if (spawner == null)
        {
            Debug.LogWarning("[PlantSpawnerUI] PlantSpawner not found. Please assign it in Inspector.");
            enabled = false;
            return;
        }

        if (rememberSeed && PlayerPrefs.HasKey(prefsKey))
        {
            spawner.masterSeed = PlayerPrefs.GetInt(prefsKey, spawner.masterSeed);
        }
        seedCache = spawner.masterSeed;
        seedStr = seedCache.ToString();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ApplyAndRegenerate();
        }

        int delta = 0;
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        int step1 = shift ? 10 : 1;
        int step100 = shift ? 1000 : 100;

        if (Input.GetKeyDown(KeyCode.LeftArrow)) delta -= step1;
        if (Input.GetKeyDown(KeyCode.RightArrow)) delta += step1;
        if (Input.GetKeyDown(KeyCode.UpArrow)) delta += step100;
        if (Input.GetKeyDown(KeyCode.DownArrow)) delta -= step100;

        if (delta != 0)
        {
            seedCache += delta;
            seedStr = seedCache.ToString();
            ApplyAndRegenerate();
        }
    }

    void OnGUI()
    {
        if (!showUI) return;

        var rect = new Rect(anchor.x, anchor.y, width, 160f);
        GUILayout.BeginArea(rect, GUI.skin.window);
        GUILayout.Label("<b>Plant Runtime Controls</b>", Rich());

        GUILayout.Space(6);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Seed", GUILayout.Width(40));
        GUI.SetNextControlName("seedField");
        seedStr = GUILayout.TextField(seedStr, GUILayout.ExpandWidth(true));
        GUILayout.EndHorizontal();

        if (autoApplyOnEnter && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
        {
            ApplyAndRegenerate();
            GUI.FocusControl(null);
        }

        GUILayout.Space(4);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("-1000")) Step(-1000);
        if (GUILayout.Button("-100")) Step(-100);
        if (GUILayout.Button("-1")) Step(-1);
        if (GUILayout.Button("+1")) Step(+1);
        if (GUILayout.Button("+100")) Step(+100);
        if (GUILayout.Button("+1000")) Step(+1000);
        GUILayout.EndHorizontal();

        GUILayout.Space(4);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Randomize"))
        {
            seedCache = Random.Range(int.MinValue, int.MaxValue);
            seedStr = seedCache.ToString();
        }
        if (GUILayout.Button("Apply + Regenerate"))
        {
            ApplyAndRegenerate();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(4);
        GUILayout.Label($"Current: <b>{spawner.masterSeed}</b>", Rich());
        GUILayout.EndArea();
    }

    void Step(int d)
    {
        if (!int.TryParse(seedStr, out seedCache)) seedCache = spawner.masterSeed;
        seedCache += d;
        seedStr = seedCache.ToString();
    }

    void ApplyAndRegenerate()
    {
        if (!int.TryParse(seedStr, out seedCache))
        {
            seedCache = spawner.masterSeed;
            seedStr = seedCache.ToString();
        }

        spawner.masterSeed = seedCache;

        if (rememberSeed) PlayerPrefs.SetInt(prefsKey, spawner.masterSeed);

        spawner.Generate();
    }

    static GUIStyle rich;
    static GUIStyle Rich()
    {
        if (rich == null) { rich = new GUIStyle(GUI.skin.label) { richText = true }; }
        return rich;
    }
}
