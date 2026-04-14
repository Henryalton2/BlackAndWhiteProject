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

    // ─────────────────────────────────────────────
    //  SPRITE ANGLE SWAPPING
    // ─────────────────────────────────────────────
    [Header("Sprite Angle Swapping")]
    [SerializeField] private bool enableSpriteSwapping = false;

    [Tooltip("The SpriteRenderer on this object (or a child) whose sprite will be swapped.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("4-direction mode uses Front/Back/Left/Right.\n8-direction mode also uses the diagonal slots.")]
    [SerializeField] private DirectionMode directionMode = DirectionMode.FourWay;

    [Tooltip("Optional: assign a separate GameObject (NOT a child of this object) whose forward " +
             "represents where the NPC is actually facing. If empty, uses World Facing Direction.")]
    [SerializeField] private Transform facingRoot;

    [Tooltip("World-space direction the NPC is facing. Call SetFacingDirection() from your NPC AI to drive this.")]
    [SerializeField] private Vector3 worldFacingDirection = Vector3.forward;

    [Tooltip("Which local axis of Facing Root is forward. Only used when Facing Root is assigned.")]
    [SerializeField] private ForwardAxis npcForwardAxis = ForwardAxis.PositiveZ;

    [Header("  4-Way Sprites")]
    [SerializeField] private Sprite frontSprite;   // Camera is in front of NPC  (NPC faces camera)
    [SerializeField] private Sprite backSprite;    // Camera is behind NPC
    [SerializeField] private Sprite rightSprite;   // Camera is to NPC's right
    [SerializeField] private Sprite leftSprite;    // Camera is to NPC's left

    [Header("  8-Way Sprites (optional)")]
    [Tooltip("Only used when Direction Mode is set to EightWay.")]
    [SerializeField] private Sprite frontRightSprite;
    [SerializeField] private Sprite frontLeftSprite;
    [SerializeField] private Sprite backRightSprite;
    [SerializeField] private Sprite backLeftSprite;

    // ─────────────────────────────────────────────

    private float nextRefreshTime;
    private float refreshEndTime;
    private bool forceBillboard = false;

    private Vector3 originalRotation;
    private Quaternion originalQuaternion;
    private Quaternion lastBillboardRotation;
    private bool wasInRange = false;

    private Sprite currentSprite;

    public enum BillboardType { LookAtCamera, CameraForward }
    public enum DirectionMode { FourWay, EightWay }
    public enum ForwardAxis { PositiveZ, NegativeZ, PositiveX, NegativeX }

    // ──────────────────────────────────────────────────────────────────────────
    //  8-direction angle boundaries (centre of each 45° slice)
    //
    //   0°  = front   (camera directly ahead of NPC)
    //  45°  = front-right
    //  90°  = right
    // 135°  = back-right
    // 180°  = back    (camera directly behind NPC)
    // -135° = back-left
    //  -90° = left
    //  -45° = front-left
    // ──────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        originalRotation = transform.rotation.eulerAngles;
        originalQuaternion = transform.rotation;
        lastBillboardRotation = transform.rotation;

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        // Auto-find SpriteRenderer if not assigned
        if (enableSpriteSwapping && spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        nextRefreshTime = Time.time + Random.Range(0f, refreshInterval);
    }

    void LateUpdate()
    {
        if (Camera.main == null) return;

        Transform checkTransform = playerTransform != null ? playerTransform : Camera.main.transform;
        bool isInRange = true;

        // ── Distant refresh logic ──────────────────────────────────────────
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
            forceBillboard = false;

        // ── Distance check ─────────────────────────────────────────────────
        if (useDistanceCheck && checkTransform != null && !forceBillboard)
        {
            float distance = Vector3.Distance(transform.position, checkTransform.position);

            if (showDebug)
                Debug.Log($"{gameObject.name}: Distance = {distance:F2}, Billboard Distance = {billboardDistance}");

            isInRange = distance <= billboardDistance;

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

        // ── Billboard rotation ─────────────────────────────────────────────
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

        lastBillboardRotation = transform.rotation;
        wasInRange = isInRange;

        // ── Sprite swapping ────────────────────────────────────────────────
        if (enableSpriteSwapping && spriteRenderer != null)
            UpdateDirectionalSprite();
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  Sprite Swapping Logic
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Calculates the signed horizontal angle between the NPC's facing direction
    /// and the direction toward the camera, then picks the matching sprite.
    ///
    ///   Positive angle → camera is to the NPC's RIGHT
    ///   Negative angle → camera is to the NPC's LEFT
    ///   ~0°             → camera is directly IN FRONT of the NPC
    ///   ~±180°          → camera is directly BEHIND the NPC
    /// </summary>
    private void UpdateDirectionalSprite()
    {
        // Build the NPC's logical forward vector in world space
        Vector3 npcForward = GetNpcForward();

        // Direction from NPC to camera, flattened to the horizontal plane
        Vector3 toCamera = Camera.main.transform.position - transform.position;
        toCamera.y = 0f;
        if (toCamera.sqrMagnitude < 0.001f) return; // Camera is directly above/below — skip

        npcForward.y = 0f;
        if (npcForward.sqrMagnitude < 0.001f) return;

        // Signed angle: positive = camera to NPC's right, negative = NPC's left
        float angle = Vector3.SignedAngle(npcForward, toCamera, Vector3.up);

        Sprite chosen = PickSprite(angle);

        if (chosen != null && chosen != currentSprite)
        {
            spriteRenderer.sprite = chosen;
            currentSprite = chosen;
        }

        if (showDebug)
            Debug.Log($"{gameObject.name}: Angle to cam = {angle:F1}°  →  {(chosen != null ? chosen.name : "null")}");
    }

    private Sprite PickSprite(float angle)
    {
        if (directionMode == DirectionMode.FourWay)
        {
            // ──────────────────────────────
            //  4-way:  45° boundary slices
            //   Front:  -45 .. 45
            //   Right:   45 .. 135
            //   Back:   135 .. 180  /  -180 .. -135
            //   Left:  -135 .. -45
            // ──────────────────────────────
            float abs = Mathf.Abs(angle);

            if (abs <= 45f)
                return frontSprite;
            else if (abs >= 135f)
                return backSprite;
            else if (angle > 0f)
                return rightSprite;
            else
                return leftSprite;
        }
        else // EightWay
        {
            // ──────────────────────────────
            //  8-way:  22.5° boundary slices
            // ──────────────────────────────
            float abs = Mathf.Abs(angle);

            if (abs <= 22.5f)
                return frontSprite;
            else if (abs <= 67.5f)
                return angle > 0f ? frontRightSprite : frontLeftSprite;
            else if (abs <= 112.5f)
                return angle > 0f ? rightSprite : leftSprite;
            else if (abs <= 157.5f)
                return angle > 0f ? backRightSprite : backLeftSprite;
            else
                return backSprite;
        }
    }

    /// <summary>Returns the NPC's world-space forward vector, independent of billboard rotation.</summary>
    private Vector3 GetNpcForward()
    {
        // Priority 1: a dedicated facing Transform (unaffected by billboard rotation)
        if (facingRoot != null)
        {
            switch (npcForwardAxis)
            {
                case ForwardAxis.NegativeZ: return -facingRoot.forward;
                case ForwardAxis.PositiveX: return facingRoot.right;
                case ForwardAxis.NegativeX: return -facingRoot.right;
                default: return facingRoot.forward;
            }
        }

        // Priority 2: manually set world-space direction (driven by NPC AI at runtime)
        if (worldFacingDirection.sqrMagnitude > 0.001f)
            return worldFacingDirection.normalized;

        // Fallback: world forward (NPC is stationary and no facing set)
        return Vector3.forward;
    }

    /// <summary>
    /// Call this from your NPC movement/AI script whenever the NPC changes direction.
    /// Example: billboard.SetFacingDirection(agent.velocity.normalized);
    /// </summary>
    public void SetFacingDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude > 0.001f)
            worldFacingDirection = direction.normalized;
    }
}