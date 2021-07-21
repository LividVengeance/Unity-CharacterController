using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class MovementController : MonoBehaviour
{
    private PlayerInput playerInput;
    private CharacterController characterController;
    private Animator animator;
    
    [Header("Gameplay")] 
    [SerializeField] private float maxHealth = 100f;
    [SerializeField, Range(0, 100)] private float currentHealth;

    private Vector2 currentMovementInput;
    private Vector3 currentMovement;
    private Vector3 currentRunMovement;
    [SerializeField] private float runSpeed = 3f;
    [SerializeField] private float jumpResetTime = 0.5f;
    private Coroutine jumpResetRoutine;
    private bool isMovementPressed;
    private bool isJumpPressed = false;
    private bool isRunPressed;
    
    private bool isAimingDownSights;
    private bool isCrouching;
    private float rotationFactorPerFrame = 15f;

    private bool isJumpAnimating = false;
    
    // Jumping
    private bool isJumping;
    private float initialJumpVelocity;
    private int jumpCount = 0;
    private Dictionary<int, float> initaljumpVelocitys = new Dictionary<int, float>();
    private Dictionary<int, float> jumpGravities = new Dictionary<int, float>();
    [SerializeField] private float maxJumpHeight = 1f;
    [SerializeField] private float maxJumpTime = 0.5f;
    [SerializeField] private float maxFallVelocity = 20f;
    
    // Gravity
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float groundedGravity = -0.05f;
    
    [Header("Animation")]
    [SerializeField] private float acceleration = 2.0f;
    [SerializeField] private float deceleration = 0.1f;
    
    [SerializeField] private float maxWalkVelocity = 0.5f;
    [SerializeField] private float maxRunVelocity = 2.0f;
    
    // Aim Down Sights
    [SerializeField] private float adsSpeed = 1f;
    private float currentAdsTime = 0f;
    
    // Crouching
    [SerializeField] private float crouchSpeed = 1f;
    private float currentCrouchTime;
    
    // Animation Hashing
    private int velocityXHash;
    private int velocityZHash;
    private int isJumpingHash;
    private int jumpCountHash;

    private float velocityX = 0f;
    private float velocityZ = 0f;
    
    private void Awake()
    {
        playerInput = new PlayerInput();
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        // Start player with full health
        currentHealth = maxHealth;
        
        // Animation hashing
        velocityXHash = Animator.StringToHash("Velocity X");
        velocityZHash = Animator.StringToHash("Velocity Z");
        isJumpingHash = Animator.StringToHash(("isJumping"));
        jumpCountHash = Animator.StringToHash("jumpCount");

        // Gets and stores the inputs from the New Unity input system
        playerInput.CharacterControls.Move.started += OnMovementInput;
        playerInput.CharacterControls.Move.canceled += OnMovementInput;
        playerInput.CharacterControls.Move.performed += OnMovementInput;
        // Player sprint
        playerInput.CharacterControls.Run.started += OnRun;
        playerInput.CharacterControls.Run.canceled += OnRun;
        // Player aiming down sights
        playerInput.CharacterControls.AimDownSights.started += OnADS;
        playerInput.CharacterControls.AimDownSights.canceled += OnADS;
        // Player crouch
        playerInput.CharacterControls.Crouch.started += OnCrouch;
        // Jump
        playerInput.CharacterControls.Jump.started += OnJump;
        playerInput.CharacterControls.Jump.canceled += OnJump;

        JumpSetup();
    }

    private void JumpSetup()
    {
        // First Jump
        float timeToApex = maxJumpTime / 2;
        gravity = (-2 * maxJumpHeight) / Mathf.Pow(timeToApex, 2);
        initialJumpVelocity = (2 * maxJumpHeight) / timeToApex;
        
        // Second Jump
        float secondJumpGravity = (-2 * maxJumpHeight + 2) / Mathf.Pow((timeToApex * 1.25f), 2f);
        float secondJumpInitialVelocity = (2 * (maxJumpHeight + 2)) / (timeToApex * 1.25f);
        
        // Third jump
        float thirdJumpGravity = (-2 * maxJumpHeight + 4) / Mathf.Pow((timeToApex * 1.5f), 2f);
        float thirdJumpInitialVelocity = (2 * (maxJumpHeight + 4)) / (timeToApex * 1.5f);
        
        initaljumpVelocitys.Add(1, initialJumpVelocity);
        initaljumpVelocitys.Add(2, secondJumpInitialVelocity);
        initaljumpVelocitys.Add(3, thirdJumpInitialVelocity);
        
        jumpGravities.Add(0, gravity);
        jumpGravities.Add(1, gravity);
        jumpGravities.Add(2, secondJumpGravity);
        jumpGravities.Add(3, thirdJumpGravity);
    }

    private void OnMovementInput(InputAction.CallbackContext context)
    {
        currentMovementInput = context.ReadValue<Vector2>();
        //print(currentMovementInput);
        currentMovement.x = currentMovementInput.x;
        currentMovement.z = currentMovementInput.y;

        currentRunMovement.x = currentMovementInput.x * runSpeed;
        currentRunMovement.z = currentMovementInput.y * runSpeed;
        
        isMovementPressed = currentMovementInput.x != 0 || currentMovementInput.y != 0; 
    }
    
    private void OnRun(InputAction.CallbackContext context) => isRunPressed = context.ReadValueAsButton();
    private void OnADS(InputAction.CallbackContext context) => isAimingDownSights = context.ReadValueAsButton();
    private void OnCrouch(InputAction.CallbackContext context) => isCrouching = !isCrouching;
    private void OnJump(InputAction.CallbackContext context) => isJumpPressed = context.ReadValueAsButton();

    // Update is called once per frame
    void Update()
    {
        //AnimationHandler();
        RotationHandler();
        float currentMaxVelocity = isRunPressed ? maxRunVelocity : maxWalkVelocity;
        VelocityHandler(currentMaxVelocity);
        LockOrResetVelocity(currentMaxVelocity);

        AimDownSights();
        Crouch();

        // Adjusts animation for health
        animator.SetLayerWeight(animator.GetLayerIndex("Injured"), 1 - (currentHealth/maxHealth));
        
        // Changes movement speed when running
        if (isRunPressed) characterController.Move(currentRunMovement * Time.deltaTime);
        else characterController.Move(currentMovement * Time.deltaTime);    
        
        // Pass velocities to animation controller
        animator.SetFloat(velocityZHash, velocityZ);
        animator.SetFloat(velocityXHash, velocityX);
        
        GravityHandler();
        Jump();
    }

    private void AimDownSights()
    {
        // Is aiming down sights
        if (isAimingDownSights && !isJumping)
        {
            if (currentAdsTime < 1f) currentAdsTime += (Time.deltaTime * adsSpeed);
            else if (currentAdsTime > 1f) currentAdsTime = 1f; 
            
            animator.SetLayerWeight(animator.GetLayerIndex("Aiming"), currentAdsTime);
        }
        // Is NOT aiming down sights
        else
        {
            if (currentAdsTime > 0f) currentAdsTime -= (Time.deltaTime * adsSpeed);
            else if (currentAdsTime < 0f) currentAdsTime = 0f;
            
            animator.SetLayerWeight(animator.GetLayerIndex("Aiming"), currentAdsTime);
        }
    }

    private void Jump()
    {
        if (!isJumping && characterController.isGrounded && isJumpPressed)
        {
            if (jumpCount < 3 && jumpResetRoutine != null) StopCoroutine(jumpResetRoutine);
            jumpCount += 1;
            animator.SetBool(isJumpingHash, true);
            animator.SetInteger(jumpCountHash, jumpCount);
            isJumpAnimating = true;
            isJumping = true;
            currentMovement.y = initaljumpVelocitys[jumpCount] * 0.5f;
            currentRunMovement.y = initaljumpVelocitys[jumpCount] * 0.5f;
        }
        else if (!isJumpPressed && characterController.isGrounded && isJumping)
        {
            isJumping = false;
        }
    }

    private IEnumerator JumpReset()
    {
        yield return (new WaitForSeconds(jumpResetTime));
        jumpCount = 0;
    }

    private void Crouch()
    {
        // Is crouching
        if (isCrouching)
        {
            if (currentCrouchTime < 1f) currentCrouchTime += (Time.deltaTime * crouchSpeed);
            else if (currentCrouchTime > 1f) currentCrouchTime = 1f;
            
            animator.SetLayerWeight(animator.GetLayerIndex("Crouching"), currentCrouchTime);
        }
        // Is NOT crouching
        else
        {
            if (currentCrouchTime > 0f) currentCrouchTime -= (Time.deltaTime * crouchSpeed);
            else if (currentCrouchTime > 1f) currentCrouchTime = 0f;
            
            animator.SetLayerWeight(animator.GetLayerIndex("Crouching"), currentCrouchTime);
        }
    }

    private void VelocityHandler(float currentMaxVelocity)
    {
        // If press forward
        if (currentMovement.z > 0f && velocityZ < currentMaxVelocity) velocityZ += Time.deltaTime * acceleration;
        // If NOT press forward
        if (currentMovement.z <= 0f && velocityZ > 0f) velocityZ -= Time.deltaTime * deceleration;
        
        // If press back
        if (currentMovement.z < 0f && velocityZ > -currentMaxVelocity) velocityZ -= Time.deltaTime * acceleration;
        // If NOT press back
        if (currentMovement.z >= 0f && velocityZ < 0f) velocityZ += Time.deltaTime * deceleration;
        
        // If press right
        if (currentMovement.x > 0f && velocityX < currentMaxVelocity) velocityX += Time.deltaTime * acceleration;
        // If NOT press right
        if (currentMovement.x <= 0f && velocityX > 0f) velocityX -= Time.deltaTime * deceleration;
        
        // If press left
        if (currentMovement.x < 0f && velocityX > -currentMaxVelocity) velocityX -= Time.deltaTime * acceleration;
        // If NOT press left
        if (currentMovement.x >= 0f && velocityX < 0f) velocityX += Time.deltaTime * deceleration;
    }
    
    private void LockOrResetVelocity(float currentMaxVelocity)
    {
        float snap = 0.1f;
        // Rest velocity Z
        if (currentMovement.z <= 0 && velocityZ < 0f) velocityZ = 0f;
        // Reset velocity X
        if (currentMovement.x == 0 && velocityX != 0f && (velocityX > -snap && velocityX < snap)) velocityX = 0f;
                
        // Clamping forward velocity
        if (currentMovement.z > 0f && isRunPressed && velocityZ > currentMaxVelocity)
        {
            velocityZ = currentMaxVelocity;
        }
        // Decelerate to the mac walk velocity
        else if (currentMovement.z > 0f && velocityZ > currentMaxVelocity)
        {
            velocityZ -= Time.deltaTime * deceleration;
            // Round to currentMaxVelocity if within the offset
            if (velocityZ > currentMaxVelocity && velocityZ < (currentMaxVelocity + snap))
            {
                velocityZ = currentMaxVelocity;
            }
        }
        // Round to currentMaxVelocity if within the offset
        else if (currentMovement.z > 0f && velocityZ < currentMaxVelocity && velocityZ > (currentMaxVelocity - snap))
        {
            velocityZ = currentMaxVelocity;
        }

        // Clamping backward velocity
        if (currentMovement.z < 0f && isRunPressed && velocityZ < -currentMaxVelocity)
        {
            velocityZ = -currentMaxVelocity;
        }
        // Decelerate t the max walk velocity
        else if (currentMovement.z < 0f && velocityZ < -currentMaxVelocity)
        {
            velocityZ += Time.deltaTime * deceleration;
            // Round to currentMaxVelocity if within the offset
            if (velocityZ < -currentMaxVelocity && velocityZ > (-currentMaxVelocity + snap))
            {
                velocityZ = -currentMaxVelocity;
            }
        }
        // Round to currentMaxVelocity if within the offset
        else if (currentMovement.z < 0f && velocityZ > -currentMaxVelocity && velocityZ < (currentMaxVelocity - snap))
        {
            velocityZ = -currentMaxVelocity;
        }
        
        // Clamp Left
        if (currentMovement.x > 0f && isRunPressed && velocityX < -currentMaxVelocity)
        {
            velocityX = -currentMaxVelocity;
        }
        // Decelerate to max walk velocity
        else if (currentMovement.x > 0f && velocityX < -currentMaxVelocity)
        {
            velocityX -= Time.deltaTime * deceleration;
            // Round to currentMaxVelocity if within offset
            if (velocityX < -currentMaxVelocity && velocityX > (-currentMaxVelocity + snap))
            {
                velocityX = -currentMaxVelocity;
            }
        }
        // Round to currentMaxVelocity if within offset
        else if (currentMovement.x > 0f && velocityX > -currentMaxVelocity && velocityX < (-currentMaxVelocity - snap))
        {
            velocityX -= -currentMaxVelocity;
        }
        
        // Clamp right
        if (currentMovement.x < 0f && isRunPressed && velocityX > currentMaxVelocity)
        {
            velocityX = currentMaxVelocity;
        }
        // Decelerate to max walk velocity
        else if (currentMovement.x < 0f && velocityX > currentMaxVelocity)
        {
            velocityX += Time.deltaTime * deceleration;
            // Round to currentMaxVelocity if within offset
            if (velocityX > currentMaxVelocity && velocityX < (currentMaxVelocity + snap))
            {
                velocityX = currentMaxVelocity;
            }
        }
        // Round to currentMaxVelocity if within offset
        else if (currentMovement.x < 0f && velocityX < currentMaxVelocity && velocityX > (currentMaxVelocity - snap))
        {
            velocityX = currentMaxVelocity;
        }
    }

    private void RotationHandler()
    {
        if (isAimingDownSights)
        {
            Vector3 posToLookAt;
            posToLookAt.x = currentMovement.x;
            posToLookAt.y = 0f;
            posToLookAt.z = currentMovement.z;
            Quaternion currentRotation = transform.rotation;

            if (isMovementPressed)
            {
                Quaternion targetRotation = Quaternion.LookRotation(posToLookAt);
                transform.rotation = Quaternion.Slerp(currentRotation, targetRotation,
                    rotationFactorPerFrame * Time.deltaTime);
            }

        }
    }

    private void GravityHandler()
    {
        bool isFalling = currentMovement.y <= 0f || !isJumpPressed;
        float fallMultiplier = 2f;
        if (characterController.isGrounded)
        {
            if (isJumpAnimating)
            {
                animator.SetBool(isJumpingHash, false);
                jumpResetRoutine = StartCoroutine(JumpReset());
                if (jumpCount == 3)
                {
                    jumpCount = 0; 
                    animator.SetInteger(jumpCountHash, jumpCount);
                }
                isJumpAnimating = false;
            }
            currentMovement.y = groundedGravity;
            currentRunMovement.y = groundedGravity;
        }
        else if (isFalling)
        {
            float previousYVelocity = currentMovement.y;
            float newYVelocity = currentMovement.y + (jumpGravities[jumpCount] * fallMultiplier * Time.deltaTime);
            float nextYVelocity = Mathf.Max((previousYVelocity + newYVelocity) * 0.5f, -maxFallVelocity);
            currentMovement.y = nextYVelocity;
            currentRunMovement.y = nextYVelocity;
        }
        else
        {
            float previousYVelocity = currentMovement.y;
            float newYVelocity = currentMovement.y + (jumpGravities[jumpCount] * Time.deltaTime);
            float nextYVelocity = (previousYVelocity + newYVelocity) * 0.5f;
            currentMovement.y = nextYVelocity;
            currentRunMovement.y = nextYVelocity;
        }
    }

    private void OnEnable() => playerInput.CharacterControls.Enable();
    private void OnDisable() => playerInput.CharacterControls.Disable();
}