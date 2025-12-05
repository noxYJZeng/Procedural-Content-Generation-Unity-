using UnityEngine;
using System.Collections.Generic;

public class Jellyfish : MonoBehaviour
{
    public bool enableFlockCentering = true;
    public bool enableVelocityMatching = true;
    public bool enableCollisionAvoidance = true;
    public bool enableWandering = true;
    public bool enableObstacleAvoidance = true;
    public bool enableBoundarySteer = true;

    public float weightFlockCentering = 0.5f;
    public float weightVelocityMatching = 1.2f;
    public float weightCollisionAvoidance = 2.0f;
    public float weightWandering = 0.35f;
    public float weightObstacleAvoidance = 6f;
    public float weightBoundarySteer = 8f;

    public Vector3 velocity;
    private Vector3 desiredHeading;
    private float wanderSeed;

    private Transform tr;
    private Transform visualRoot;

    private Transform tailRoot;
    private Transform finLeft;
    private Transform finRight;
    private float finSeed;

    private List<GameObject> trailObjs = new List<GameObject>();
    private float trailTimer = 0f;

    public float pointsSpawnInterval = 0.18f;
    public int pointsMaxCount = 8;

    private LineRenderer line;
    private List<Vector3> linePoints = new List<Vector3>();
    public float lineMinDistance = 0.05f;
    public int lineMaxCount = 30;

    void Start()
    {
        tr = transform;

        if (tr.childCount == 0)
            CreateVisuals();

        SetRandomVelocity();
        desiredHeading = velocity.normalized;

        wanderSeed = Random.value * 10f;
        finSeed = Random.value * 5f;

        line = gameObject.AddComponent<LineRenderer>();
        line.enabled = false;
        line.positionCount = 0;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.numCapVertices = 6;
        line.numCornerVertices = 6;

        AnimationCurve w = new AnimationCurve();
        w.AddKey(0f, 0.02f);
        w.AddKey(0.5f, 0.09f);
        w.AddKey(1f, 0.02f);
        line.widthCurve = w;
    }

    public void SetRandomVelocity()
    {
        float speed = Random.Range(FlockManager.Instance.minSpeed, FlockManager.Instance.maxSpeed);
        velocity = Random.onUnitSphere * speed;
        desiredHeading = velocity.normalized;
    }

    public void ClearTrailExternal()
    {
        ClearTrail();
    }

    void Update()
    {
        var flock = FlockManager.Instance.GetFlock();

        Vector3 align = enableVelocityMatching ? CalculateAlignment(flock) * weightVelocityMatching : Vector3.zero;
        Vector3 coh   = enableFlockCentering  ? CalculateCohesion(flock)  * weightFlockCentering  : Vector3.zero;
        Vector3 sep   = enableCollisionAvoidance ? CalculateSeparation(flock)* weightCollisionAvoidance: Vector3.zero;
        Vector3 wanderForce = enableWandering ? CalculateWander() * weightWandering : Vector3.zero;
        Vector3 avoid = enableObstacleAvoidance ? CalculateObstacleAvoidance() * weightObstacleAvoidance : Vector3.zero;
        Vector3 bounds = enableBoundarySteer ? CalculateBoundarySteer() * weightBoundarySteer : Vector3.zero;

        Vector3 steeringDir = align + coh * 0.6f + wanderForce * 0.4f + avoid + bounds;

        if (steeringDir.sqrMagnitude > 0.001f)
            desiredHeading = Vector3.Slerp(desiredHeading, steeringDir.normalized, Time.deltaTime * 3.5f);

        float dt = Time.deltaTime;

        velocity = Vector3.Slerp(velocity, desiredHeading * velocity.magnitude, dt * 3f);
        velocity += sep * dt;
        ClampVelocity();
        tr.position += velocity * dt;

        if (velocity.sqrMagnitude > 0.0001f)
            tr.rotation = Quaternion.Slerp(tr.rotation, Quaternion.LookRotation(velocity), dt * 6f);

        AnimateTail();
        AnimateFins();

        UpdateTrail();
    }

    void UpdateTrail()
    {
        if (!FlockManager.Instance.enableTrails)
        {
            ClearTrail();
            return;
        }

        if (FlockManager.Instance.trailType == FlockManager.TrailType.Points)
        {
            line.enabled = false;
            linePoints.Clear();
            line.positionCount = 0;

            trailTimer += Time.deltaTime;
            if (trailTimer >= pointsSpawnInterval)
            {
                trailTimer = 0f;
                SpawnPointTrail();
            }
        }
        else
        {
            if (trailObjs.Count > 0)
            {
                foreach (var o in trailObjs)
                    if (o != null) Destroy(o);
                trailObjs.Clear();
            }

            UpdateSilkLine();
        }
    }

    void SpawnPointTrail()
    {
        GameObject o = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        o.transform.position = tr.position + -tr.forward * 0.5f;
        o.transform.localScale = Vector3.one * 0.08f;
        Destroy(o.GetComponent<Collider>());

        Renderer r = o.GetComponent<Renderer>();
        r.material = new Material(Shader.Find("Standard"));
        r.material.color = FlockManager.Instance.trailColor;

        trailObjs.Add(o);

        if (trailObjs.Count > pointsMaxCount)
        {
            Destroy(trailObjs[0]);
            trailObjs.RemoveAt(0);
        }
    }

    void UpdateSilkLine()
    {
        if (!line.enabled)
            line.enabled = true;

        Vector3 p = tr.position + -tr.forward * 0.5f;

        if (linePoints.Count == 0 ||
            Vector3.Distance(linePoints[linePoints.Count - 1], p) > lineMinDistance)
        {
            linePoints.Add(p);
            if (linePoints.Count > lineMaxCount)
                linePoints.RemoveAt(0);
        }

        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(FlockManager.Instance.trailColor, 0f),
                new GradientColorKey(FlockManager.Instance.trailColor * 1.3f, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        line.colorGradient = g;

        float speed01 = Mathf.Clamp01(velocity.magnitude / FlockManager.Instance.maxSpeed);
        line.widthMultiplier = Mathf.Lerp(0.8f, 1.2f, speed01);

        line.positionCount = linePoints.Count;
        for (int i = 0; i < linePoints.Count; i++)
            line.SetPosition(i, linePoints[i]);
    }

    void ClearTrail()
    {
        foreach (var o in trailObjs)
            if (o != null) Destroy(o);
        trailObjs.Clear();

        linePoints.Clear();
        line.positionCount = 0;
        line.enabled = false;
    }

    Vector3 CalculateAlignment(List<Jellyfish> flock)
    {
        Vector3 avg = Vector3.zero;
        int count = 0;

        foreach (var other in flock)
        {
            if (other == null || other == this) continue;

            float d = Vector3.Distance(tr.position, other.transform.position);
            if (d < FlockManager.Instance.neighborDistance)
            {
                avg += other.velocity.normalized;
                count++;
            }
        }

        return count == 0 ? Vector3.zero : avg.normalized;
    }

    Vector3 CalculateCohesion(List<Jellyfish> flock)
    {
        Vector3 center = Vector3.zero;
        int count = 0;

        foreach (var other in flock)
        {
            if (other == null || other == this) continue;

            float d = Vector3.Distance(tr.position, other.transform.position);
            if (d < FlockManager.Instance.neighborDistance)
            {
                center += other.transform.position;
                count++;
            }
        }

        if (count == 0) return Vector3.zero;

        center /= count;
        return (center - tr.position).normalized;
    }

    Vector3 CalculateSeparation(List<Jellyfish> flock)
    {
        Vector3 force = Vector3.zero;
        float minRad = 1.25f;

        foreach (var other in flock)
        {
            if (other == null || other == this) continue;

            Vector3 dir = tr.position - other.transform.position;
            float d = dir.magnitude;

            if (d < minRad)
                force += dir.normalized * (minRad - d) * 12f;
        }

        return force;
    }

    Vector3 CalculateWander()
    {
        float nX = Mathf.PerlinNoise(Time.time * 0.8f + wanderSeed, wanderSeed * 1.3f);
        float nZ = Mathf.PerlinNoise(Time.time * 0.8f + wanderSeed * 2.1f, wanderSeed * 0.9f);

        nX = (nX - 0.5f) * 2f;
        nZ = (nZ - 0.5f) * 2f;

        Vector3 dir = new Vector3(nX, 0, nZ);
        return dir.normalized * 0.8f;
    }

    Vector3 CalculateObstacleAvoidance()
    {
        LayerMask mask = FlockManager.Instance.obstacleMask;

        float detectDist = 15f;
        float sideOffset = 2f;
        float angleOffset = 25f;
        Vector3 fwd = velocity.normalized;

        if (Physics.Raycast(tr.position, fwd, out RaycastHit hit, detectDist, mask))
            return hit.normal * 1.8f;

        Vector3 leftDir = Quaternion.Euler(0, -angleOffset, 0) * fwd;
        Vector3 leftOrigin = tr.position - tr.right * sideOffset;
        if (Physics.Raycast(leftOrigin, leftDir, out hit, detectDist * 0.8f, mask))
            return hit.normal * 2f;

        Vector3 rightDir = Quaternion.Euler(0, angleOffset, 0) * fwd;
        Vector3 rightOrigin = tr.position + tr.right * sideOffset;
        if (Physics.Raycast(rightOrigin, rightDir, out hit, detectDist * 0.8f, mask))
            return hit.normal * 2f;

        return Vector3.zero;
    }

    Vector3 CalculateBoundarySteer()
    {
        Vector3 half = FlockManager.Instance.worldSize * 0.5f;
        Vector3 pos = tr.position;
        float safe = 5f;

        Vector3 steer = Vector3.zero;

        if (pos.x > half.x - safe) steer += Vector3.left;
        if (pos.x < -half.x + safe) steer += Vector3.right;
        if (pos.y > half.y - safe) steer += Vector3.down;
        if (pos.y < -half.y + safe) steer += Vector3.up;
        if (pos.z > half.z - safe) steer += Vector3.back;
        if (pos.z < -half.z + safe) steer += Vector3.forward;

        return steer.normalized;
    }

    void ClampVelocity()
    {
        float sp = velocity.magnitude;

        if (sp > FlockManager.Instance.maxSpeed)
            velocity = velocity.normalized * FlockManager.Instance.maxSpeed;

        if (sp < FlockManager.Instance.minSpeed)
            velocity = velocity.normalized * FlockManager.Instance.minSpeed;
    }

    void CreateVisuals()
    {
        visualRoot = new GameObject("FishVisual").transform;
        visualRoot.SetParent(tr);
        visualRoot.localPosition = Vector3.zero;

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.transform.SetParent(visualRoot);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(0.4f, 0.25f, 1.0f);
        Destroy(body.GetComponent<Collider>());
        Material bodyMat = new Material(Shader.Find("Standard"));
        bodyMat.color = new Color(0.15f, 0.45f, 0.95f);
        body.GetComponent<Renderer>().material = bodyMat;

        Material tailMat = new Material(Shader.Find("Standard"));
        tailMat.color = new Color(0.05f, 0.2f, 0.5f);

        GameObject tailObj = new GameObject("TailRoot");
        tailObj.transform.SetParent(visualRoot);
        tailObj.transform.localPosition = new Vector3(0, 0, -0.7f);
        tailRoot = tailObj.transform;

        CreateTailFin(tailRoot, tailMat, true);
        CreateTailFin(tailRoot, tailMat, false);

        Material finMat = new Material(Shader.Find("Standard"));
        finMat.color = new Color(0.30f, 0.75f, 1f);

        finLeft  = CreateChestFin(visualRoot, finMat, true);
        finRight = CreateChestFin(visualRoot, finMat, false);
    }

    Transform CreateChestFin(Transform parent, Material mat, bool isLeft)
    {
        GameObject finRoot = new GameObject(isLeft ? "FinRoot_L" : "FinRoot_R");
        finRoot.transform.SetParent(parent);

        float side = isLeft ? -1f : 1f;

        finRoot.transform.localPosition = new Vector3(0.30f * side, 0.0f, 0.02f);

        GameObject finMesh = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        finMesh.transform.SetParent(finRoot.transform);

        finMesh.transform.localPosition = new Vector3(0.04f * side, 0, 0);
        finMesh.transform.localScale = new Vector3(0.24f, 0.08f, 0.32f);

        Destroy(finMesh.GetComponent<Collider>());
        finMesh.GetComponent<Renderer>().material = mat;

        return finRoot.transform;
    }

    void CreateTailFin(Transform parent, Material mat, bool left)
    {
        GameObject fin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        fin.transform.SetParent(parent);

        float side = left ? -1f : 1f;
        fin.transform.localPosition = new Vector3(0.15f * side, 0, 0);
        fin.transform.localScale = new Vector3(0.05f, 0.25f, 0.12f);
        fin.transform.localRotation = Quaternion.Euler(0, side * 20f, 90);

        Destroy(fin.GetComponent<Collider>());
        fin.GetComponent<Renderer>().material = mat;
    }

    void AnimateTail()
    {
        float speedFactor = Mathf.Clamp(velocity.magnitude, 0.5f, 5f);
        float angle = Mathf.Sin(Time.time * 9f * speedFactor) * 25f;
        tailRoot.localRotation = Quaternion.Euler(0, angle, 0);
    }

    void AnimateFins()
    {
        if (!finLeft || !finRight) return;

        float speedFactor = Mathf.Clamp(velocity.magnitude * 0.5f, 0.8f, 1.8f);
        float flap = Mathf.Sin(Time.time * 10f * speedFactor + finSeed) * 45f;

        finLeft.localRotation  = Quaternion.Euler(0, 0, flap);
        finRight.localRotation = Quaternion.Euler(0, 0, -flap);
    }
}
