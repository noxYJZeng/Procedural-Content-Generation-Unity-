using UnityEngine;
using System.Collections.Generic;

public class ObstacleManager : MonoBehaviour
{
    public static ObstacleManager Instance;

    public LayerMask obstacleMask;
    public bool autoScan = true;

    private List<Transform> obstacles = new List<Transform>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (autoScan)
            ScanAllObstacles();
    }

    public void ScanAllObstacles()
    {
        obstacles.Clear();

        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (((1 << obj.layer) & obstacleMask.value) != 0)
            {
                obstacles.Add(obj.transform);
            }
        }
    }

    public List<Transform> GetObstacles()
    {
        return obstacles;
    }
}
