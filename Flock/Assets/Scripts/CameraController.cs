using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target;
    public float rotationSpeed = 50f;
    public float moveSpeed = 20f;
    public float fastMultiplier = 3f;
    public float distance = 60f;
    public float minDistance = 10f;
    public float maxDistance = 200f;
    public float scrollSpeed = 10f;
    public bool autoRotate = false;

    float yaw;
    float pitch;

    void Start()
    {
        if (target == null)
        {
            GameObject t = new GameObject("CameraTarget");
            t.transform.position = Vector3.zero;
            target = t.transform;
        }

        Vector3 dir = transform.position - target.position;
        distance = dir.magnitude;
        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            autoRotate = !autoRotate;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
            distance = Mathf.Clamp(distance - scroll * scrollSpeed, minDistance, maxDistance);

        if (Input.GetMouseButton(1))
        {
            yaw += Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
            pitch -= Input.GetAxis("Mouse Y") * rotationSpeed * 0.5f * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, -40f, 80f);
        }

        float speed = Input.GetKey(KeyCode.LeftShift) ? moveSpeed * fastMultiplier : moveSpeed;
        Vector3 move = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) move += transform.forward;
        if (Input.GetKey(KeyCode.S)) move -= transform.forward;
        if (Input.GetKey(KeyCode.A)) move -= transform.right;
        if (Input.GetKey(KeyCode.D)) move += transform.right;
        if (Input.GetKey(KeyCode.E)) move += Vector3.up;
        if (Input.GetKey(KeyCode.Q)) move -= Vector3.up;
        target.position += move * speed * Time.deltaTime;
    }

    void LateUpdate()
    {
        if (autoRotate && !Input.GetMouseButton(1))
            yaw += rotationSpeed * 1.5f * Time.deltaTime;

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 offset = rotation * new Vector3(0, 0, -distance);
        transform.position = target.position + offset;
        transform.LookAt(target);
    }
}
