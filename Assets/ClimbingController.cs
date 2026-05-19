using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
public class ClimbingController : MonoBehaviour
{
    [Header("Climbing")]
    public KeyCode climbKey = KeyCode.E;
    public float climbSpeed = 3f;
    public float grabReach = 2f;
    public LayerMask climbableLayers = ~0;
    public Camera playerCamera;

    [Header("Rotation")]
    public float rotationSnapSpeed = 8f;

    [Header("Look")]
    public float lookSpeed = 2f;
    public float lookXLimit = 60f;

    [Header("Stamina")]
    public float maxStamina = 30f;
    public float staminaRecovery = 8f;

    public static bool isClimbing = false;
    public float Stamina { get; private set; }
    public float MaxStamina => maxStamina;

    private CharacterController cc;
    private PlayerMovement playerMovement;

    private Vector3 surfaceNormal;
    private Quaternion targetRotation;
    private float cameraRotationX;
    private float cameraRotationY;   // free left/right look while climbing

    private const float SurfaceOffset = 0.6f;
    private const float CastLift = 1.0f;
    private const float CastLength = 1.8f;
    private const float MaxCeilingAngle = 10f;

    private HashSet<Collider> selfColliders = new HashSet<Collider>();
    private RaycastHit[] hitBuffer = new RaycastHit[16];
    private Collider[] overlapBuffer = new Collider[16];

    private Text staminaText;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    void Start()
    {
        cc = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();
        Stamina = maxStamina;

        foreach (var col in GetComponentsInChildren<Collider>(true))
            selfColliders.Add(col);

        BuildStaminaUI();
    }

    // ── Raycast / overlap helpers that ignore own colliders ───────────────

    bool RaycastIgnoreSelf(Vector3 origin, Vector3 dir, out RaycastHit result,
                           float maxDist, LayerMask mask)
    {
        int count = Physics.RaycastNonAlloc(origin, dir, hitBuffer, maxDist, mask);
        float closest = float.MaxValue;
        result = default;
        bool found = false;

        for (int i = 0; i < count; i++)
        {
            if (selfColliders.Contains(hitBuffer[i].collider)) continue;
            if (hitBuffer[i].distance < closest)
            {
                closest = hitBuffer[i].distance;
                result = hitBuffer[i];
                found = true;
            }
        }
        return found;
    }

    // Returns true if any non-self collider is within radius of the given point.
    bool NearAnySurface(Vector3 point, float radius)
    {
        int count = Physics.OverlapSphereNonAlloc(point, radius, overlapBuffer, climbableLayers);
        for (int i = 0; i < count; i++)
            if (!selfColliders.Contains(overlapBuffer[i])) return true;
        return false;
    }

    // ── Update ────────────────────────────────────────────────────────────

    void Update()
    {
        if (!isClimbing && cc.isGrounded)
            Stamina = Mathf.Min(maxStamina, Stamina + staminaRecovery * Time.deltaTime);

        if (staminaText != null)
        {
            staminaText.gameObject.SetActive(isClimbing);
            if (isClimbing)
                staminaText.text = Stamina.ToString("F1") + "s";
        }

        if (!isClimbing)
        {
            if (Input.GetKeyDown(climbKey) && Stamina > 0f)
                TryStartClimbing();
            return;
        }

        // ── Active climbing ──────────────────────────────────────────────

        Stamina -= Time.deltaTime;
        if (Stamina <= 0f) { Stamina = 0f; StopClimbing(); return; }

        if (Input.GetKeyDown(climbKey)) { StopClimbing(); return; }

        // General proximity check — if the player has floated away from all
        // surfaces (glitch, edge case), kick them out immediately.
        if (!NearAnySurface(transform.position + Vector3.up, grabReach))
        {
            StopClimbing();
            return;
        }

        // WASD along the surface plane
        Vector3 moveForward = Vector3.ProjectOnPlane(transform.TransformDirection(Vector3.forward), surfaceNormal).normalized;
        Vector3 moveRight = Vector3.ProjectOnPlane(transform.TransformDirection(Vector3.right), surfaceNormal).normalized;
        Vector3 delta = (moveForward * Input.GetAxis("Vertical") +
                               moveRight * Input.GetAxis("Horizontal")) * climbSpeed * Time.deltaTime;

        Vector3 proposed = transform.position + delta;
        if (!Physics.Linecast(transform.position + surfaceNormal * 0.05f,
                              proposed + surfaceNormal * 0.05f, climbableLayers))
            transform.position = proposed;

        // Surface tracking — keeps player flush; exits at edges
        Vector3 castOrigin = transform.position + surfaceNormal * CastLift;
        if (RaycastIgnoreSelf(castOrigin, -surfaceNormal, out RaycastHit hit, CastLength, climbableLayers))
        {
            surfaceNormal = hit.normal;
            transform.position = hit.point + hit.normal * SurfaceOffset;
            SetTargetRotation(hit.normal);
        }
        else
        {
            StopClimbing();
            return;
        }

        // Smooth rotation perpendicular to wall
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSnapSpeed);

        // Camera look — X tilts up/down, Y rotates freely left/right
        cameraRotationX -= Input.GetAxis("Mouse Y") * lookSpeed;
        cameraRotationX = Mathf.Clamp(cameraRotationX, -lookXLimit, lookXLimit);
        cameraRotationY += Input.GetAxis("Mouse X") * lookSpeed;
        playerCamera.transform.localRotation = Quaternion.Euler(cameraRotationX, cameraRotationY, 0f);
    }

    // ── Start / Stop ──────────────────────────────────────────────────────

    void TryStartClimbing()
    {
        Vector3 center = transform.position + Vector3.up * 1.0f;
        float minY = Mathf.Sin(MaxCeilingAngle * Mathf.Deg2Rad);

        float bestDist = float.MaxValue;
        Vector3 bestNormal = Vector3.zero;
        Vector3 bestPoint = Vector3.zero;

        Vector3[] dirs = { transform.forward, -transform.forward, transform.right, -transform.right };
        foreach (Vector3 dir in dirs)
        {
            Vector3 castFrom = center + dir * grabReach;
            if (!RaycastIgnoreSelf(castFrom, -dir, out RaycastHit hit, grabReach, climbableLayers)) continue;
            if (hit.normal.y < -minY) continue;

            float dist = Vector3.Distance(center, hit.point);
            if (dist < bestDist) { bestDist = dist; bestNormal = hit.normal; bestPoint = hit.point; }
        }

        if (RaycastIgnoreSelf(transform.position + Vector3.up * 3f, Vector3.down,
                              out RaycastHit groundHit, 5f, climbableLayers))
        {
            if (groundHit.normal.y >= -minY)
            {
                float dist = Vector3.Distance(center, groundHit.point);
                if (dist < bestDist) { bestDist = dist; bestNormal = groundHit.normal; bestPoint = groundHit.point; }
            }
        }

        if (bestNormal == Vector3.zero) return;

        surfaceNormal = bestNormal;
        transform.position = bestPoint + bestNormal * SurfaceOffset;

        isClimbing = true;
        cc.enabled = false;

        SetTargetRotation(surfaceNormal);

        // Sync camera rotation — no snap on transition
        cameraRotationX = playerCamera.transform.localEulerAngles.x;
        if (cameraRotationX > 180f) cameraRotationX -= 360f;
        cameraRotationY = 0f;
    }

    void StopClimbing()
    {
        isClimbing = false;
        cc.enabled = true;

        // Use the camera's world yaw as the new body yaw so there's no snap
        float worldYaw = playerCamera.transform.eulerAngles.y;
        transform.rotation = Quaternion.Euler(0f, worldYaw, 0f);
        playerCamera.transform.localRotation = Quaternion.identity;
        cameraRotationX = 0f;
        cameraRotationY = 0f;

        playerMovement?.ResetVerticalVelocity();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    void SetTargetRotation(Vector3 normal)
    {
        float minY = Mathf.Sin(MaxCeilingAngle * Mathf.Deg2Rad);
        if (normal.y < -minY) return;

        Vector3 up = normal;
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, normal).normalized;
        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.ProjectOnPlane(Vector3.up, normal).normalized;
        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.ProjectOnPlane(Vector3.right, normal).normalized;

        Quaternion candidate = Quaternion.LookRotation(forward, up);
        if ((candidate * Vector3.up).y >= 0f)
            targetRotation = candidate;
    }

    void BuildStaminaUI()
    {
        GameObject canvasGO = new GameObject("ClimbStaminaCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject textGO = new GameObject("StaminaCountdown");
        textGO.transform.SetParent(canvasGO.transform, false);

        staminaText = textGO.AddComponent<Text>();
        staminaText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                        ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        staminaText.fontSize = 28;
        staminaText.fontStyle = FontStyle.Bold;
        staminaText.color = Color.white;
        staminaText.alignment = TextAnchor.MiddleCenter;

        RectTransform rt = textGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(220f, 50f);

        staminaText.gameObject.SetActive(false);
    }
}
