using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Main character controller for player movement, camera, sprint, and crouch.
/// Works for both Hider and Seeker roles.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 1.5f;
    [SerializeField] private float crouchMultiplier = 0.5f;
    [SerializeField] private float gravity = -20f;

    [Header("Camera Settings")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float verticalLookLimit = 85f;

    [Header("Crouch Settings")]
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchTransitionSpeed = 10f;

    // Components
    private CharacterController characterController;

    // State
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float verticalVelocity;
    private float cameraPitch;
    private bool isSprinting;
    private bool isCrouching;
    private float targetHeight;

    // External modifiers (for abilities, tag slow, etc.)
    private float externalSpeedMultiplier = 1f;

    // Properties
    public bool IsSprinting => isSprinting;
    public bool IsCrouching => isCrouching;
    public bool IsGrounded => characterController.isGrounded;
    public float CurrentSpeed => characterController.velocity.magnitude;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (cameraTransform == null)
        {
            cameraTransform = GetComponentInChildren<Camera>()?.transform;
        }

        targetHeight = standingHeight;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleInput();
        HandleMovement();
        HandleLook();
        HandleCrouch();
    }

    private void HandleInput()
    {
        // Keyboard input
        moveInput = Vector2.zero;
        if (Input.GetKey(KeyCode.W)) moveInput.y += 1;
        if (Input.GetKey(KeyCode.S)) moveInput.y -= 1;
        if (Input.GetKey(KeyCode.A)) moveInput.x -= 1;
        if (Input.GetKey(KeyCode.D)) moveInput.x += 1;
        moveInput = moveInput.normalized;

        // Mouse input
        lookInput.x = Input.GetAxis("Mouse X");
        lookInput.y = Input.GetAxis("Mouse Y");

        // Sprint (hold shift)
        isSprinting = Input.GetKey(KeyCode.LeftShift) && !isCrouching;

        // Crouch toggle (C key)
        if (Input.GetKeyDown(KeyCode.C))
        {
            isCrouching = !isCrouching;
            targetHeight = isCrouching ? crouchHeight : standingHeight;
            if (isCrouching) isSprinting = false;
        }
    }

    private void HandleMovement()
    {
        // Calculate movement direction
        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;

        // Calculate speed with modifiers
        float currentSpeed = walkSpeed;
        if (isSprinting)
            currentSpeed *= sprintMultiplier;
        else if (isCrouching)
            currentSpeed *= crouchMultiplier;

        currentSpeed *= externalSpeedMultiplier;

        // Apply gravity
        if (characterController.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f;
        }
        verticalVelocity += gravity * Time.deltaTime;

        // Final movement
        Vector3 velocity = moveDirection * currentSpeed;
        velocity.y = verticalVelocity;

        characterController.Move(velocity * Time.deltaTime);
    }

    private void HandleLook()
    {
        if (cameraTransform == null) return;

        // Horizontal rotation - rotate the body
        float mouseX = lookInput.x * mouseSensitivity;
        transform.Rotate(Vector3.up * mouseX);

        // Vertical rotation - rotate only the camera
        float mouseY = lookInput.y * mouseSensitivity;
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -verticalLookLimit, verticalLookLimit);

        cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    private void HandleCrouch()
    {
        float currentHeight = characterController.height;
        if (Mathf.Abs(currentHeight - targetHeight) > 0.01f)
        {
            float newHeight = Mathf.Lerp(currentHeight, targetHeight, crouchTransitionSpeed * Time.deltaTime);
            float heightDifference = newHeight - currentHeight;

            characterController.height = newHeight;

            // Adjust center to keep feet on ground
            Vector3 center = characterController.center;
            center.y += heightDifference / 2f;
            characterController.center = center;

            // Adjust camera position
            if (cameraTransform != null)
            {
                Vector3 camPos = cameraTransform.localPosition;
                camPos.y = newHeight - 0.2f;
                cameraTransform.localPosition = camPos;
            }
        }
    }

    /// <summary>
    /// Apply external speed modifier (e.g., from tag slow effect)
    /// </summary>
    public void SetSpeedMultiplier(float multiplier)
    {
        externalSpeedMultiplier = Mathf.Clamp(multiplier, 0.1f, 2f);
    }

    /// <summary>
    /// Reset speed to normal
    /// </summary>
    public void ResetSpeedMultiplier()
    {
        externalSpeedMultiplier = 1f;
    }
}
