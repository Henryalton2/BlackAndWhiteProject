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

    [Header("Long Range Billboard Refresh")]
    [SerializeField] private bool enableDistantRefresh = true;
    [SerializeField] private float refreshInterval = 8f;
    [SerializeField] private float refreshDuration = 0.25f;
    [Range(0f, 1f)]
    [SerializeField] private float refreshChance = 0.25f;

    private float nextRefreshTime;
    private float refreshEndTime;
    private bool forceBillboard = false;

    private Vector3 originalRotation;
    private Quaternion originalQuaternion;

    // 🔽 NEW: Cache the last valid billboard rotation
    private Quaternion lastBillboardRotation;

    private bool wasInRange = false;

    public enum BillboardType { LookAtCamera, CameraForward };

    private void Awake()
    {
        originalRotation = transform.rotation.eulerAngles;
        originalQuaternion = transform.rotation;

        // 🔽 Initialize last billboard rotation to starting rotation
        lastBillboardRotation = transform.rotation;

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        nextRefreshTime = Time.time + Random.Range(0f, refreshInterval);
    }

    void LateUpdate()
    {
        if (Camera.main == null) return;

        Transform checkTransform = playerTransform != null ? playerTransform : Camera.main.transform;
        bool isInRange = true;

        // 🔽 Distant refresh logic
        if (enableDistantRefresh && Time.time >= nextRefreshTime)
        {
            nextRefreshTime = Time.time + refreshInterval;

            if (Random.value <= refreshChance)
            {
                forceBillboard = true;
                refreshEndTime = Time.time + refreshDuration;
            }
        }

        if (forceBillboard && Time.time > refreshEndTime)
        {
            forceBillboard = false;
        }

        // Distance check (unless forced)
        if (useDistanceCheck && checkTransform != null && !forceBillboard)
        {
            float distance = Vector3.Distance(transform.position, checkTransform.position);

            if (showDebug)
            {
                Debug.Log($"{gameObject.name}: Distance = {distance:F2}, Billboard Distance = {billboardDistance}");
            }

            isInRange = distance <= billboardDistance;

            // 🔽 CHANGED: return to LAST billboarded rotation, not original
            if (!isInRange)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    lastBillboardRotation,
                    Time.deltaTime * transitionSpeed
                );

                wasInRange = false;
                return;
            }
        }

        // Calculate target billboard rotation
        Quaternion targetRotation = transform.rotation;

        switch (billboardType)
        {
            case BillboardType.LookAtCamera:
                Vector3 directionToCamera = Camera.main.transform.position - transform.position;
                targetRotation = Quaternion.LookRotation(directionToCamera);
                break;

            case BillboardType.CameraForward:
                targetRotation = Camera.main.transform.rotation;
                break;
        }

        Vector3 rotation = targetRotation.eulerAngles;
        if (lockX) rotation.x = originalRotation.x;
        if (lockY) rotation.y = originalRotation.y;
        if (lockZ) rotation.z = originalRotation.z;

        targetRotation = Quaternion.Euler(rotation);

        // Smooth entry
        if (!wasInRange && isInRange)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * transitionSpeed
            );
        }
        else
        {
            transform.rotation = targetRotation;
        }

        // 🔽 NEW: Cache this rotation as the new resting rotation
        lastBillboardRotation = transform.rotation;

        wasInRange = isInRange;
    }
}
