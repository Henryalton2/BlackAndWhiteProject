using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public Camera playerCamera;
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float jumpPower = 7f;
    public float gravity = 10f;
    public float lookSpeed = 2f;
    public float lookXLimit = 45f;
    public float defaultHeight = 2f;
    public float crouchHeight = 1f;
    public float crouchSpeed = 3f;

    // Movement states (for audio / animation)
    public bool isWalking;
    public bool isRunningState;
    public bool isCrouching;
    static public bool dialogue = false;

    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;
    private CharacterController characterController;
    private bool canMove = true;

    // Store the original speeds so we can restore them
    private float originalWalkSpeed;
    private float originalRunSpeed;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Cache the original speeds from Inspector values
        originalWalkSpeed = walkSpeed;
        originalRunSpeed = runSpeed;
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

    void Update()
    {
        // Don't allow any input when paused
        if (PauseMenu.GameisPaused)
            return;

        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        bool isRunning = Input.GetKey(KeyCode.LeftShift);

        // Input check FIRST
        bool hasInput =
            Mathf.Abs(Input.GetAxis("Vertical")) > 0.1f ||
            Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f;

        // Crouch logic - changes speeds
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
            // Restore original speeds instead of hardcoded values
            walkSpeed = originalWalkSpeed;
            runSpeed = originalRunSpeed;
        }

        // Movement speeds (uses current walkSpeed/runSpeed which may be crouch or normal)
        float curSpeedX = canMove ? (isRunning ? runSpeed : walkSpeed) * Input.GetAxis("Vertical") : 0;
        float curSpeedY = canMove ? (isRunning ? runSpeed : walkSpeed) * Input.GetAxis("Horizontal") : 0;
        float movementDirectionY = moveDirection.y;
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        // Jump
        if (Input.GetButton("Jump") && canMove && characterController.isGrounded)
            moveDirection.y = jumpPower;
        else
            moveDirection.y = movementDirectionY;

        // Gravity
        if (!characterController.isGrounded)
            moveDirection.y -= gravity * Time.deltaTime;

        // Movement states (AFTER crouch logic)
        isRunningState =
            isRunning &&
            hasInput &&
            characterController.isGrounded &&
            !isCrouching;

        isWalking =
            characterController.isGrounded &&
            hasInput &&
            !isRunningState &&
            !isCrouching &&
            canMove;

        // Move the character
        characterController.Move(moveDirection * Time.deltaTime);

        // Camera look
        if (canMove)
        {
            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
        }
    }
}