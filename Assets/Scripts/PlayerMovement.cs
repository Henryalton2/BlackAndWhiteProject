using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float jumpPower = 7f;
    public float gravity = 10f;
    public float crouchHeight = 1f;
    public float defaultHeight = 2f;
    public float crouchSpeed = 3f;

    [Header("Look")]
    public Camera playerCamera;
    public float lookSpeed = 2f;
    public float lookXLimit = 45f;

    [Header("Ground Detection")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundMask;

    [Header("Slope")]
    [SerializeField] private float maxJumpSlopeAngle = 45f;
    [SerializeField] private float slideForce = 12f;
    [Range(1f, 20f)]
    [SerializeField] private float slideSmoothSpeed = 8f;

    [Header("Bhop")]
    public bool bhopEnabled = false;
    public float bhopSpeedLimit = 15f;
    [Range(0.5f, 5f)]
    public float bhopAcceleration = 1f;

    [Header("Speed FOV")]
    public float baseFOV = 60f;
    public float maxFOV = 75f;
    [Range(1f, 10f)]
    public float fovLerpSpeed = 5f;

    [Header("Speed Camera Shake")]
    public float shakeIntensity = 0.04f;
    [Range(1f, 30f)]
    public float shakeFrequency = 12f;

    [Header("Head Bob")]
    public float bobFrequency = 2.4f;
    public float bobAmplitudeY = 0.05f;

    [Header("Landing Squish")]
    public float squishAmount = 0.12f;
    [Range(1f, 20f)]
    public float squishRecovery = 10f;

    [Header("Strafe Tilt")]
    public float maxStrafeTilt = 4f;
    [Range(1f, 15f)]
    public float strafeTiltSpeed = 8f;

    [Header("Jump Sway")]
    public float jumpSwayAmount = 0.08f;
    [Range(1f, 15f)]
    public float jumpSwayDecay = 7f;

    [Header("Crouch Camera")]
    public float crouchCameraOffset = 0.5f;
    [Range(1f, 20f)]
    public float crouchLerpSpeed = 12f;

    public bool isWalking;
    public bool isRunningState;
    public bool isCrouching;

    public static bool dialogue = false;

    public bool IsGrounded => characterController.isGrounded;
    public bool IsOnGround { get; private set; }

    private CharacterController characterController;
    private Vector3 moveDirection = Vector3.zero;
    private Vector3 externalVelocity = Vector3.zero;
    private Vector3 lastContactNormal  = Vector3.up;
    private Vector3 smoothedNormal     = Vector3.up;
    private Vector3 slideVelocity      = Vector3.zero;

    private float rotationX = 0f;
    private float originalWalkSpeed;
    private float originalRunSpeed;
    private bool canMove = true;
    private bool wasClimbing = false;
    private float _bhopBonus = 0f;
    private float _shakeTime = 0f;
    private Vector3 _cameraBaseLocalPos;
    private Vector3 _shakeOffset;

    // Camera effect state
    private float _bobTimer        = 0f;
    private float _bobY            = 0f;
    private float _squishOffset    = 0f;
    private float _strafeTilt      = 0f;
    private float _jumpSwayOffset  = 0f;
    private float _crouchOffset    = 0f;
    private bool  _wasGrounded     = false;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        originalWalkSpeed = walkSpeed;
        originalRunSpeed = runSpeed;
        _cameraBaseLocalPos = playerCamera.transform.localPosition;
    }

    void Update()
    {
        if (PauseMenu.GameisPaused) return;

        if (ClimbingController.isClimbing) { wasClimbing = true; return; }

        // Sync camera rotation the first frame after climbing ends so it doesn't snap
        if (wasClimbing)
        {
            wasClimbing = false;
            rotationX = playerCamera.transform.localEulerAngles.x;
            if (rotationX > 180f) rotationX -= 360f;
        }

        IsOnGround = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundMask);

        HandleMovement();
        HandleLook();
        HandleFOV();

        bool grounded = characterController.isGrounded;
        UpdateHeadBob(grounded);
        UpdateLandingSquish(grounded);
        UpdateJumpSway(grounded);
        UpdateStrafeTilt();
        UpdateCrouchOffset();
        HandleCameraShake();
        ApplyCameraEffects();
        _wasGrounded = grounded;
    }

    private void FixedUpdate()
    {
        if (dialogue)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void HandleMovement()
    {
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        bool isRunningInput = Input.GetKey(KeyCode.LeftShift);

        if (Input.GetKey(KeyCode.LeftControl) && canMove)
        {
            isCrouching = true;
            characterController.height = crouchHeight;
            walkSpeed = crouchSpeed;
            runSpeed = crouchSpeed;
        }
        else
        {
            isCrouching = false;
            characterController.height = defaultHeight;
            walkSpeed = originalWalkSpeed;
            runSpeed = originalRunSpeed;
        }

        float curSpeedX = canMove ? (isRunningInput ? runSpeed : walkSpeed) * Input.GetAxis("Vertical") : 0f;
        float curSpeedZ = canMove ? (isRunningInput ? runSpeed : walkSpeed) * Input.GetAxis("Horizontal") : 0f;

        float movementY = moveDirection.y;

        moveDirection = (forward * curSpeedX) + (right * curSpeedZ);

        // Lerp toward the raw contact normal so the slide direction can't snap
        // abruptly when OnControllerColliderHit fires from different geometry faces.
        smoothedNormal = Vector3.Lerp(smoothedNormal, lastContactNormal, Time.deltaTime * slideSmoothSpeed);

        float slopeAngle = Vector3.Angle(smoothedNormal, Vector3.up);
        bool onSteepSlope = characterController.isGrounded && slopeAngle > maxJumpSlopeAngle;

        if (characterController.isGrounded)
        {
            // Don't reset Y while sliding — the slide vector handles vertical movement.
            if (movementY < 0 && !onSteepSlope)
                movementY = -2f;

            // Bhop: hold space to auto-jump; single press otherwise
            bool wantsJump = bhopEnabled ? Input.GetButton("Jump") : Input.GetButtonDown("Jump");

            if (!onSteepSlope && wantsJump && canMove)
            {
                movementY = jumpPower;
                if (bhopEnabled)
                    _bhopBonus = Mathf.Min(_bhopBonus + bhopAcceleration, bhopSpeedLimit);
            }
            else if (bhopEnabled && !Input.GetButton("Jump"))
            {
                _bhopBonus = 0f;   // landed without jumping — chain broken
            }
        }

        movementY -= gravity * Time.deltaTime;
        moveDirection.y = movementY;

        if (onSteepSlope)
        {
            Vector3 slideDir    = Vector3.ProjectOnPlane(Vector3.down, smoothedNormal).normalized;
            Vector3 slideTarget = slideDir * slideForce;
            slideVelocity = Vector3.MoveTowards(slideVelocity, slideTarget, slideForce * slideSmoothSpeed * Time.deltaTime);

            // Apply the full slide vector including Y so the player moves *along*
            // the slope surface rather than being pushed into it by separate gravity.
            moveDirection.x += slideVelocity.x;
            moveDirection.z += slideVelocity.z;
            moveDirection.y  = slideVelocity.y;
        }
        else
        {
            slideVelocity = Vector3.Lerp(slideVelocity, Vector3.zero, Time.deltaTime * slideSmoothSpeed);
            moveDirection.x += slideVelocity.x;
            moveDirection.z += slideVelocity.z;
        }

        // Bhop speed boost — applied to XZ only
        if (bhopEnabled && _bhopBonus > 0f)
        {
            Vector3 flat = new Vector3(moveDirection.x, 0f, moveDirection.z);
            if (flat.magnitude > 0.1f)
            {
                Vector3 boosted = flat + flat.normalized * _bhopBonus;
                if (boosted.magnitude > bhopSpeedLimit)
                    boosted = boosted.normalized * bhopSpeedLimit;
                moveDirection.x = boosted.x;
                moveDirection.z = boosted.z;
            }
        }

        // Hard cap: when bhop is on, total XZ speed never exceeds the limit —
        // this catches sprint speed bleeding in even without a bonus active.
        if (bhopEnabled)
        {
            Vector3 flat = new Vector3(moveDirection.x, 0f, moveDirection.z);
            if (flat.magnitude > bhopSpeedLimit)
            {
                flat = flat.normalized * bhopSpeedLimit;
                moveDirection.x = flat.x;
                moveDirection.z = flat.z;
            }
        }

        moveDirection += externalVelocity;
        externalVelocity = Vector3.Lerp(externalVelocity, Vector3.zero, Time.deltaTime * 10f);

        characterController.Move(moveDirection * Time.deltaTime);

        bool hasMoveInput = Mathf.Abs(Input.GetAxis("Vertical")) > 0.1f || Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f;

        isRunningState = characterController.isGrounded && isRunningInput && !isCrouching && hasMoveInput;
        isWalking = characterController.isGrounded && !isRunningState && !isCrouching && hasMoveInput;
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.normal.y > 0.1f)
            lastContactNormal = hit.normal;
    }

    private void HandleLook()
    {
        if (!canMove) return;

        rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
        // Camera rotation is applied in ApplyCameraEffects so effects can be composed cleanly.
        transform.rotation *= Quaternion.Euler(0f, Input.GetAxis("Mouse X") * lookSpeed, 0f);
    }

    public void ApplyExternalVelocity(Vector3 velocity)
    {
        externalVelocity += velocity;
    }

    public void AddBoost(Vector3 boost)
    {
        moveDirection += boost;
    }

    // Called by ClimbingController when the player releases the wall,
    // so they don't plummet at whatever vertical speed they had before grabbing.
    public void ResetVerticalVelocity()
    {
        moveDirection.y = 0f;
    }

    // Called by ClimbingController for wall jumps — sets a clean upward velocity
    // the same way a normal jump does, so gravity takes over naturally from there.
    public void SetVerticalVelocity(float y)
    {
        moveDirection.y = y;
    }

    private void HandleCameraShake()
    {
        float hSpeed = new Vector3(characterController.velocity.x, 0f, characterController.velocity.z).magnitude;
        float maxSpeed = bhopEnabled ? bhopSpeedLimit : runSpeed;
        float t = Mathf.Clamp01(hSpeed / maxSpeed);

        _shakeTime += Time.deltaTime * shakeFrequency;

        // Perlin noise gives smooth, organic motion — offset by large values on each
        // axis so X and Y noise don't look identical.
        float intensity = shakeIntensity * t;
        float x = (Mathf.PerlinNoise(_shakeTime,        0f) - 0.5f) * 2f * intensity;
        float y = (Mathf.PerlinNoise(0f, _shakeTime + 100f) - 0.5f) * 2f * intensity;

        _shakeOffset = new Vector3(x, y, 0f);
    }

    private void HandleFOV()
    {
        float hSpeed = new Vector3(characterController.velocity.x, 0f, characterController.velocity.z).magnitude;
        float maxSpeed = bhopEnabled ? bhopSpeedLimit : runSpeed;
        float t = Mathf.Clamp01(hSpeed / maxSpeed);
        float targetFOV = Mathf.Lerp(baseFOV, maxFOV, t);
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * fovLerpSpeed);
    }

    // ── Head bob ──────────────────────────────────────────────────────────────
    private void UpdateHeadBob(bool grounded)
    {
        bool moving = grounded && (isWalking || isRunningState);
        if (moving)
        {
            float freq = isRunningState ? bobFrequency * 1.6f : bobFrequency;
            _bobTimer += Time.deltaTime * freq;
            float amp = bobAmplitudeY * (isRunningState ? 1.4f : 1f);
            _bobY = Mathf.Sin(_bobTimer * Mathf.PI * 2f) * amp;
        }
        else
        {
            _bobY = Mathf.Lerp(_bobY, 0f, Time.deltaTime * 10f);
        }
    }

    // ── Landing squish ────────────────────────────────────────────────────────
    private void UpdateLandingSquish(bool grounded)
    {
        if (!_wasGrounded && grounded)
            _squishOffset = -squishAmount;      // snap down on impact

        _squishOffset = Mathf.Lerp(_squishOffset, 0f, Time.deltaTime * squishRecovery);
    }

    // ── Jump sway ─────────────────────────────────────────────────────────────
    private void UpdateJumpSway(bool grounded)
    {
        // Detect the moment of leaving the ground with upward velocity (a jump, not a walk-off)
        if (!grounded && _wasGrounded && characterController.velocity.y > 1f)
            _jumpSwayOffset = jumpSwayAmount;

        _jumpSwayOffset = Mathf.Lerp(_jumpSwayOffset, 0f, Time.deltaTime * jumpSwayDecay);
    }

    // ── Strafe tilt ───────────────────────────────────────────────────────────
    private void UpdateStrafeTilt()
    {
        float targetTilt = -Input.GetAxis("Horizontal") * maxStrafeTilt;
        _strafeTilt = Mathf.Lerp(_strafeTilt, targetTilt, Time.deltaTime * strafeTiltSpeed);
    }

    // ── Crouch camera lerp ────────────────────────────────────────────────────
    private void UpdateCrouchOffset()
    {
        float target = isCrouching ? -crouchCameraOffset : 0f;
        _crouchOffset = Mathf.Lerp(_crouchOffset, target, Time.deltaTime * crouchLerpSpeed);
    }

    // ── Compose and apply all camera effects ──────────────────────────────────
    private void ApplyCameraEffects()
    {
        // Position: base + every vertical offset + shake
        float totalY = _bobY + _squishOffset + _jumpSwayOffset + _crouchOffset + _shakeOffset.y;
        playerCamera.transform.localPosition = _cameraBaseLocalPos + new Vector3(_shakeOffset.x, totalY, 0f);

        // Rotation: look tilt (up/down) composed with strafe roll
        Quaternion lookRot  = Quaternion.Euler(rotationX, 0f, 0f);
        Quaternion tiltRot  = Quaternion.Euler(0f, 0f, _strafeTilt);
        playerCamera.transform.localRotation = lookRot * tiltRot;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
