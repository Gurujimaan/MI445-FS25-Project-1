using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// This class handles the control of a third person character controller.
/// 
/// Motion is based on the direction the player(user) expects to move 
/// i.e. left and right are based on the camera view and not the direction the player's character is facing
/// </summary>
public class ThirdPersonCharacterController : MonoBehaviour
{
    // the character controller used for player motion
    private CharacterController characterController;
    // the health used by the player
    private Health playerHealth;

    // Enum to handle the player's state
    public enum PlayerState { Idle, Moving, Jumping, DoubleJumping, Falling, Dead };

    [Header("State Information")]
    [Tooltip("The state the player controller is currently in")]
    public PlayerState playerState = PlayerState.Idle;

    [Header("Input Actions & Controls")]
    [Tooltip("The input action(s) that map to player movement")]
    public InputAction moveInput;
    [Tooltip("The input action(s) that map to jumping")]
    public InputAction jumpInput;
    
    public GameObject thruster;

    [Header("Effects settings")]
    [Tooltip("The effect to create when jumping")]
    public GameObject jumpEffect;
    [Tooltip("The effect to create when double jumping")]
    public GameObject doubleJumpEffect;
    [Tooltip("The effec to create when the player lands on the ground")]
    public GameObject landingEffect;

    [Header("Following Game Objects")]
    [Tooltip("The following scripts of game objects that should follow this controller after it is done moving each frame")]
    public List<FollowLikeChild> followers;
    
    
    [Header("Jetpack Settings")]
    [Tooltip("Maximum jetpack fuel")]
    public float maxFuel = 5f;
    [Tooltip("Current jetpack fuel")]
    public float currentFuel;
    [Tooltip("How fast the jetpack fuel regenerates per second")]
    public float fuelRegenRate = 1f;
    [Tooltip("How fast the jetpack fuel burns per second while active")]
    public float fuelBurnRate = 1f;
    [Tooltip("Upward force applied when jetpack is active")]
    public float jetpackThrust = 5f;

    [Header("Dash Settings")]
    public float dashForce = 15f;
    public float dashCooldown = 3f;
    public float dashDuration = 0.3f;
    private bool canDash = true;
    private float dashCooldownTimer = 0f;
    private float dashTimer = 0f;
    private Vector3 dashMomentum = Vector3.zero;

    public GameObject shield;


    /// <summary>
    /// Standard Unity function called whenever the attached gameobject is enabled
    /// </summary>
    private void OnEnable()
    {
        moveInput.Enable();
        jumpInput.Enable();
    }

    /// <summary>
    /// Standard Unity function called whenever the attached gameobject is disabled
    /// </summary>
    private void OnDisable()
    {
        moveInput.Disable();
        jumpInput.Disable();
    }

    /// <summary>
    /// Description:
    /// Standard Unity function that is called before the first frame
    /// Input:
    /// none
    /// Return:
    /// void
    /// </summary>
    void Start()
    {
        InitialSetup();
        currentFuel = maxFuel;
        shield.SetActive(false);
    }

    /// <summary>
    /// Description:
    /// Checks for and gets the needed refrences for this script to run correctly
    /// Input:
    /// none
    /// Return:
    /// void
    /// </summary>
    void InitialSetup()
    {
        characterController = gameObject.GetComponent<CharacterController>();
        if (characterController == null)
        {
         
        }

        if (GetComponent<Health>() == null)
        {
            
        }
        else
        {
            playerHealth = GetComponent<Health>();
        }

        if (GetComponent<Rigidbody>())
        {
            GetComponent<Rigidbody>().useGravity = false;
        }
    }

    /// <summary>
    /// Description:
    /// Standard Unity function called once per frame
    /// Inputs:
    /// none
    /// Retuns:
    /// void
    /// </summary>
    void Update()
    {
        // Handle dash input in Update
        if ((Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)) && canDash && characterController.isGrounded)
        {
        
            dashMomentum = transform.forward * dashForce;
            dashTimer = dashDuration;
            canDash = false;
            dashCooldownTimer = dashCooldown;
        }
    }

    void LateUpdate()
    {
        // Don't do anything if the game is paused
        if (Time.timeScale == 0)
        {
            return;
        }
        MatchCameraYRotation();
        CentralizedControl(moveInput.ReadValue<Vector2>().x, moveInput.ReadValue<Vector2>().y, jumpInput.triggered, jumpInput.IsPressed());
        HandleRightClick();
    }
    
    void HandleRightClick()
    {
        if (Input.GetMouseButton(1) && characterController.isGrounded) 
        {
            shield.SetActive(true);
        }
        else
        {
            shield.SetActive(false);
        }
    }
    
    [Header("Related Gameobjects / Scripts needed for determining control states")]
    [Tooltip("The player camera gameobject, used to manage the controller's rotations")]
    public GameObject playerCamera;
    [Tooltip("The player character's representation (model)")]
    public PlayerRepresentation playerRepresentation;

    /// <summary>
    /// Description:
    /// Makes the character controller's rotation along the Y match the camera's
    /// Input:
    /// none
    /// Return:
    /// void
    /// </summary>
    void MatchCameraYRotation()
    {
        if (playerCamera != null)
        {
            this.gameObject.transform.rotation = Quaternion.Euler(0, playerCamera.transform.rotation.eulerAngles.y, 0);
        }
    }

    [Header("Speed Control")]
    [Tooltip("The speed at which to move the player")]
    public float moveSpeed = 5f;
    [Tooltip("The strength with which to jump")]
    public float jumpStrength = 8.0f;
    [Tooltip("The strength of gravity on this controller")]
    public float gravity = 20.0f;

    [Tooltip("The amount of downward movement to register as falling")]
    public float fallAmount = -9.0f;

    // The direction the player is moving in
    private Vector3 moveDirection = Vector3.zero;

    // Whether or not the double jump is currently available
    bool doubleJumpAvailable = false;

    [Header("Jump Timing")]
    [Tooltip("How long to be lenient for when the player becomes ungrounded")]
    public float jumpTimeLeniency = 0.1f;
    // When to stop being lenient
    float timeToStopBeingLenient = 0;

    bool landed = false;

    // Horizontal force to apply on bounce
    float xForce = 0f;
    float zForce = 0f;

    /// <summary>
    /// Description:
    /// Handles swithing between control styles if more than one is coded in
    /// Inputs:
    /// float leftCharacterMovement | float rightCharacterMovement | bool jumpPressed
    /// Returns:
    /// void
    /// </summary>
    void CentralizedControl(float leftRightMovementAxis, float forwardBackwardMovementAxis, bool jumpPressed, bool jumpHeld)
    {
        if (playerHealth.currentHealth <= 0)
        {
            DeadControl();
        }
        else
        {
            NormalControl(leftRightMovementAxis, forwardBackwardMovementAxis, jumpPressed, jumpHeld);
        }
    }

    /// <summary>
    /// Description:
    /// Handles motion of the player representation under the average or normal use case
    /// Inputs:
    /// float leftCharacterMovement | float rightCharacterMovement | bool jumpPressed
    /// Returns:
    /// void
    /// </summary>
    void NormalControl(float leftRightMovementAxis, float forwardBackwardMovementAxis, bool jumpPressed, bool jumpHeld)
    {
        // The input corresponding to the left and right movement
        float leftRightInput = leftRightMovementAxis;
        // The input corresponding to the forward and backward movement
        float forwardBackwardInput = forwardBackwardMovementAxis;

        // If the controller is grounded
        if (characterController.isGrounded && !bounced)
        {
            timeToStopBeingLenient = Time.time + jumpTimeLeniency;

            xForce = 0f;
            zForce = 0f;

            if (!landed && landingEffect != null && moveDirection.y <= fallAmount)
            {
                landed = true;
                Instantiate(landingEffect, transform.position, transform.rotation, null);
            }

            // Handle jump input when grounded
            if (jumpPressed && shield.activeSelf==false)
            {
                moveDirection.y = jumpStrength;
                if (jumpEffect != null)
                {
                    Instantiate(jumpEffect, transform.position, transform.rotation, null);
                }
                playerState = PlayerState.Jumping;
            }

            // movement in XZ plane
            moveDirection = new Vector3(leftRightInput, 0, forwardBackwardInput);
            moveDirection = transform.TransformDirection(moveDirection);
            moveDirection *= moveSpeed;

            // basic grounded state control
            if (moveDirection == Vector3.zero)
                playerState = PlayerState.Idle;
            else
                playerState = PlayerState.Moving;
        }
        else
        {
            // Apply move direction while airborne
            moveDirection = new Vector3(leftRightInput * moveSpeed + xForce, moveDirection.y, forwardBackwardInput * moveSpeed + zForce);
            moveDirection = transform.TransformDirection(moveDirection);

            if (moveDirection.y < -5.0f)
            {
                bounced = false;
                landed = false;
                playerState = PlayerState.Falling;
            }
        }

        // Handle jetpack
        if (jumpHeld && currentFuel > 0f && shield.activeSelf==false)
        {
            moveDirection.y = jetpackThrust;
            currentFuel -= fuelBurnRate * Time.deltaTime;
            playerState = PlayerState.Jumping; 
            thruster.SetActive(true);
        }
        else if (!jumpHeld && characterController.isGrounded)
        {
            // regenerate fuel when not using jetpack
            currentFuel = Mathf.Min(maxFuel, currentFuel + fuelRegenRate * Time.deltaTime);
        }

        if (!jumpHeld || characterController.isGrounded)
        {
            thruster.SetActive(false);
        }

        // Apply dash momentum if active
        if (dashTimer > 0)
        {
            moveDirection += dashMomentum;
            dashTimer -= Time.deltaTime;
            
            // Gradually reduce dash power
            dashMomentum = Vector3.Lerp(dashMomentum, Vector3.zero, Time.deltaTime * 5f);
        }
        else
        {
            dashMomentum = Vector3.zero; // reset when dash ends
        }

        // Countdown cooldown
        if (!canDash)
        {
            dashCooldownTimer -= Time.deltaTime;
            if (dashCooldownTimer <= 0f)
            {
                canDash = true;
                dashCooldownTimer = 0f;
            }
        }

        // Apply gravity
        moveDirection.y -= gravity * Time.deltaTime;

        // prevent excessive downward buildup
        if (characterController.isGrounded && moveDirection.y < 0)
        {
            moveDirection.y = -0.3f;
        }

        // Apply movement
        characterController.Move(moveDirection * Time.deltaTime);

        // followers
        foreach (FollowLikeChild follower in followers)
        {
            follower.FollowParent();
        }
    }

    bool bounced = false;
    /// <summary>
    /// Description:
    /// Bounces the player upwards with some multiplier by the jump strength
    /// Input:
    /// float bounceForceMultiplier | float bounceJumpButtonHeldMultiplyer
    /// Output:
    /// void
    /// </summary>
    public void Bounce(float bounceForceMultiplier, float bounceJumpButtonHeldMultiplyer, bool applyHorizontalForce)
    {
        bounced = true;
        playerState = PlayerState.Jumping;
        if (jumpInput.ReadValue<float>() != 0)
        {
            moveDirection.y = jumpStrength * bounceJumpButtonHeldMultiplyer;
        }
        else
        {
            moveDirection.y = jumpStrength * bounceForceMultiplier;
        }

        if (applyHorizontalForce)
        {
            float horizontalForce = moveDirection.y * 0.5f;
            xForce = Random.Range(-horizontalForce, horizontalForce);
            zForce = Random.Range(-horizontalForce, horizontalForce);
        }
    }

    /// <summary>
    /// Description:
    /// Control when the player is dead
    /// Input:
    /// none
    /// Return:
    /// void
    /// </summary>
    void DeadControl()
    {
        playerState = PlayerState.Dead;
        moveDirection = new Vector3(0, moveDirection.y, 0);
        moveDirection = transform.TransformDirection(moveDirection);

        moveDirection.y -= gravity * Time.deltaTime;

        characterController.Move(moveDirection * Time.deltaTime);

        // Make all assigned followers do their following of the player now
        foreach (FollowLikeChild follower in followers)
        {
            follower.FollowParent();
        }
    }

    /// <summary>
    /// Description:
    /// Resets the double jump of the player
    /// Input:
    /// None
    /// Return:
    /// void (no return)
    /// </summary>
    public void ResetJumps()
    {
        doubleJumpAvailable = true;
    }

    /// <summary>
    /// Description:
    /// Moved the player to a specified position
    /// Input:
    /// Vector3 of the newPosition to move to
    /// Return:
    /// void (no return)
    /// </summary>
    public void MoveToPosition(Vector3 newPosition)
    {
        // must turn off the Character Controller prior to moving, as when on, the Character Controller controls all movement
        characterController.enabled = false;

        // reposition the player
        transform.position = newPosition;

        // turn character controller back on
        characterController.enabled = true;
    }
}