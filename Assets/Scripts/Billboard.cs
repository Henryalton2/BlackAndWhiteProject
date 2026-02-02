using UnityEngine;

public class Billboard : MonoBehaviour
{
    [SerializeField] private BillboardType billboardType;

    [Header("Distance Settings")]
    public bool useDistanceCheck = true;
    [SerializeField] private float billboardDistance = 50f;
    [SerializeField] private float transitionSpeed = 5f;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private bool showDebug = false;

    [Header("Lock Rotation")]
    [SerializeField] private bool lockX;
    [SerializeField] private bool lockY;
    [SerializeField] private bool lockZ;

    private Vector3 originalRotation;
    private Quaternion originalQuaternion;
    private bool wasInRange = false;

    public enum BillboardType { LookAtCamera, CameraForward };

    private void Awake()
    {
        originalRotation = transform.rotation.eulerAngles;
        originalQuaternion = transform.rotation;

        // Try to find player if not assigned
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
    }

    void LateUpdate()
    {
        if (Camera.main == null) return;

        // Get the target transform for distance checking (player or camera)
        Transform checkTransform = playerTransform != null ? playerTransform : Camera.main.transform;

        bool isInRange = true;

        // Check distance if distance checking is enabled
        if (useDistanceCheck && checkTransform != null)
        {
            float distance = Vector3.Distance(transform.position, checkTransform.position);

            if (showDebug)
            {
                Debug.Log($"{gameObject.name}: Distance = {distance:F2}, Billboard Distance = {billboardDistance}, Within Range = {distance <= billboardDistance}");
            }

            isInRange = distance <= billboardDistance;

            // If beyond billboard distance, smoothly return to original rotation
            if (!isInRange)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, originalQuaternion, Time.deltaTime * transitionSpeed);
                wasInRange = false;
                return;
            }
        }

        // Calculate target billboard rotation
        Quaternion targetRotation = transform.rotation;

        // Apply billboard effect
        switch (billboardType)
        {
            case BillboardType.LookAtCamera:
                
                Vector3 directionToCamera = Camera.main.transform.position - transform.position;
                targetRotation = Quaternion.LookRotation(directionToCamera);
                break;
            case BillboardType.CameraForward:
                
                targetRotation = Camera.main.transform.rotation;
                break;
            default:
                break;
        }
               
        Vector3 rotation = targetRotation.eulerAngles;
        if (lockX) { rotation.x = originalRotation.x; }
        if (lockY) { rotation.y = originalRotation.y; }
        if (lockZ) { rotation.z = originalRotation.z; }
        targetRotation = Quaternion.Euler(rotation);

        // Smooth transition when entering billboard range
        if (!wasInRange && isInRange)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * transitionSpeed);
        }
        else
        {
            transform.rotation = targetRotation;
        }

        wasInRange = isInRange;
    }
}