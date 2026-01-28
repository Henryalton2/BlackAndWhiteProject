using UnityEngine;

public class BillboardBend : MonoBehaviour
{
    private Vector3 originalPosition;
    private Transform cameraTransform;

    void Start()
    {
        originalPosition = transform.position;
        cameraTransform = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        // Read bend from WorldBendTrigger
        float bendAmount = WorldBendTrigger.CurrentBend;

        // Calculate horizontal distance from camera
        Vector3 camPos = cameraTransform.position;
        float dx = transform.position.x - camPos.x; // use current X
        float dz = transform.position.z - camPos.z; // use current Z
        float distSq = dx * dx + dz * dz;

        // Apply bend only to Y-axis
        Vector3 newPos = transform.position;
        newPos.y = originalPosition.y - distSq * bendAmount; // keep X/Z free for other movement
        transform.position = newPos;
    }
}
