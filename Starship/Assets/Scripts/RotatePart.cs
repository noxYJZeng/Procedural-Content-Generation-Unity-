using UnityEngine;

public class RotatePart : MonoBehaviour
{
    public Vector3 localAxis = Vector3.up;
    public float speedDegPerSec = 60f;

    void Update()
    {
        transform.Rotate(localAxis, speedDegPerSec * Time.deltaTime, Space.Self);
    }
}
