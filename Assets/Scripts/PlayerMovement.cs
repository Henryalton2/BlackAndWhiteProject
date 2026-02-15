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

    // 🔽 REQUIRED BY OTHER SYSTEMS (Audio, etc.)
    public bool isWalking;
    public bool isRunningState;
    public bool isCrouching;

    public static bool dialogue = false;

    private CharacterController characterController;
    private Vector3 moveDirection = Vector3.zero;
    private Vector3 externalVelocity = Vector3.zero;

    private float rotationX = 0f;
    private float originalWalkSpeed;
    private float originalRunSpeed;
    private bool canMove = true;

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

        // Crouch
        if (Input.GetKey(KeyCode.R) && canMove)
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

        if (characterController.isGrounded)
        {
            if (Input.GetButtonDown("Jump") && canMove)
                movementY = jumpPower;
        }

        movementY -= gravity * Time.deltaTime;
        moveDirection.y = movementY;

        // Apply external forces (cloud bounce)
        moveDirection += externalVelocity;
        externalVelocity = Vector3.Lerp(externalVelocity, Vector3.zero, Time.deltaTime * 10f);

        characterController.Move(moveDirection * Time.deltaTime);

        // 🔽 State flags (AudioManager relies on these)
        bool hasMoveInput = Mathf.Abs(Input.GetAxis("Vertical")) > 0.1f || Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f;

        isRunningState = characterController.isGrounded && isRunningInput && !isCrouching && hasMoveInput;
        isWalking = characterController.isGrounded && !isRunningState && !isCrouching && hasMoveInput;
    }

    private void HandleLook()
    {
        if (!canMove) return;

        rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);

        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
        transform.rotation *= Quaternion.Euler(0f, Input.GetAxis("Mouse X") * lookSpeed, 0f);
    }

    // 🔽 Used by CloudPlatformSpawner
    public void ApplyExternalVelocity(Vector3 velocity)
    {
        externalVelocity += velocity;
    }
    public void AddBoost(Vector3 boost)
    {
        moveDirection += boost;
    }
}
