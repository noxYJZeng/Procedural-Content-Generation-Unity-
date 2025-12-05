using UnityEngine;

public class FlockStatusUI : MonoBehaviour
{
    public FlockManager flock;
    public CameraController cam;
    GUIStyle white;
    GUIStyle green;

    void OnGUI()
    {
        if (flock == null) flock = FindObjectOfType<FlockManager>();
        if (cam == null) cam = FindObjectOfType<CameraController>();

        if (white == null)
        {
            white = new GUIStyle(GUI.skin.label);
            white.fontSize = 13;
            white.normal.textColor = Color.white;
            white.alignment = TextAnchor.UpperCenter;

            green = new GUIStyle(GUI.skin.label);
            green.fontSize = 13;
            green.normal.textColor = Color.green;
            green.alignment = TextAnchor.UpperCenter;
        }

        var f = flock;
        if (f == null) return;

        float y = 5;
        float w = Screen.width;
        float h = 22;

        GUILayout.BeginArea(new Rect(0, y, w, h));
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        GUILayout.Label("Status: ", white);
        GUILayout.Label("FlockCentering", f.enableFlockCentering ? green : white);
        GUILayout.Label("  ");
        GUILayout.Label("VelocityMatching", f.enableVelocityMatching ? green : white);
        GUILayout.Label("  ");
        GUILayout.Label("CollisionAvoidance", f.enableCollisionAvoidance ? green : white);
        GUILayout.Label("  ");
        GUILayout.Label("Wandering", f.enableWandering ? green : white);
        GUILayout.Label("  ");
        GUILayout.Label("ObstacleAvoidance", f.enableObstacleAvoidance ? green : white);

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
        y += h;

        GUILayout.BeginArea(new Rect(0, y, w, h));
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        GUILayout.Label("[Space] Scatter flock", white);
        GUILayout.Label("   ");
        GUILayout.Label("[R]Camera Rotation  [Wheel] Zoom  [WASD/QE] Move", white);

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
        y += h;

        bool isPoints = f.trailType == FlockManager.TrailType.Points;
        bool isLines  = f.trailType == FlockManager.TrailType.Lines;

        GUILayout.BeginArea(new Rect(0, y, w, h));
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        GUILayout.Label("Trail: ", white);
        GUILayout.Label("Points", isPoints ? green : white);
        GUILayout.Label("   ");
        GUILayout.Label("Lines", isLines ? green : white);

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }
}
