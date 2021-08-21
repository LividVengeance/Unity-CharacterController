using System.Collections;
using System.Collections.Generic;
using KinematicCharacterController;
using UnityEngine;

public struct PlayerCharacterInputs
{
    // All inputs the player can give to the character
    public float MoveAxisForward;
    public float MoveAxisRight;
    public Quaternion CameraRotation;
    public bool JumpDown;
    public bool CrouchDown;
    public bool CrouchUp;
}

public class CharacterController : MonoBehaviour, ICharacterController
{
    public KinematicCharacterMotor characterMotor;
    
    [Header("Stable Movement")]
    [SerializeField] private float maxStableMoveSpeed = 10f;
    [SerializeField] private float stableMovementSharpness = 15;
    [SerializeField] private float orientationSharpness = 10;

    [Header("Air Movement")]
    [SerializeField] private float maxAirMoveSpeed = 10f;
    [SerializeField] private float airAccelerationSpeed = 5f;
    [SerializeField] private float drag = 0.1f;
    
    [Header("Jumping")]
    [SerializeField] private bool allowJumpingWhenSliding = false;
    [SerializeField] private bool allowDoubleJump = false;
    [SerializeField] private bool allowWallJump = false;
    [SerializeField] private float jumpSpeed = 10f;
    [SerializeField, Tooltip("Time before landing where jump input will still allow jump once you land")] 
    private float jumpPreGroundingGraceTime = 0f;
    [SerializeField, Tooltip("Time after leaving stable ground where jump will still be allowed")]
    private float jumpPostGroundingGraceTime = 0f;

    [Header("Misc")] 
    [SerializeField, Tooltip("Always orient its up direction in the opposite direction of the gravity")] 
    private bool orientTowardsGravity;
    [SerializeField] private bool rotationObstruction;
    [SerializeField] private Vector3 gravity = new Vector3(0, -30f, 0);
    [SerializeField] private Transform meshRoot;
    
    [Header("Camera")]
    [SerializeField, Tooltip("Will have the character face camera look direction")] 
    private bool rotateToCameraFacing = true;
    
    // Input vectors
    private Vector3 moveInputVector;
    private Vector3 lookInputVector;
    
    // Jump vars
    private bool jumpRequested = false;
    private bool jumpConsumed = false;
    private bool jumpedThisFrame = false;
    private float timeSinceJumpRequested = Mathf.Infinity;
    private float timeSinceLastAbleToJump = 0f;
    // Double jump vars
    private bool doubleJumpConsumed = false;
    // Wall jump vars
    private bool canWallJump = false;
    private Vector3 wallJumpNormal;
    // Velocity to be added on next update 
    private Vector3 internalVelocityAdd = Vector3.zero;
    // Crouching vars
    private bool shouldBeCrouching = false;
    private bool isCrouching = false;
    private Collider[] probedColliders = new Collider[8];
    

    /// Start is called before the first frame update
    void Start()
    {
        // Assigns as the motors controller
        characterMotor.CharacterController = this;
    }

    /// Update is called once per frame
    void Update()
    {
        
    }

    /// This is called every frame by InputHandler to tell character what inputs its receiving
    public void SetInputs(ref PlayerCharacterInputs inputs)
    {
        // Clamp input
        Vector3 _moveInputVector = Vector3.ClampMagnitude(new Vector3(inputs.MoveAxisRight, 0f, inputs.MoveAxisForward), 1f);

        // Calculate camera direction and rotation on the character plane
        Vector3 cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.forward, characterMotor.CharacterUp).normalized;
        if (cameraPlanarDirection.sqrMagnitude == 0f)
        {
            cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.up, characterMotor.CharacterUp).normalized;
        }
        Quaternion cameraPlanarRotation = Quaternion.LookRotation(cameraPlanarDirection, characterMotor.CharacterUp);

        // Move and look inputs
        moveInputVector = cameraPlanarRotation * _moveInputVector;
        lookInputVector = cameraPlanarDirection;
        
        // Jumping input
        if (inputs.JumpDown)
        {
            timeSinceJumpRequested = 0f;
            jumpRequested = true;
        }
        
        // Crouching input
        if (inputs.CrouchDown)
        {
            shouldBeCrouching = true;

            if (!isCrouching)
            {
                isCrouching = true;
                characterMotor.SetCapsuleDimensions(0.5f, 1f, 0.5f);
                meshRoot.localScale = new Vector3(1f, 0.5f, 1f);
            }
        }
        else if (inputs.CrouchUp) shouldBeCrouching = false;
    }

    /// This is where you tell your character what its rotation should be right now. 
    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        if (rotateToCameraFacing && lookInputVector != Vector3.zero && orientationSharpness > 0f)
        {
            // Smoothly interpolate from current to target look direction
            Vector3 smoothedLookInputDirection = Vector3.Slerp(characterMotor.CharacterForward, lookInputVector, 
                1 - Mathf.Exp(-orientationSharpness * deltaTime)).normalized;

            // Set the current rotation (which will be used by the KinematicCharacterMotor)
            currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, 
                characterMotor.CharacterUp);
            
            if (orientTowardsGravity)
            {
                // Rotate from current up to invert gravity
                currentRotation = Quaternion.FromToRotation((currentRotation * Vector3.up), -gravity) * currentRotation;
            }
        }
    }

    /// This is where you tell your character what its velocity should be right now. 
    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        Vector3 targetMovementVelocity = Vector3.zero;
        
        // Is on stable ground
        if (characterMotor.GroundingStatus.IsStableOnGround)
        {
            // Reorient source velocity on current ground slope
            // (this is because we don't want our smoothing to cause any velocity losses in slope changes)
            currentVelocity = characterMotor.GetDirectionTangentToSurface(currentVelocity, 
                characterMotor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;

            // Calculate target velocity
            Vector3 inputRight = Vector3.Cross(moveInputVector, characterMotor.CharacterUp);
            Vector3 reorientedInput = Vector3.Cross(characterMotor.GroundingStatus.GroundNormal,
                inputRight).normalized * moveInputVector.magnitude;
            targetMovementVelocity = reorientedInput * maxStableMoveSpeed;

            // Smooth movement Velocity
            currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 
                1 - Mathf.Exp(-stableMovementSharpness * deltaTime));
        }
        else
        {
            // Add move input
            if (moveInputVector.sqrMagnitude > 0f)
            {
                targetMovementVelocity = moveInputVector * maxAirMoveSpeed;

                // Prevent climbing on un-stable slopes with air movement
                if (characterMotor.GroundingStatus.FoundAnyGround)
                {
                    Vector3 perpenticularObstructionNormal = Vector3.Cross(Vector3.Cross(
                        characterMotor.CharacterUp, characterMotor.GroundingStatus.GroundNormal),
                        characterMotor.CharacterUp).normalized;
                    targetMovementVelocity = Vector3.ProjectOnPlane(
                        targetMovementVelocity, perpenticularObstructionNormal);
                }

                Vector3 velocityDiff = Vector3.ProjectOnPlane(targetMovementVelocity - currentVelocity, gravity);
                currentVelocity += velocityDiff * airAccelerationSpeed * deltaTime;
            }

            // Gravity
            currentVelocity += gravity * deltaTime;

            // Drag
            currentVelocity *= (1f / (1f + (drag * deltaTime)));
        }
        
        // Handle jumping
        jumpedThisFrame = false;
        timeSinceJumpRequested += deltaTime;
        if (jumpRequested)
        {
            // Handle double jump
            if (allowDoubleJump)
            {
                if (jumpConsumed && !doubleJumpConsumed && (allowJumpingWhenSliding ? 
                    !characterMotor.GroundingStatus.FoundAnyGround : !characterMotor.GroundingStatus.IsStableOnGround))
                {
                    characterMotor.ForceUnground(0.1f);

                    // Add to the return velocity and reset jump state
                    currentVelocity += (characterMotor.CharacterUp * jumpSpeed) - Vector3.Project(currentVelocity, characterMotor.CharacterUp);
                    jumpRequested = false;
                    doubleJumpConsumed = true;
                    jumpedThisFrame = true;
                }
            }
            
            // See if we actually are allowed to jump
            if (canWallJump || !jumpConsumed && ((allowJumpingWhenSliding ? characterMotor.GroundingStatus.FoundAnyGround : 
                characterMotor.GroundingStatus.IsStableOnGround) || timeSinceLastAbleToJump <= jumpPostGroundingGraceTime))
            {
                // Calculate jump direction before un-grounding
                Vector3 jumpDirection = characterMotor.CharacterUp;
                
                // Wall jumping direction
                if (canWallJump) jumpDirection = wallJumpNormal;
                // Normal/double jumping direction
                else if (characterMotor.GroundingStatus.FoundAnyGround && !characterMotor.GroundingStatus.IsStableOnGround)
                {
                    jumpDirection = characterMotor.GroundingStatus.GroundNormal;
                }

                // Makes the character skip ground probing/snapping on its next update. 
                characterMotor.ForceUnground(0.1f);

                // Add to the return velocity and reset jump state
                currentVelocity += (jumpDirection * jumpSpeed) - Vector3.Project(currentVelocity, characterMotor.CharacterUp);
                jumpRequested = false;
                jumpConsumed = true;
                jumpedThisFrame = true;
            }
            
            // Reset wall jump
            canWallJump = false;
        }
        
        // Take into account additive velocity
        if (internalVelocityAdd.sqrMagnitude > 0f)
        {
            currentVelocity += internalVelocityAdd;
            internalVelocityAdd = Vector3.zero;
        }
    }

    public void BeforeCharacterUpdate(float deltaTime)
    {
    }

    public void PostGroundingUpdate(float deltaTime)
    {
        // Handle landing and leaving ground
        if (characterMotor.GroundingStatus.IsStableOnGround && !characterMotor.LastGroundingStatus.IsStableOnGround)
        {
            OnLanded();
        }
        else if (!characterMotor.GroundingStatus.IsStableOnGround && characterMotor.LastGroundingStatus.IsStableOnGround)
        {
            OnLeaveStableGround();
        }
    }

    /// This is called after the character has finished its movement update
    public void AfterCharacterUpdate(float deltaTime)
    {
        // Handle jumping pre-ground grace period
        if (jumpRequested && timeSinceJumpRequested > jumpPreGroundingGraceTime) jumpRequested = false;

        // Handle jumping while sliding
        if (allowJumpingWhenSliding ? characterMotor.GroundingStatus.FoundAnyGround : characterMotor.GroundingStatus.IsStableOnGround)
        {
            // If we're on a ground surface, reset jumping values
            if (!jumpedThisFrame)
            {
                doubleJumpConsumed = false;
                jumpConsumed = false;
            }
            timeSinceLastAbleToJump = 0f;
        }
        // Keep track of time since we were last able to jump (for grace period)
        else timeSinceLastAbleToJump += deltaTime;
        
        // Handle uncrouching
        if (isCrouching && !shouldBeCrouching)
        {
            // Do an overlap test with the character's standing height to see if there are any obstructions
            characterMotor.SetCapsuleDimensions(0.5f, 2f, 1f);
            if (characterMotor.CharacterCollisionsOverlap(
                characterMotor.TransientPosition,
                characterMotor.TransientRotation,
                probedColliders) > 0)
            {
                // If obstructions, just stick to crouching dimensions
                characterMotor.SetCapsuleDimensions(0.5f, 1f, 0.5f);
            }
            else
            {
                // If no obstructions, uncrouch
                meshRoot.localScale = new Vector3(1f, 1f, 1f);
                isCrouching = false;
            }
        }
    }

    public bool IsColliderValidForCollisions(Collider coll)
    {
        return true;
    }

    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
    }

    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
        ref HitStabilityReport hitStabilityReport)
    {
        // We can wall jump only if we are not stable on ground and are moving against an obstruction
        if (allowWallJump && !characterMotor.GroundingStatus.IsStableOnGround && !hitStabilityReport.IsStable)
        {
            canWallJump = true;
            wallJumpNormal = hitNormal;
        }
    }

    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition,
        Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
    {
    }

    public void OnDiscreteCollisionDetected(Collider hitCollider)
    {
    }

    private void OnLanded()
    {
        Debug.Log("Landed");
    }

    private void OnLeaveStableGround()
    {
        Debug.Log("Left ground");
    }
    
    /// Adds a velocity to the character controller
    public void AddVelocity(Vector3 velocity) =>  internalVelocityAdd += velocity;
}
