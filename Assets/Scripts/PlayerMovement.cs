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

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        originalWalkSpeed = walkSpeed;
        originalRunSpeed = runSpeed;
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

            if (!onSteepSlope && Input.GetButtonDown("Jump") && canMove)
                movementY = jumpPower;
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

        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
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

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
