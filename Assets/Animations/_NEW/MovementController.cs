using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MovementController : MonoBehaviour
{
    private PlayerInput playerInput;
    private CharacterController characterController;
    private Animator animator;

    private Vector2 currentMovementInput;
    private Vector3 currentMovement;
    private Vector3 currentRunMovement;
    [SerializeField] private float runSpeed = 3f;
    private bool isMovementPressed;
    private bool isRunPressed;
    private float rotationFactorPerFrame = 15f;
    
    private void Awake()
    {
        playerInput = new PlayerInput();
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        
        // Gets and stores the inputs from the New Unity input system
        playerInput.CharacterControls.Move.started += OnMovementInput;
        playerInput.CharacterControls.Move.canceled += OnMovementInput;
        playerInput.CharacterControls.Move.performed += OnMovementInput;
        // Player sprint
        playerInput.CharacterControls.Run.started += OnRun;
        playerInput.CharacterControls.Run.canceled += OnRun;
    }

    private void OnMovementInput(InputAction.CallbackContext context)
    {
        currentMovementInput = context.ReadValue<Vector2>();
        currentMovement.x = currentMovementInput.x;
        currentMovement.z = currentMovementInput.y;

        currentRunMovement.x = currentMovementInput.x * runSpeed;
        currentRunMovement.z = currentMovementInput.y * runSpeed;
        
        isMovementPressed = currentMovementInput.x != 0 || currentMovementInput.y != 0; 
    }
    
    private void OnRun(InputAction.CallbackContext context)
    {
        isRunPressed = context.ReadValueAsButton();
    }

    // Update is called once per frame
    void Update()
    {
        AnimationHandler();
        RotationHandler();
        
        // Changes movement speed when running
        if (isRunPressed) characterController.Move(currentRunMovement * Time.deltaTime);
        else characterController.Move(currentMovement * Time.deltaTime);

    }

    private void AnimationHandler()
    {
        bool isWalking = animator.GetBool("isWalking");
        bool isRunning = animator.GetBool("isRunning");
        
        // Walking Animations
        if (isMovementPressed && !isWalking) animator.SetBool("isWalking", true);
        if (!isMovementPressed && isWalking) animator.SetBool("isWalking", false);
        
        // Running Animations
        if((isRunPressed && isMovementPressed) && !isRunning) animator.SetBool("isRunning", true);
        else if ((!isMovementPressed || !isRunning) && isRunning) animator.SetBool("isRunning", false);
    }

    private void RotationHandler()
    {
        Vector3 posToLookAt;
        posToLookAt.x = currentMovement.x;
        posToLookAt.y = 0f;
        posToLookAt.z = currentMovement.z;
        Quaternion currentRotation = transform.rotation;
        

        if (isMovementPressed)
        {
            Quaternion targetRotation = Quaternion.LookRotation(posToLookAt);
            transform.rotation = Quaternion.Slerp(currentRotation, targetRotation, rotationFactorPerFrame * Time.deltaTime);
        }
    }

    private void GravityHandler()
    {
        if (characterController.isGrounded)
        {
            float groundedGravity = -0.05f;
            currentMovement.y = groundedGravity;
            currentRunMovement.y = groundedGravity;
        }
        else
        {
            float gravity = -9.81f;
            currentMovement.y = gravity;
            currentRunMovement.y = gravity;
        }
    }

    private void OnEnable()
    {
        playerInput.CharacterControls.Enable();
    }

    private void OnDisable()
    {
        playerInput.CharacterControls.Disable();
    }
}
