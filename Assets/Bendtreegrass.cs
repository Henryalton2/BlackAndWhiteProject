using UnityEngine;

public class BillboardBend : MonoBehaviour
{
    public float bendAmount = 0.0005f;
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

        // Calculate horizontal distance from camera (squared)
        Vector3 camPos = cameraTransform.position;
        float dx = originalPosition.x - camPos.x;
        float dz = originalPosition.z - camPos.z;
        float distSq = dx * dx + dz * dz;

        // Apply bend to Y position
        Vector3 newPos = originalPosition;
        newPos.y -= distSq * bendAmount;
        transform.position = newPos;
    }
}