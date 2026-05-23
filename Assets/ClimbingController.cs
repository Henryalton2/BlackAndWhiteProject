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

    [Header("Climb Feel")]
    [Range(0.1f, 2f)]   public float surfaceOffset     = 0.6f;
    [Range(0.1f, 1.5f)] public float climbStepDistance = 0.45f;
    [Range(0.1f, 1f)]   public float climbStepCooldown = 0.35f;

    [Header("Auto Climb")]
    public bool autoClimb = true;
    [Range(30f, 85f)] public float autoClimbSlopeAngle = 60f;   // should be above PlayerMovement.maxJumpSlopeAngle (45°)
    [Range(5f, 45f)]  public float stopClimbAngle      = 30f;   // stop climbing when surface is this flat (separate from auto-climb threshold)

    [Header("Rotation")]
    public float rotationSnapSpeed = 8f;

    [Header("Look")]
    public float lookSpeed = 2f;
    public float lookXLimit = 60f;

    [Header("Wall Jump")]
    public float wallJumpOutForce = 6f;
    public float wallJumpUpForce  = 7f;

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

    private const float CastLift        = 1.0f;
    private const float MaxCeilingAngle = 10f;

    // Grab lerp state
    private Vector3 _grabFrom     = Vector3.zero;
    private Vector3 _grabTo       = Vector3.zero;
    private float   _grabProgress = 1f;   // 1 = settled, ready for next grab
    private int     _armSide      = 1;

    private float _autoClimbCooldown = 0f;
    private const float AutoClimbCooldownDuration = 0.6f;

    [Header("Grace Period")]
    [Range(0f, 3f)] public float climbGracePeriod = 0.25f;

    private float _climbStartTime = 0f;

    private HashSet<Collider> selfColliders = new HashSet<Collider>();
    private RaycastHit[] hitBuffer = new RaycastHit[16];
    private Collider[] overlapBuffer = new Collider[16];

    private RectTransform staminaBarFillRect;
    private const float BarMaxWidth = 150f;
    private const float BarHeight   = 10f;

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

        if (staminaBarFillRect != null)
        {
            float fraction = maxStamina > 0f ? Stamina / maxStamina : 0f;
            staminaBarFillRect.sizeDelta = new Vector2(BarMaxWidth * fraction, BarHeight);
        }

        if (!isClimbing)
        {
            if (_autoClimbCooldown > 0f)
                _autoClimbCooldown -= Time.deltaTime;

            if (Input.GetKeyDown(climbKey) && Stamina > 0f)
                TryStartClimbing();
            return;
        }

        // ── Active climbing ──────────────────────────────────────────────

        Stamina -= Time.deltaTime;
        if (Stamina <= 0f) { Stamina = 0f; StopClimbing(); return; }

        if (Input.GetKeyDown(climbKey)) { StopClimbing(); return; }

        // Wall jump — only on steep surfaces
        if (Input.GetButtonDown("Jump"))
        {
            if (Vector3.Angle(surfaceNormal, Vector3.up) > autoClimbSlopeAngle)
            {
                StopClimbing();
                playerMovement?.SetVerticalVelocity(wallJumpUpForce);
                playerMovement?.ApplyExternalVelocity(surfaceNormal * wallJumpOutForce);
            }
            return;
        }

        // General proximity check — if the player has floated away from all
        // surfaces (glitch, edge case), kick them out immediately.
        if (!NearAnySurface(transform.position + Vector3.up, grabReach))
        {
            StopClimbing();
            return;
        }

        // WASD anchored to world directions on the surface:
        // W/S = up/down the wall (against/with gravity), A/D = left/right across it.
        // This is the same regardless of which angle the player approached from.
        Vector3 wallUp = Vector3.ProjectOnPlane(Vector3.up, surfaceNormal).normalized;
        if (wallUp.sqrMagnitude < 0.01f)   // nearly flat surface — fall back to body forward
            wallUp = Vector3.ProjectOnPlane(transform.forward, surfaceNormal).normalized;
        Vector3 wallRight = Vector3.Cross(surfaceNormal, wallUp).normalized;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        bool hasInput = Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f;
        bool settled  = _grabProgress >= 1f;

        // During the entry lerp the player may still be up to grabReach away from
        // the wall, so extend the cast to cover the full approach distance.
        // Once settled on the wall, the shorter range is sufficient.
        float castLength = settled
            ? surfaceOffset + CastLift + 0.5f
            : grabReach    + CastLift + 0.5f;

        // ── Fire a new grab when settled and input held ──────────────────
        if (settled && hasInput)
        {
            Vector3 inputDir = (wallUp * v + wallRight * h).normalized;
            Vector3 sway     = wallRight * (_armSide * 0.04f);
            Vector3 proposed = transform.position + inputDir * climbStepDistance + sway;

            // Cast from the proposed spot back to the wall to find the exact
            // snap point — this means _grabTo is always on the surface.
            Vector3 probeCast = proposed + surfaceNormal * CastLift;
            if (RaycastIgnoreSelf(probeCast, -surfaceNormal, out RaycastHit grabHit, castLength, climbableLayers))
            {
                _grabFrom     = transform.position;
                _grabTo       = grabHit.point + grabHit.normal * surfaceOffset;
                _grabProgress = 0f;
                _armSide      = -_armSide;
            }
            else if (v > 0.1f)
            {
                // Wall face ended above — probe downward to find a ledge to pull onto.
                Vector3 ledgeOrigin = proposed + Vector3.up * CastLift;
                float   ledgeRange  = CastLift * 2f + climbStepDistance;
                if (RaycastIgnoreSelf(ledgeOrigin, Vector3.down, out RaycastHit ledgeHit, ledgeRange, climbableLayers))
                {
                    _grabFrom     = transform.position;
                    _grabTo       = ledgeHit.point + Vector3.up * 0.05f;  // feet just above ledge
                    _grabProgress = 0f;
                    _armSide      = -_armSide;
                }
            }
        }

        // ── Smooth lerp toward grab target (SmoothStep = slow→fast→slow) ─
        if (_grabProgress < 1f)
        {
            _grabProgress += Time.deltaTime / climbStepCooldown;
            _grabProgress  = Mathf.Clamp01(_grabProgress);
            float t = Mathf.SmoothStep(0f, 1f, _grabProgress);
            transform.position = Vector3.Lerp(_grabFrom, _grabTo, t);
        }

        // ── Surface tracking ─────────────────────────────────────────────
        // Always update the normal (keeps rotation correct on curved walls).
        // Only snap position when settled — never fight the lerp.
        Vector3 castOrigin = transform.position + surfaceNormal * CastLift;
        if (RaycastIgnoreSelf(castOrigin, -surfaceNormal, out RaycastHit hit, castLength, climbableLayers))
        {
            // If the surface we're now tracking is flat enough to walk on, the player
            // has crested a ledge — stop climbing. Require settled so we don't stop
            // mid-lerp while pulling onto the ledge, and skip the entry grace period
            // to avoid false hits from terrain behind the player during the approach.
            float surfaceAngle = Vector3.Angle(hit.normal, Vector3.up);
            if (surfaceAngle < stopClimbAngle && settled && Time.time - _climbStartTime > climbGracePeriod)
            {
                StopClimbing();
                return;
            }

            surfaceNormal = hit.normal;
            if (settled)
                transform.position = hit.point + hit.normal * surfaceOffset;
            SetTargetRotation(hit.normal);
        }
        else
        {
            // Allow a short grace window on entry — the cast can miss for one or two
            // frames while the player settles against the surface.
            if (Time.time - _climbStartTime > climbGracePeriod)
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

    void TryStartClimbing(bool requireFacing = true)
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

            // For manual key press only: wall must be roughly in front of the player.
            // Auto-climb skips this — it already verified the input direction.
            if (requireFacing && Vector3.Dot(transform.forward, -hit.normal) < 0.3f) continue;

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

        // Don't teleport the player — lerp them onto the wall instead.
        // This avoids the Y conflict with the surface tracking snap and
        // prevents the camera jumping on entry.
        _grabFrom     = transform.position;
        _grabTo       = bestPoint + bestNormal * surfaceOffset;
        _grabProgress = 0f;   // start lerping immediately
        _armSide      = 1;

        _climbStartTime = Time.time;
        isClimbing      = true;
        cc.enabled      = false;

        SetTargetRotation(surfaceNormal);

        // Sync camera rotation — no snap on transition
        cameraRotationX = playerCamera.transform.localEulerAngles.x;
        if (cameraRotationX > 180f) cameraRotationX -= 360f;
        cameraRotationY = 0f;
    }

    void StopClimbing()
    {
        isClimbing           = false;
        cc.enabled           = true;
        _autoClimbCooldown   = AutoClimbCooldownDuration;

        // Use the camera's world yaw as the new body yaw so there's no snap
        float worldYaw = playerCamera.transform.eulerAngles.y;
        transform.rotation = Quaternion.Euler(0f, worldYaw, 0f);
        playerCamera.transform.localRotation = Quaternion.identity;
        cameraRotationX = 0f;
        cameraRotationY = 0f;

        playerMovement?.ResetVerticalVelocity();
    }

    // ── Auto-climb on steep surface contact ───────────────────────────────

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (isClimbing)                          return;
        if (!autoClimb)                          return;
        if (Stamina <= 0f)                       return;
        if (_autoClimbCooldown > 0f)             return;

        // Only surfaces on climbable layers
        if (((1 << hit.gameObject.layer) & climbableLayers) == 0) return;

        // Surface must be steep enough to count as a wall
        float angle = Vector3.Angle(hit.normal, Vector3.up);
        if (angle < autoClimbSlopeAngle) return;

        // Use input direction rather than cc.velocity — velocity is near zero
        // when the CC is blocked by the wall, which is exactly when we want to climb.
        Vector3 inputDir = transform.TransformDirection(
            new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical")));
        if (inputDir.magnitude < 0.2f) return;                                    // no input
        if (Vector3.Dot(inputDir.normalized, -hit.normal) < 0.3f) return;         // not aimed at wall

        // Don't auto-climb while falling fast
        if (cc.velocity.y < -4f) return;

        TryStartClimbing(requireFacing: false);
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

        // ── Background track ────────────────────────────────────────────────
        GameObject bgGO = new GameObject("StaminaBarBG");
        bgGO.transform.SetParent(canvasGO.transform, false);
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin        = new Vector2(0f, 0f);
        bgRect.anchorMax        = new Vector2(0f, 0f);
        bgRect.pivot            = new Vector2(0f, 0f);
        bgRect.anchoredPosition = new Vector2(20f, 20f);
        bgRect.sizeDelta        = new Vector2(BarMaxWidth, BarHeight);

        // ── Fill bar ────────────────────────────────────────────────────────
        GameObject fillGO = new GameObject("StaminaBarFill");
        fillGO.transform.SetParent(bgGO.transform, false);
        Image fillImg = fillGO.AddComponent<Image>();
        fillImg.color = Color.white;
        staminaBarFillRect             = fillGO.GetComponent<RectTransform>();
        staminaBarFillRect.anchorMin   = new Vector2(0f, 0f);
        staminaBarFillRect.anchorMax   = new Vector2(0f, 0f);
        staminaBarFillRect.pivot       = new Vector2(0f, 0f);
        staminaBarFillRect.anchoredPosition = Vector2.zero;
        staminaBarFillRect.sizeDelta   = new Vector2(BarMaxWidth, BarHeight);
    }
}
