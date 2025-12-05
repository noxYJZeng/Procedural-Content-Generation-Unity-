using UnityEngine;
using System.Collections.Generic;

public class FlockManager : MonoBehaviour
{
    public enum TrailType { Points, Lines }

    public GameObject jellyfishPrefab;
    public int creatureCount = 20;
    public Vector3 spawnBounds = new Vector3(10, 10, 10);

    public Vector3 worldSize = new Vector3(40, 30, 40);
    public bool useToroidal = false;
    public bool showWorldGizmos = true;

    public float minSpeed = 2f;
    public float maxSpeed = 8f;
    public float neighborDistance = 7f;
    public float avoidanceDistance = 2.5f;

    public bool enableFlockCentering = true;
    public bool enableVelocityMatching = true;
    public bool enableCollisionAvoidance = true;
    public bool enableWandering = true;
    public bool enableObstacleAvoidance = true;

    public float weightFlockCentering = 1.0f;
    public float weightVelocityMatching = 1.0f;
    public float weightCollisionAvoidance = 2.2f;
    public float weightWandering = 0.5f;
    public float weightObstacleAvoidance = 10.0f;

    public LayerMask obstacleMask;

    public bool enableTrails = true;
    public Color trailColor = new Color(0.8f, 0.4f, 0.25f);
    public TrailType trailType = TrailType.Points;

    public int pointsMaxCount = 8;
    public float pointsSpawnInterval = 0.18f;

    public int lineMaxCount = 30;
    public float lineMinDistance = 0.05f;

    private List<Jellyfish> flock = new List<Jellyfish>();
    public static FlockManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this);
        else
            Instance = this;

        if (obstacleMask.value == 0)
        {
            int layerIndex = LayerMask.NameToLayer("Obstacle");
            if (layerIndex >= 0) obstacleMask = 1 << layerIndex;
        }
    }

    void Start()
    {
        UpdateFlockCount();
        SyncJellyfishSettings();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            ScatterFlock();

        if (flock.Count != creatureCount)
            UpdateFlockCount();

        SyncJellyfishSettings();
    }

    void UpdateFlockCount()
    {
        while (flock.Count < creatureCount)
        {
            Vector3 pos = transform.position + new Vector3(
                Random.Range(-spawnBounds.x, spawnBounds.x),
                Random.Range(-spawnBounds.y, spawnBounds.y),
                Random.Range(-spawnBounds.z, spawnBounds.z)
            );

            GameObject obj = jellyfishPrefab != null ?
                Instantiate(jellyfishPrefab, pos, Quaternion.identity) :
                new GameObject("Fish_" + flock.Count);

            Jellyfish jelly = obj.GetComponent<Jellyfish>();
            if (jelly == null) jelly = obj.AddComponent<Jellyfish>();
            flock.Add(jelly);
        }

        while (flock.Count > creatureCount)
        {
            Jellyfish last = flock[flock.Count - 1];
            flock.RemoveAt(flock.Count - 1);
            if (last != null)
                Destroy(last.gameObject);
        }
    }

    void ScatterFlock()
    {
        foreach (Jellyfish jelly in flock)
        {
            jelly.transform.position = new Vector3(
                Random.Range(-worldSize.x / 2, worldSize.x / 2),
                Random.Range(-worldSize.y / 2, worldSize.y / 2),
                Random.Range(-worldSize.z / 2, worldSize.z / 2)
            );

            jelly.SetRandomVelocity();
            jelly.ClearTrailExternal();
        }
    }

    void SyncJellyfishSettings()
    {
        foreach (var j in flock)
        {
            if (j == null) continue;

            j.enableFlockCentering = enableFlockCentering;
            j.enableVelocityMatching = enableVelocityMatching;
            j.enableCollisionAvoidance = enableCollisionAvoidance;
            j.enableWandering = enableWandering;
            j.enableObstacleAvoidance = enableObstacleAvoidance;

            j.weightFlockCentering = weightFlockCentering;
            j.weightVelocityMatching = weightVelocityMatching;
            j.weightCollisionAvoidance = weightCollisionAvoidance;
            j.weightWandering = weightWandering;
            j.weightObstacleAvoidance = weightObstacleAvoidance;

            j.pointsMaxCount = pointsMaxCount;
            j.pointsSpawnInterval = pointsSpawnInterval;
            j.lineMaxCount = lineMaxCount;
            j.lineMinDistance = lineMinDistance;
        }
    }

    public List<Jellyfish> GetFlock() => flock;

    private void OnDrawGizmos()
    {
        if (showWorldGizmos)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(Vector3.zero, worldSize);
        }
    }
}
