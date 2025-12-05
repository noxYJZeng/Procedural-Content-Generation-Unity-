using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class WorldBox : MonoBehaviour
{
    private BoxCollider boxCollider;

    void Start()
    {
        boxCollider = GetComponent<BoxCollider>();
        boxCollider.isTrigger = true;
        UpdateSize();
    }

    void Update()
    {
        UpdateSize();
    }

    void UpdateSize()
    {
        if (FlockManager.Instance != null)
        {
            boxCollider.size = FlockManager.Instance.worldSize;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1, 0, 0.2f);
        if (FlockManager.Instance != null)
            Gizmos.DrawWireCube(transform.position, FlockManager.Instance.worldSize);
    }
}
