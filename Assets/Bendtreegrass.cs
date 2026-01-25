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
        float dx = originalPosition.x - camPos.x;
        float dz = originalPosition.z - camPos.z;
        float distSq = dx * dx + dz * dz;

        // Apply bend
        Vector3 newPos = originalPosition;
        newPos.y -= distSq * bendAmount;
        transform.position = newPos;
    }
}
